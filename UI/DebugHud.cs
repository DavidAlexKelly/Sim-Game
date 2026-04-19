using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimGame.Core;
using SimGame.Entities;
using SimGame.World;

namespace SimGame.UI
{
    /// <summary>
    /// Draws a lightweight HUD over the game using SpriteBatch.
    /// Shows tick/speed, hunger stats, time/season, wind, weather,
    /// planetary position, and terrain profile.
    /// </summary>
    public class DebugHud
    {
        private readonly SpriteBatch _sb;
        private readonly SpriteFont  _font;

        private const string KeybindHint =
            "WASD: pan  +/-: zoom  Space: pause  1-6: speed  R: regen  Esc: quit";

        public DebugHud(SpriteBatch spriteBatch, SpriteFont font)
        {
            _sb   = spriteBatch;
            _font = font;
        }

        public void Draw(
            TickSystem               ticks,
            TimeSystem               time,
            World.World              world,
            IReadOnlyList<Character> characters,
            int                      worldSeed)
        {
            string speedLabel = ticks.Paused
                ? "[PAUSED]"
                : $"Speed {ticks.SpeedMultiplier}x";

            // ── Character stats ───────────────────────────────────────────────
            int   idle = 0, seeking = 0, eating = 0;
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

            float avgHunger = characters.Count > 0
                ? totalHunger / characters.Count
                : 0f;

            // ── Planetary info ────────────────────────────────────────────────
            var planet = world.PlanetarySettings;

            // ── Build lines ───────────────────────────────────────────────────

            string line1 = $"Tick: {ticks.TotalTicks}  " +
                           $"Entities: {characters.Count}  " +
                           $"{speedLabel}  " +
                           $"Seed: {worldSeed}";

            string line2 = $"Avg hunger: {avgHunger:P0}  " +
                           $"Idle: {idle}  " +
                           $"Seeking food: {seeking}  " +
                           $"Eating: {eating}";

            string line3 = $"Year {time.Year}  " +
                           $"{time.MonthName}  " +
                           $"Day {time.Day}  " +
                           $"{time.Hour:D2}:{time.Minute:D2}  " +
                           $"{time.TimeOfDayLabel}  " +
                           $"({time.SeasonName})  " +
                           $"Light: {time.LightFactorSmooth:P0}";

            string line4 = $"Wind: {world.Wind.Direction}  " +
                           $"{world.Wind.SpeedLabel}  " +
                           $"({world.Wind.Speed:F2})";

            string line5 = $"Weather: {world.Weather.WeatherLabel}  " +
                           $"Rain: {world.Weather.RainfallIntensity:F2}  " +
                           $"Snow: {world.Weather.SnowfallIntensity:F2}  " +
                           $"Light mod: {world.Weather.LightingModifier:F2}";

            string line6 = $"Lat: {planet.LatitudeLabel}  " +
                           $"Lon: {planet.LongitudeLabel}  " +
                           $"Zone: {planet.ClimateZoneLabel}  " +
                           $"Terrain: {world.TerrainProfileLabel}";

            // Base climate values at map centre for quick reference
            float centreTemp  = PlanetarySettings.BaseTemperature(planet.CentreLatitude);
            float centrePrec  = PlanetarySettings.BasePrecipitation(planet.CentreLatitude);

            string line7 = $"Base climate — " +
                           $"Temp: {centreTemp:F2}  " +
                           $"Precip: {centrePrec:F2}  " +
                           $"Lat span: {planet.LatitudeMin:F4}° to {planet.LatitudeMax:F4}°";

            // ── Draw ──────────────────────────────────────────────────────────

            _sb.Begin();

            _sb.DrawString(_font, line1,
                new Vector2(10, 10),  Color.White);

            _sb.DrawString(_font, line2,
                new Vector2(10, 36),  new Color(220, 180, 100));

            _sb.DrawString(_font, line3,
                new Vector2(10, 62),  new Color(120, 200, 255));

            _sb.DrawString(_font, line4,
                new Vector2(10, 88),  new Color(180, 220, 180));

            _sb.DrawString(_font, line5,
                new Vector2(10, 114), new Color(150, 200, 255));

            _sb.DrawString(_font, line6,
                new Vector2(10, 140), new Color(200, 180, 255));

            _sb.DrawString(_font, line7,
                new Vector2(10, 166), new Color(180, 180, 220));

            _sb.DrawString(_font, KeybindHint,
                new Vector2(10, 192), new Color(180, 180, 180));

            _sb.End();
        }
    }
}