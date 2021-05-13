/*
 * Name: Aaron Lip
 * Assignment: Lab 2 - Vintage Pong
 */
using System;
using System.Drawing;
using GDIDrawer;

namespace Lab2
{
    // Represents a pong paddle
    class Paddle
    {
        public (int x, int y) Position;  // This is where the paddle exists in the game engine's play area
        public (int x, int y) Velocity;  // This is how much the position will change every tick

        public Paddle((int x, int y) Position)
        {
            this.Position = Position;
            this.Velocity = (0, 0);
        }

        public void Update()
        {
            // Update position by applying velocity
            Position = (Position.x + Velocity.x, Position.y + Velocity.y);
        }

        public void HandleMousePosition(Point ScaledMousePosition)
        {
            // Velocity is based on whether the mouse is displaced above or below the paddle in order to chase the pointer
            if (Math.Abs(ScaledMousePosition.Y - Position.y) >= 2)
                Velocity.y = 2 * (ScaledMousePosition.Y > Position.y ? 1 : -1);
            else Velocity.y = 0;
        }

        public void Render(CDrawer Canvas)
        {
            Canvas.AddCenteredRectangle(Position.x, Position.y, 1, 10, Color.Red);
        }
    }

    // Represents a pong ball that bounces around the game area
    class Ball
    {
        public (int x, int y) Position;  // This is where the ball exists in the game engine's play area
        public (int x, int y) Velocity;  // This is how much the position will change every tick

        public Ball((int x, int y) Position, (int x, int y) Velocity)
        {
            this.Position = Position;
            this.Velocity = Velocity;
        }

        // Update the ball's physics variables, which would ordinarily be a Component of an entity
        // GDIDrawer locks us into .NET Framework which does not natively support abstract multiple inheritance (C# 8.0 in .NET Core approaches having this) or mixins
        // Implementing concise inheritance mechanisms is very far outside this assignment's rubric
        public void Update()
        {
            Position = (Position.x + Velocity.x, Position.y + Velocity.y);  // Update position
        }

        // Draws a 2 unit sized green ball at this object's Position
        public void Render(CDrawer Canvas)
        {
            Canvas.AddCenteredRectangle(Position.x, Position.y, 2, 2, Color.Green);
        }
    }

    // Metadata for some basic button functionality in the engine
    class Button
    {
        string Label;                    // This is the text displayed inside the button. It will be cut off if it doesn't fit the button's bounds
        float FontSize;                  // This is the size of the font, and I'm not sure right now whether it's in points, ems, or pixels. float suggests that it's points
        Color ForegroundColour;          // This is the colour of the button's text and outline
        (int x, int y) Position;         // This is where the button is located in the game window
        (int width, int height) Bounds;  // This defines the area of the button

        public Button((int x, int y) Position, (int width, int height) Bounds, Color ForegroundColour, string Label = "", float FontSize = 16f)
        {
            this.Position = Position;
            this.Bounds = Bounds;
            this.ForegroundColour = ForegroundColour;
            this.Label = Label;
            this.FontSize = FontSize;
        }

        // Draws a 1 pixel-thick outline of the button with text centered inside on a transparent background
        public void Render(CDrawer Canvas)
        {
            Canvas.AddRectangle(Position.x, Position.y, Bounds.width, Bounds.height, Color.FromArgb(0, 0, 0, 0), 1, ForegroundColour);
            Canvas.AddText(Label, FontSize, Position.x, Position.y, Bounds.width, Bounds.height, ForegroundColour);
        }

        // This event handler determines whether a click was inside a button and reports back to the caller
        // Ordinarily, this would be implemented with a callback instead to ensure code is event-driven and asynchronous
        public bool CheckClick(Point ScaledClickPosition)
        {
            // AABB Collision detection
            return (
                ScaledClickPosition.X >= Position.x &&
                ScaledClickPosition.X <= Position.x + Bounds.width &&
                ScaledClickPosition.Y >= Position.y &&
                ScaledClickPosition.Y <= Position.y + Bounds.height);
        }
    }

