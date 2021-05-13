# simple-pong

This project was an assignment for my Computer Engineering Technology coursework.

![Simple pong preview gif](readme-assets/preview.gif)

The game loop has a variable timestep with a fixed rendering timestep. This keeps the physics more or less in sync with realtime, while allowing the rendering to run at a fixed rate of 10 FPS to create a retro feeling. Mouse input is updated immediately, but is consumed by physics updates in order to keep the UI responsive.