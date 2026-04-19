using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimGame.Core;

namespace SimGame.UI
{
    /// <summary>
    /// Draws a lightweight HUD over the game using SpriteBatch.
    /// Keeps all UI string formatting out of the main game class.
    /// </summary>
    public class DebugHud
    {
        private readonly SpriteBatch _sb;
        private readonly SpriteFont  _font;

        private const string KeybindHint =
            "WASD: pan   +/-: zoom   Space: pause   1-4: speed   R: regen   Esc: quit";

        public DebugHud(SpriteBatch spriteBatch, SpriteFont font)
        {
            _sb   = spriteBatch;
            _font = font;
        }

        public void Draw(TickSystem ticks, int entityCount, int worldSeed)
        {
            string speedLabel = ticks.Paused
                ? "[PAUSED]"
                : $"Speed {ticks.SpeedMultiplier}x";

            string line1 = $"Tick: {ticks.TotalTicks}   Entities: {entityCount}   {speedLabel}   Seed: {worldSeed}";

            _sb.Begin();
            _sb.DrawString(_font, line1,       new Vector2(10, 10), Color.White);
            _sb.DrawString(_font, KeybindHint, new Vector2(10, 36), new Color(180, 180, 180));
            _sb.End();
        }
    }
}
