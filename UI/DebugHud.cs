using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimGame.Core;
using SimGame.Entities;

namespace SimGame.UI
{
    /// <summary>
    /// Draws a lightweight HUD over the game using SpriteBatch.
    /// Now surfaces hunger stats so you can verify the system is working.
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

        public void Draw(TickSystem ticks, IReadOnlyList<Character> characters, int worldSeed)
        {
            string speedLabel = ticks.Paused ? "[PAUSED]" : $"Speed {ticks.SpeedMultiplier}x";

            // Count characters by goal
            int idle = 0, seeking = 0, eating = 0;
            float totalHunger = 0f;
            foreach (var c in characters)
            {
                totalHunger += c.Hunger;
                switch (c.Goal)
                {
                    case CharacterGoal.SeekingFood: seeking++; break;
                    case CharacterGoal.Eating:      eating++;  break;
                    default:                        idle++;    break;
                }
            }

            float avgHunger = characters.Count > 0 ? totalHunger / characters.Count : 0f;

            string line1 = $"Tick: {ticks.TotalTicks}   Entities: {characters.Count}" +
                           $"   {speedLabel}   Seed: {worldSeed}";

            string line2 = $"Avg hunger: {avgHunger:P0}" +
                           $"   Idle: {idle}   Seeking food: {seeking}   Eating: {eating}";

            _sb.Begin();
            _sb.DrawString(_font, line1,       new Vector2(10, 10), Color.White);
            _sb.DrawString(_font, line2,       new Vector2(10, 36), new Color(220, 180, 100));
            _sb.DrawString(_font, KeybindHint, new Vector2(10, 62), new Color(180, 180, 180));
            _sb.End();
        }
    }
}