    class Program
    {
        // Declare engine variables
        static Random Random = new System.Random();                          // Seed an RNG with the current time to be used for the game
        static CDrawer Canvas = new CDrawer(160 * 5, 120 * 5, false, true);  // Create a GDIDrawer window with continuous update off and no duplicate event suppression
        static TimeSpan TickRate;                                            // How quickly physics, collisions, and input are updated
        static TimeSpan FrameRate = TimeSpan.FromSeconds((double)1 / 10);    // How quickly new frames are drawn
        static Point MousePosition = default;                                // The player's current mouse position
        static Point ClickPosition = default;                                // The last unprocessed mouse click position

        // Declare game entities
        static uint Score;
        static Paddle PlayerPaddle;
        static Ball Ball;
        static Button AgainButton;
        static Button QuitButton;

        static void Main(string[] args)
        {
            // Engine setup
            Canvas.Scale = 5;  // pixels are smol, let's make them more visible

            // Render a main menu
            Canvas.AddText("2021 CNT CMPE 1300\n\n\n", 24f, Color.Goldenrod);
            Canvas.AddText("\nAaron's Vintage Pong\n\n", 24f, Color.CadetBlue);
            Canvas.AddText("\n\n\nClick Anywhere", 24f, Color.White);
            Canvas.Render();

            // Pause until the user clicks inside the play area
            while (!Canvas.GetLastMouseLeftClickScaled(out ClickPosition));

            // Game setup
            Start:

            // Declare variables for the game loop
            DateTime Last = DateTime.Now;   // The last time the loop began processing
            DateTime Current = default;     // Current time
            TimeSpan Elapsed = default;     // Time elapsed since last
            TimeSpan Runtime = default;     // How long the game has been running
            TimeSpan LogicDelta = default;  // The time since the logic system was last updated
            TimeSpan FrameDelta = default;  // The time since the rendering system was last updated

            // The score begins at 0
            Score = default;

            // Draw 3 walls on the back buffer that aren't erased when the canvas is cleared
            for (int x = 0; x < Canvas.ScaledWidth; x++)
                for (int y = 0; y < Canvas.ScaledHeight; y += (x == Canvas.ScaledWidth - 1 ? 1 : Canvas.ScaledHeight - 1))  // Colour every vertical pixel only on the final iteration
                    Canvas.SetBBScaledPixel(x, y, Color.BlueViolet);

            // The ball starts in a random position on the left half moving right, with a random vertical velocity
            Ball = new Ball(
                (Math.Max(10, Random.Next(Canvas.ScaledWidth / 2)), Random.Next(5, Canvas.ScaledHeight - 5)),
                (1, (Random.Next(1) == 1 ? 1 : -1)));

            // The paddle is centered on the left side of the screen
            PlayerPaddle = new Paddle((0, Canvas.ScaledWidth / 2));

            // Here's the gameloop
            do
            {
                // This game has a dynamic tickrate that gets progressively faster
                TickRate = TimeSpan.FromSeconds(1.0d / (30.0d + 2 * Score));

                // Update timekeeping variables
                Current = DateTime.Now;
                Elapsed = Current - Last;
                Runtime += Elapsed;
                LogicDelta += Elapsed;
                FrameDelta += Elapsed;
                Last = Current;

                // Consume all the event queues until empty
                while (Canvas.GetLastMousePositionScaled(out MousePosition));  // Update the last mouse position
                while (Canvas.GetLastMouseLeftClickScaled(out _));
                while (Canvas.GetLastMouseRightClickScaled(out _));
                while (Canvas.GetLastMouseLeftReleaseScaled(out _));
                while (Canvas.GetLastMouseRightClickScaled(out _)) ;

                // Consume units of logic time until 'caught up' to real time
                while (LogicDelta >= TickRate)
                {
                    LogicDelta -= TickRate;
                    Update();
                }

                // Reset the render delta since the rendering system only needs to render the most recent state
                if (FrameDelta >= FrameRate)
                {
                    FrameDelta = default;
                    Render();

                    // Debug output
                    Canvas.AddText($"{1.0m / TickRate.Milliseconds * 100:N1} ticks per second", 16f, Canvas.ScaledWidth - 48, Canvas.ScaledHeight - 8, 48, 8, Color.FromArgb(64, 0, 255, 0));
                }

                System.Threading.Thread.Sleep(5);
            } while (InAABBBounds(
                Ball.Position,
                (0, Canvas.ScaledWidth),
                (0, Canvas.ScaledHeight)));

            // When the ball leaves the play area, clear the window
            Canvas.Clear();
            Canvas.BBColour = Color.Black;

            // Then render the final score and game control buttons
            Canvas.AddText($"Final Score: {Score}\n\nClick a button to continue", 24f, Color.White);
            AgainButton = new Button((Canvas.ScaledWidth - 40, Canvas.ScaledHeight - 12), (16, 8), Color.LightGreen, "Play Again", 8f);
            AgainButton.Render(Canvas);
            QuitButton = new Button((Canvas.ScaledWidth - 20, Canvas.ScaledHeight - 12), (16, 8), Color.Red, "Quit", 8f);
            QuitButton.Render(Canvas);
            Canvas.Render();

            // Prompt the user to quit or play again
            while (true) {

                // Handle left mouse-click events queued up
                while (Canvas.GetLastMouseLeftClickScaled(out ClickPosition))
                {
                    // The Play Again button restarts the game
                    // Every once in a blue moon, goto is relevant. Be wary of keeping it in projects that will continue development after being shipped.
                    if (AgainButton.CheckClick(ClickPosition)) goto Start;

                    // The quit button terminates the program immediately
                    if (QuitButton.CheckClick(ClickPosition))
                    {
                        Canvas.Close();
                        Environment.Exit(0);
                    }

                    // Passing control to other threads ensures that the event listener has opportunity to catch clicks properly
                    System.Threading.Thread.Sleep(1);
                }

            }
            // Optionally implement sound effects
        }

