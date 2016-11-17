﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Timers;


namespace PongR.Models
{
    public static class Engine
    {
        // Store of couples <playerRoomId, GameStatus> 
        private static Dictionary<string, Game> _games = new Dictionary<string, Game>();
        // Key : room Id / value: timestamp of the goal 
        private static Dictionary<string, DateTime> _goalTimestamps = new Dictionary<string, DateTime>();
        private const int BAR_SCROLL_UNIT = 400; // 330 px per second
        private const int BAR_SCROLL_UNIT_PERC = 5; // %
        private const int BALL_FIXED_STEP = 400; // 400 px per second 
        private const int FIELD_WIDTH = 1200; // px
        private const int FIELD_HEIGHT = 600; // px
        // Minimum distance between the player and the field delimiters (up and down)
        private const int FIXED_GAP = 30; // px
        private const int PAUSE_AFTER_GOAL = 3; //seconds
        private const double DELTA_TIME = 0.025; // The physics update loop runs each 15ms

        public static Game CreateGame(string gameId, Player host, Player opponent)
        {
            Random random = new Random();         
            string ballDirection = random.Next() % 2 == 0 ? "left" : "right";
            int ballAngle = ballDirection.Equals("left") ? 180 : 0;
            Game game = new Game(gameId, host, opponent, new Ball(ballDirection, ballAngle, FIELD_WIDTH, FIELD_HEIGHT));            
            return game;
        }

        public static void AddGame(Game game)
        {
            if (!_games.ContainsKey(game.GameId))
            {
                _games.Add(game.GameId, game);
            }
        }

        public static void RemoveGame(string gameId)
        {
            if (_games.ContainsKey(gameId))
            {
                _games.Remove(gameId);
            }
        }

        public static Player CreatePlayer(User user, int playerNumber, bool isHost)
        {
            return new Player(user, playerNumber, isHost, FIELD_WIDTH);
        }

        public static void QueueInput(string gameId, string userId, PlayerInput input)
        {   
            Game game;
            if (_games.TryGetValue(gameId, out game))
            {
                Player player = game.GetPlayer(userId);
                player.UnprocessedPlayerInputs.Enqueue(input);
            }
        }
                
        // Specify what you want to happen when the Elapsed event is 
        // raised.
        public static void OnPhysicsTimedEvent(object source, ElapsedEventArgs e)
        {
            Engine.ProcessGamesTick();           
        }

        // Specify what you want to happen when the Elapsed event is 
        // raised.
        public static void OnUpdateClientsTimedEvent(object source, ElapsedEventArgs e)
        {
            Engine.UpdateClients();
        }

        /// <summary>
        /// Physics loop where, for each game registered in _games we process its state based on users inputs
        /// </summary>
        private static void ProcessGamesTick()
        {
            DateTime timestamp;
            foreach(var game in _games.Values)
            {
                // If in one of the previous rounds we had a goal condition
                if (_goalTimestamps.TryGetValue(game.GameId, out timestamp))
                {
                    var now = DateTime.Now;
                    var timelapse = (now - timestamp).TotalSeconds;
                    // If not enough time has passed yet since the last goal, proceed with the next game
                    if (timelapse <= PAUSE_AFTER_GOAL)
                    {
                        continue;
                    }
                    //If more than 3 seconds have passed, let's update the state of the game!
                    else 
                    {
                        // Let's remove the timestamp, so that next round everything will be back to normal
                        _goalTimestamps.Remove(game.GameId);

                        // In the meantime a user could have sent inputs, but these inputs must be discarded
                        game.Player1.ResetPlayerToIntialPositionAndState(FIELD_WIDTH);
                        game.Player2.ResetPlayerToIntialPositionAndState(FIELD_WIDTH);                        
                    }
                } 
                // Process the new state
                ProcessTick(game);                
            }
        }

        private static void UpdateClients()
        {
            // Send to each group the updated Game object (maybe I need to add properties to it...)
            foreach (var game in _games.Values)
            {
                Notifier.UpdateClients(game);
            }
        }

        /// <summary>
        /// Compute the next state for this game, based on player inputs and ball position
        /// </summary>
        /// <param name="game"></param>
        private static void ProcessTick(Game game)
        {            
            // 1: Apply inputs received from players (and progressively remove them from the buffer)
            // 2: Update ball position
            // 3: Check for collisions and if collision, update ball status
            // 4: If no collision, check for a goal condition and update status if goal
            
            // 1: TODO Write Unit Test
            MovePlayer(game.Player1, FIELD_HEIGHT);
            MovePlayer(game.Player2, FIELD_HEIGHT);
            // 2: TODO Write Unit Test
            UpdateBallPosition(game.Ball); // Just update (X,Y) based on the angle
            // 3: TODO Write Unit Test
            if (!CheckCollisions(game))
            {
                // 4: TODO Write Unit Test
                var goal = CheckGoalConditionAndUpdateStatus(game);
                if (goal)
                {                    
                    _goalTimestamps.Add(game.GameId, DateTime.Now);               
                    RestartGameAfterGoal(game);                    
                }                
            }
        }
        
