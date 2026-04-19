using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimGame.Core;
using SimGame.World;

namespace SimGame.Rendering
{
    /// <summary>
    /// Draws a full-screen colour overlay for day/night and weather lighting.
    /// Weather darkens the overlay on top of the diurnal cycle.
    /// </summary>
    public class LightingRenderer
    {
        private readonly SpriteBatch _sb;
        private readonly Texture2D   _pixel;
        private readonly BlendState  _multiplyBlend;

        private const float MinLight = 0.15f;
        private const float MaxLight = 1.00f;

        public LightingRenderer(SpriteBatch spriteBatch, GraphicsDevice gd)
        {
            _sb    = spriteBatch;
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _multiplyBlend = new BlendState
            {
                ColorBlendFunction    = BlendFunction.Add,
                ColorSourceBlend      = Blend.DestinationColor,
                ColorDestinationBlend = Blend.Zero,
                AlphaBlendFunction    = BlendFunction.Add,
                AlphaSourceBlend      = Blend.One,
                AlphaDestinationBlend = Blend.Zero
            };
        }

        public void Draw(TimeSystem time, WeatherSystem weather, GraphicsDevice gd)
        {
            // Combine day/night light with weather modifier
            float baseLight    = MathHelper.Lerp(MinLight, MaxLight, time.LightFactorSmooth);
            float weatherLight = baseLight * weather.LightingModifier;
            float light        = MathHelper.Clamp(weatherLight, MinLight, MaxLight);

            Color tint = ComputeTint(time, weather, light);

            var screen = new Rectangle(0, 0, gd.Viewport.Width, gd.Viewport.Height);

            _sb.Begin(blendState: _multiplyBlend);
            _sb.Draw(_pixel, screen, tint);
            _sb.End();
        }

        private static Color ComputeTint(
            TimeSystem    time,
            WeatherSystem weather,
            float         light)
        {
            var (sr, sg, sb) = time.Season switch
            {
                0 => ( 0.00f,  0.04f,  0.00f),
                1 => ( 0.06f,  0.03f, -0.04f),
                2 => ( 0.06f,  0.01f, -0.06f),
                3 => (-0.04f,  0.00f,  0.08f),
                _ => ( 0.00f,  0.00f,  0.00f)
            };

            // Weather colour shifts
            var (wr, wg, wb) = weather.Current switch
            {
                WeatherState.Rain   => (-0.02f, -0.01f,  0.04f), // blue-grey
                WeatherState.Storm  => (-0.04f, -0.02f,  0.02f), // dark grey
                WeatherState.Snow   => ( 0.05f,  0.05f,  0.08f), // cold white-blue
                WeatherState.Cloudy => (-0.01f, -0.01f,  0.01f), // slight grey
                _                   => ( 0.00f,  0.00f,  0.00f)
            };

            float nightBlend = 1f - time.LightFactorSmooth;
            float nr = -0.02f * nightBlend;
            float ng = -0.01f * nightBlend;
            float nb =  0.06f * nightBlend;

            float r = MathHelper.Clamp(light + sr + wr + nr, 0f, 1f);
            float g = MathHelper.Clamp(light + sg + wg + ng, 0f, 1f);
            float b = MathHelper.Clamp(light + sb + wb + nb, 0f, 1f);

            return new Color(r, g, b, 1f);
        }

        public void Dispose()
        {
            _pixel.Dispose();
            _multiplyBlend.Dispose();
        }
    }
}