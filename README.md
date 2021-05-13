# simple-pong

This project was an assignment for my Computer Engineering Technology coursework.

![Simple pong preview gif](readme-assets/preview.gif)

I leveraged a few techniques both familiar to me and unique to this assignment:

* The game loop has a variable timestep with a fixed rendering timestep. This keeps the physics more or less in sync with realtime, while allowing the rendering to run at a fixed rate of 10 FPS to create a retro feeling
* Physics updates consume the mouse position, but the mouse position is always kept in its most recent state so that the UI feels responsive
* The assignment had a variety or requirements that limited the depth of the physics simulation, which allowed simple AABB collision checks to work with no phasing that would've required swept AABB
* With some experimentation, I found that increasing by `.2` ticks per second was a satisfying difficulty curve