        /// <summary>
        /// Apply inputs received from players (and progressively remove them from the buffer)
        /// </summary>
        /// <param name="player"></param>
        private static void MovePlayer(Player player, int fieldHeight)
        {
            PlayerInput input;
            int lastInputExecuted = -1;
            List<PlayerInput> inputsToRemove = new List<PlayerInput>();
            if (player.UnprocessedPlayerInputs.Count == 0)
            {
                player.BarDirection = "";
            }
            else
            {
                while (player.UnprocessedPlayerInputs.Count > 0)
                {
                    input = player.UnprocessedPlayerInputs.Dequeue();
                    if (input != null)
                    {
                        lastInputExecuted = input.SequenceNumber;
                        var step = (int)Math.Round(BAR_SCROLL_UNIT * DELTA_TIME);
                        foreach (Command command in input.Commands)
                        {
                            if (command == Command.Up)
                            {
                                if (player.TopLeftVertex.Y - step >= FIXED_GAP)
                                {  // 30 px is the minimum distance from border                        
                                    player.TopLeftVertex.Y -= step;
                                    player.BarDirection = "up";
                                }
                                else
                                {
                                    player.TopLeftVertex.Y = FIXED_GAP;
                                    player.BarDirection = "up";
                                }
                            }
                            else if (command == Command.Down)
                            {
                                if (player.TopLeftVertex.Y + step <= fieldHeight - FIXED_GAP - player.BarHeight)
                                {
                                    player.TopLeftVertex.Y += step;
                                    player.BarDirection = "down";
                                }
                                else
                                {
                                    player.TopLeftVertex.Y = fieldHeight - FIXED_GAP - player.BarHeight;
                                }
                            }
                        }
                    }
                }
                player.LastProcessedInputId = lastInputExecuted;
            }
        }

        /// <summary>
        /// Update ball position
        /// </summary>
        /// <param name="ball"></param>
        private static void UpdateBallPosition(Ball ball)
        {
            int step = (int)Math.Round(BALL_FIXED_STEP * DELTA_TIME, 0);
            switch (ball.Angle)
            {
                case 0:
                    ball.Position.X += step; // Must be fixed step, at physics sync speed. 0.015 is the physics loop speed;
                    break;
                case 45:
                    ball.Position.X += step;
                    ball.Position.Y -= step;
                    break;
                case 135:
                    ball.Position.X -= step;
                    ball.Position.Y -= step;
                    break;
                case 180:
                    ball.Position.X -= step;
                    break;
                case 225:
                    ball.Position.X -= step;
                    ball.Position.Y += step;
                    break;
                case 315:
                    ball.Position.X += step;
                    ball.Position.Y += step;
                    break;
                default:
                    throw new Exception("Unknown angle value");
            }
        }

        private static bool CheckCollisions(Game game)
        {
            // check for collision
            // if collision with players' bar or field, update ball state (set next angle, next direction etc...)
            var collision = CheckCollisionWithPlayer(game);
            // No collision with player's bar, let's check if we have a collision with the field delimiters or if we have a goal condition
            if (!collision)
            {
                collision = CheckCollisionWithFieldDelimiters(game);                
            }

            return collision;
        }

        private static bool CheckCollisionWithFieldDelimiters(Game game)
        {
            var fieldCollision = false;
            int newAngle = -1;
            // Hit check. I check first for y axis because it's less frequent that the condition will be true, so most of the time 
            // we check only 1 if statement instead of 2 
            // We consider a hit when the ball is very close to the field delimiter (+/-5 px)
            if ((game.Ball.Position.Y >= - 5 && game.Ball.Position.Y <= + 5) ||
                    (game.Ball.Position.Y >= FIELD_HEIGHT - 5 && game.Ball.Position.Y <= FIELD_HEIGHT + 5))
            {
                if (game.Ball.Position.X >= 0 && game.Ball.Position.X <= FIELD_WIDTH)
                {
                    fieldCollision = true;
                    newAngle = CalculateNewAngleAfterFieldHit(game.Ball.Angle, game.Ball.Direction);
                }
            }
            if (fieldCollision)
            {
                game.Ball.Angle = newAngle;
            }
            return fieldCollision;
        }

