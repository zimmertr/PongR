# PongR
![Alt text](Screensots/home_page.png?raw=true "Home Page")

PongR is a pong-like game written to have multiplayer functionality and a waiting room over a network connection. It is written in C# and the project uses SignalR. 

For my Software Development Tools class at Grand Valley State University, I was responsible for identifying an existing C# program that utilied Cordova, SignalR or any web framework and adding functionality to it.

When I found PongR, it already had matchmaking, scorekeeping, and other basic functionality like paddle collisions and hitboxes programmed in. But it was missing the ability to win, interesting game dynamics, etc.

This project was hosted on a Windows Server 2012 R2 VM running on an ESXi 6.2U hypervisor running on my Mac Pro server at my apartment.

For this project, I added the following functionality:

1. A new, more interesting, home page.
2. Moved the 'Game Start' message up so it's not blocked by the ball.
3. Adding a winning function to the game after 3 points.
4. Added colored paddles to allow characters to more easily dientify their player over a remote connection.
5. Made the ball color change to the color of the last paddle it hit to denote who struck the ball last.
6. After each successful hit the ball shrinks in radius until it reaches a floor value to increase difficulty.
7. After each successful hit the paddle shrinks in radius until it reaches a floor value to increase difficulty. 

![Alt text](Screensots/game.png?raw=true "Gameplay")


Existing problems:

1. Performance over the internet is ghastly at best. LAN is okay. 


Future implementation ideas:

1. Add powerups and better graphics.
2. Tweak game engine to increase performance by reducing server calls. 
3. Increasing the ball speed with each successful hit.
4. Add sound effects to the collisions.
5. Allow the ball to spawn in multiple directions.
6. Implement a "multi-ball" powerup
7. Allow the paddle to move on the x-axis as well 
8. Add an "attack ball" powerup that destroys pieces of your paddle upon collision. 
