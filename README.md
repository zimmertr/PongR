# PongR
##Summary
PongR is a pong-like game written to have multiplayer functionality and a waiting room over a network connection. It is written in C# and the project uses SignalR. 

For my Software Development Tools class at Grand Valley State University, I was responsible for identifying an existing C# program that utilied Cordova, SignalR or any web framework and adding functionality to it.

When I found PongR, it already had matchmaking, scorekeeping, and other basic functionality like paddle collisions and hitboxes programmed in. But it was missing the ability to win, interesting game dynamics, etc.

![Alt text](https://raw.githubusercontent.com/zimmertr/PongR/master/Screenshots/home_page.png "Home Page")

##Added Functionality

- A new, more interesting, home page.
- Moved the 'Game Start' message up so it's not blocked by the ball.
- Adding a winning function to the game after 3 points.
- Added colored paddles to allow characters to more easily dientify their player over a remote connection.
- Made the ball color change to the color of the last paddle it hit to denote who struck the ball last.
- After each successful hit the ball shrinks in radius until it reaches a floor value to increase difficulty.
- After each successful hit the paddle shrinks in radius until it reaches a floor value to increase difficulty. 

![Alt text](https://raw.githubusercontent.com/zimmertr/PongR/master/Screenshots/game.png "Gameplay")


##Existing Problems

- Performance over the internet is ghastly at best. LAN is okay. 


##Ideas for Future Improvement

- Add powerups and better graphics.
- Tweak game engine to increase performance by reducing server calls. 
- Increasing the ball speed with each successful hit.
- Add sound effects to the collisions.
- Allow the ball to spawn in multiple directions.
- Implement a "multi-ball" powerup
- Allow the paddle to move on the x-axis as well 
- Add an "attack ball" powerup that destroys pieces of your paddle upon collision. 