        // Updates each entity in the engine (balls and paddles in this case)
        static void Update()
        {
            // Update the paddle physics
            PlayerPaddle.HandleMousePosition(MousePosition);
            PlayerPaddle.Update();
            PlayerPaddle.Position.y = Math.Min(Math.Max(6, PlayerPaddle.Position.y), Canvas.ScaledHeight - 6);  // Clamp the paddle in the play area

            // Bounce the ball off horizontal walls
            if (!InAABBBounds(
                Ball.Position,
                (-1, Canvas.ScaledWidth - 3),
                (0, Canvas.ScaledHeight)))
            {
                Ball.Velocity.x *= -1;
            }

            // Bounce the ball of vertical walls
            if (!InAABBBounds(
                Ball.Position,
                (0, Canvas.ScaledWidth),
                (3, Canvas.ScaledHeight - 3)))
            {
                Ball.Velocity.y *= -1;
            }
                
            // Bounce the ball off the paddle
            if (InAABBBounds(
                Ball.Position,
                (PlayerPaddle.Position.x + 2, PlayerPaddle.Position.x + 2),
                (PlayerPaddle.Position.y - 5, PlayerPaddle.Position.y + 5)))
            {
                Ball.Velocity.x *= -1;
                Score += 1;
            }

            Ball.Update();

        }

        // Renders each entity and/or does post-processing
        static void Render()
        {
            Canvas.Clear();

            // Render the score centered on the screen
            Canvas.AddText(Score.ToString(), 24f, Color.SlateGray);

            PlayerPaddle.Render(Canvas);
            Ball.Render(Canvas);

            Canvas.Render();
        }

        // Uses AABB logic to determine whether a point intersects a bounding box
        public static bool InAABBBounds((int x, int y) Point, (int min, int max) XBounds, (int min, int max) YBounds)
        {
            return (
                Point.x >= XBounds.min &&
                Point.x <= XBounds.max &&
                Point.y >= YBounds.min &&
                Point.y <= YBounds.max);
        }
    }
}
