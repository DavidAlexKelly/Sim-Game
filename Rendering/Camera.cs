using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SimGame.Rendering
{
    /// <summary>
    /// Tracks world-space offset and zoom.
    /// Provides a Matrix for BasicEffect and a world↔screen transform helper.
    /// </summary>
    public class Camera
    {
        public Vector2 Position { get; set; }
        public float   Zoom     { get; private set; } = 1f;

        private const float PanSpeed  = 400f;   // pixels/sec at zoom 1
        private const float ZoomSpeed = 1.5f;   // multiplier per second
        private const float ZoomMin   = 0.25f;
        private const float ZoomMax   = 8f;

        private readonly GraphicsDevice _gd;

        public Camera(GraphicsDevice gd)
        {
            _gd = gd;
        }

        public void CentreOn(float worldPixelX, float worldPixelY)
        {
            Position = new Vector2(worldPixelX, worldPixelY);
        }

        public void Update(float deltaSeconds, KeyboardState kb)
        {
            float speed = PanSpeed / Zoom * deltaSeconds;

            if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up))    Position -= new Vector2(0, speed);
            if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down))  Position += new Vector2(0, speed);
            if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left))  Position -= new Vector2(speed, 0);
            if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) Position += new Vector2(speed, 0);

            if (kb.IsKeyDown(Keys.OemPlus)  || kb.IsKeyDown(Keys.Add))
                Zoom = MathHelper.Clamp(Zoom * (1f + ZoomSpeed * deltaSeconds), ZoomMin, ZoomMax);
            if (kb.IsKeyDown(Keys.OemMinus) || kb.IsKeyDown(Keys.Subtract))
                Zoom = MathHelper.Clamp(Zoom * (1f - ZoomSpeed * deltaSeconds), ZoomMin, ZoomMax);
        }

        /// <summary>
        /// Matrix to pass into BasicEffect.View.
        /// Translates so Position is centred on screen, then scales by Zoom.
        /// </summary>
        public Matrix GetMatrix()
        {
            var vp = _gd.Viewport;
            return Matrix.CreateTranslation(-Position.X, -Position.Y, 0f)
                 * Matrix.CreateScale(Zoom, Zoom, 1f)
                 * Matrix.CreateTranslation(vp.Width * 0.5f, vp.Height * 0.5f, 0f);
        }

        public Vector2 WorldToScreen(Vector2 world)
        {
            var vp = _gd.Viewport;
            return (world - Position) * Zoom + new Vector2(vp.Width * 0.5f, vp.Height * 0.5f);
        }

        public Vector2 ScreenToWorld(Vector2 screen)
        {
            var vp = _gd.Viewport;
            return (screen - new Vector2(vp.Width * 0.5f, vp.Height * 0.5f)) / Zoom + Position;
        }
    }
}