        private static bool CheckCollisionWithPlayer(Game game)
        {
            var barCollision = false;
            string newBallDirection = string.Empty;
            int newAngle = 0;
            if (game.Player1.TopLeftVertex.X + game.Player1.BarWidth >= game.Ball.Position.X - game.Ball.Radius)
            {
                if ((game.Player1.TopLeftVertex.Y <= game.Ball.Position.Y + game.Ball.Radius)
                    && (game.Player1.TopLeftVertex.Y + game.Player1.BarHeight >= game.Ball.Position.Y - game.Ball.Radius))
                {
                    barCollision = true;
                    newBallDirection = "right";
                    newAngle = CalculateNewAngleAfterPlayerHit(game.Player1, newBallDirection);
                    if (game.Player1.BarHeight > 48)
                    {
                        game.Player1.BarHeight = game.Player1.BarHeight - 3;
                    }
                    if (game.Ball.Radius > 1)
                    {

                        game.Ball.Radius = game.Ball.Radius - 1;
                    }
                }
            }
            else if (game.Player2.TopLeftVertex.X <= game.Ball.Position.X + game.Ball.Radius)
            {
                if ((game.Player2.TopLeftVertex.Y <= game.Ball.Position.Y + game.Ball.Radius)
                    && (game.Player2.TopLeftVertex.Y + game.Player2.BarHeight >= game.Ball.Position.Y - game.Ball.Radius))
                {
                    barCollision = true;
                    newBallDirection = "left";
                    newAngle = CalculateNewAngleAfterPlayerHit(game.Player2, newBallDirection);
                    if (game.Player2.BarHeight > 48)
                    {
                        game.Player2.BarHeight = game.Player2.BarHeight - 3;
                    }
                    if (game.Ball.Radius > 1)
                    {

                        game.Ball.Radius = game.Ball.Radius - 1;
                    }
                }
            }
            if (barCollision)
            {
                game.Ball.Angle = newAngle;
                game.Ball.Direction = newBallDirection;
            }
            return barCollision;
        }

        private static int CalculateNewAngleAfterFieldHit(int oldAngle, string ballDirection)
        {
            int newAngle = 0;
            if (ballDirection == "right" && oldAngle == 45) {
                newAngle = 315;
            }
            else if (ballDirection == "right" && oldAngle == 315) {
                newAngle = 45;
            }
            else if (ballDirection == "left" && oldAngle == 135) {
                newAngle = 225;
            }
            else if (ballDirection == "left" && oldAngle == 225) {
                newAngle = 135;
            }
            else {                
                throw new Exception("Unknown new angle value");
            }
            return newAngle;
        }

        private static int CalculateNewAngleAfterPlayerHit(Player player, string newBallDirection)
        {
            int angle = 0;
            if (newBallDirection == "right" && player.BarDirection == "") {
                angle = 0;
            }
            else if (newBallDirection == "right" && player.BarDirection == "up") {
                angle = 45;
            }
            else if (newBallDirection == "left" && player.BarDirection == "up") {
                angle = 135;
            }
            else if (newBallDirection == "left" && player.BarDirection == "") {
                angle = 180;
            }
            else if (newBallDirection == "left" && player.BarDirection == "down") {
                angle = 225;
            }
            else if (newBallDirection == "right" && player.BarDirection == "down") {
                angle = 315;
            }
            else {
                throw new Exception("Unknown new angle value");
            }
            return angle;
        }
                
        private static bool CheckGoalConditionAndUpdateStatus(Game game)
        {
            var goal = false;
            if (game.Ball.Position.X <= 0)
            {
                game.Player2.Score++;
                goal = true;
            }
            else if (game.Ball.Position.X >= FIELD_WIDTH)
            {
                game.Player1.Score++;
                goal = true;
            }
            return goal;
        }

        private static void RestartGameAfterGoal(Game game)
        {
            //if a player won the game
            if (game.Player1.Score == 5)                                //added these lines for winning --SD 4-17-16
            {
                //reset scores
                game.Player1.Score = 0;
                game.Player2.Score = 0;

                //add win to player
                game.Player1.Wins++;
            }else if (game.Player2.Score == 5)
            {
                //reset scores
                game.Player1.Score = 0;
                game.Player2.Score = 0;

                //add win to player
                game.Player2.Wins++;
            }                                                         //added these lines for winning --SD 4-17-16


            Random random = new Random();            
            // Reset objects position to initial state
            game.Player1.ResetPlayerToIntialPositionAndState(FIELD_WIDTH);
            game.Player2.ResetPlayerToIntialPositionAndState(FIELD_WIDTH);
            string ballDirection = random.Next() % 2 == 0 ? "left" : "right";
            int ballAngle = ballDirection.Equals("left") ? 180 : 0;
            game.Ball.Radius = 30;
            game.Player1.BarHeight = 96;
            game.Player2.BarHeight = 96;
            game.Ball.ResetBallToInitialPosition(ballDirection, ballAngle, FIELD_WIDTH, FIELD_HEIGHT);
        }
    }
}