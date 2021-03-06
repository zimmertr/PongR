﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PongR.Models;
using SignalR.Hubs;

namespace PongR.Hubs
{
    public class PongRHub : Hub, IDisconnect
    {
        private InMemoryUserRepository _userRepository;
        private InMemoryRoomRepository _roomRepository;

        public PongRHub()
        {            
            _userRepository = InMemoryUserRepository.GetInstance();
            _roomRepository = InMemoryRoomRepository.GetInstance();
        }

        #region IDisconnect event handler
        /// <summary>
        /// Fired when a user disconnects. 
        /// </summary>
        /// <returns></returns>
        public Task Disconnect()
        {
            // 1: Get the user that disconnected
            // 2: Remove him from the list of connected users
            // 3: If the user was playing, notify the opponent that the user disconnected
            // 4: Re-queue the opponent in the waiting list
            // 5: Remove the room from the list
            User user = _userRepository.ConnectedUsers.Where(u => u.Id.Equals(Context.ConnectionId)).FirstOrDefault();            
            if (user != null)
            {
                _userRepository.RemoveUser(user);
                _userRepository.RemoveFromWaitingList(user);
                PlayRoom room = _roomRepository.Rooms.Where(r => (r.Player1.Id.Equals(user.Id) || r.Player2.Id.Equals(user.Id))).FirstOrDefault();
                // if the user was in the middle of a match
                if (room != null)
                {                    
                    var opponent = room.Player1.Id.Equals(user.Id) ? room.Player2 : room.Player1;
                    _userRepository.AddToWaitingList(opponent);
                    _roomRepository.Remove(room);
                    Engine.RemoveGame(room.Id);
                    return Clients[opponent.Id].opponentLeft();                    
                }
            }
            return null;
        }
        #endregion

        #region PongR event handlers
        /// <summary>
        /// Invoked when a new client joins the system
        /// </summary>        
        public void Joined()
        {                 
            // 1: Add user to list of connected users
            // 2: If waiting list is empty add user to waiting list            
            // 3: Else find an opponent (first in the waiting list) and remove him from the waiting list
            // 4: Create room and assign both users
            // 5: Create a group for this room
            // 6: Setup match (playRoom Id, initial ball direction, player on the left and right etc...)
            // 7: Notify the group the match can start
            // 8: Add the game to the list of games that the Engine must simulate
            var user = new User()
            {
                Id = Context.ConnectionId,
                Username = Caller.username
            };
            _userRepository.AddUser(user);
            if (_userRepository.WaitingList.Count() == 0)
            {
                _userRepository.AddToWaitingList(user);
                Caller.wait();
            }
            else
            {
                var opponent = _userRepository.WaitingList.First();
                _userRepository.RemoveFromWaitingList(opponent);
                var playRoom = new PlayRoom()
                {
                    Id = Guid.NewGuid().ToString(),
                    Player1 = opponent,
                    Player2 = user
                };
                _roomRepository.Add(playRoom);
                Task t1 = Groups.Add(opponent.Id, playRoom.Id);
                Task t2 = Groups.Add(user.Id, playRoom.Id);

                t1.Wait();
                t2.Wait();
                
                // Rough solution. We have to be sure the clients have received the group add messages over the wire
                // TODO: ask maybe on Jabbr or on StackOverflow and think about a better solution
                Thread.Sleep(3000);
                
                Player player1 = Engine.CreatePlayer(playRoom.Player1, 1, true);
                Player player2 = Engine.CreatePlayer(playRoom.Player2, 2, false);

                Game game = Engine.CreateGame(playRoom.Id, player1, player2);

                dynamic matchOptions = new ExpandoObject();
                matchOptions.PlayRoomId = playRoom.Id;
                matchOptions.Player1 = playRoom.Player1;
                matchOptions.Player2 = playRoom.Player2;
                matchOptions.BallDirection = game.Ball.Direction;
                
                Clients[playRoom.Id].setupMatch(matchOptions);

                Thread.Sleep(3000);
                Engine.AddGame(game);
            }
        }        
        
        public void QueueInput(string playRoomId, string userId, PlayerInput input)
        {
            Engine.QueueInput(playRoomId, userId, input);
        }

        #endregion
    }
}

