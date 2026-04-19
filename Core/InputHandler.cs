using Microsoft.Xna.Framework.Input;

namespace SimGame.Core
{
    /// <summary>
    /// Wraps MonoGame keyboard state to provide both held-key and
    /// just-pressed queries. Call Update() once per frame.
    /// </summary>
    public class InputHandler
    {
        private KeyboardState _current;
        private KeyboardState _previous;

        public void Update()
        {
            _previous = _current;
            _current  = Keyboard.GetState();
        }

        /// <summary>True while the key is held down.</summary>
        public bool IsHeld(Keys key) => _current.IsKeyDown(key);

        /// <summary>True only on the frame the key was first pressed.</summary>
        public bool JustPressed(Keys key)
            => _current.IsKeyDown(key) && _previous.IsKeyUp(key);

        public KeyboardState State => _current;
    }
}
