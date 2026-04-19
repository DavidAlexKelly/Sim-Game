using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SimGame.Core
{
    /// <summary>
    /// Wraps MonoGame keyboard and mouse state to provide both held and
    /// just-pressed/clicked queries. Call Update() once per frame.
    /// </summary>
    public class InputHandler
    {
        private KeyboardState _current;
        private KeyboardState _previous;

        private MouseState _mouse;
        private MouseState _prevMouse;

        public void Update()
        {
            _previous  = _current;
            _current   = Keyboard.GetState();
            _prevMouse = _mouse;
            _mouse     = Mouse.GetState();
        }

        /// <summary>True while the key is held down.</summary>
        public bool IsHeld(Keys key) => _current.IsKeyDown(key);

        /// <summary>True only on the frame the key was first pressed.</summary>
        public bool JustPressed(Keys key)
            => _current.IsKeyDown(key) && _previous.IsKeyUp(key);

        /// <summary>True only on the frame the left mouse button was released.</summary>
        public bool JustClicked() =>
            _mouse.LeftButton    == ButtonState.Released &&
            _prevMouse.LeftButton == ButtonState.Pressed;

        /// <summary>Current mouse position in screen pixels.</summary>
        public Vector2 MouseScreenPos => new Vector2(_mouse.X, _mouse.Y);

        public KeyboardState State => _current;
    }
}