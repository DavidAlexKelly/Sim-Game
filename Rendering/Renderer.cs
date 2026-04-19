using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimGame.Core;
using SimGame.Entities;
using SimGame.World;

namespace SimGame.Rendering
{
    public class Renderer
    {
        private readonly GraphicsDevice   _gd;
        private readonly BasicEffect      _effect;
        private readonly WorldRenderer    _worldRenderer;
        private readonly EntityRenderer   _entityRenderer;
        private readonly LightingRenderer _lightingRenderer;

        public Renderer(GraphicsDevice gd, SpriteBatch spriteBatch, int tileSize)
        {
            _gd    = gd;
            _effect = new BasicEffect(gd)
            {
                VertexColorEnabled = true,
                LightingEnabled    = false,
                TextureEnabled     = false
            };

            _worldRenderer    = new WorldRenderer(gd, tileSize);
            _entityRenderer   = new EntityRenderer(gd, tileSize);
            _lightingRenderer = new LightingRenderer(spriteBatch, gd);

            UpdateProjection();
        }

        public void UpdateProjection()
        {
            var vp = _gd.Viewport;
            _effect.Projection = Matrix.CreateOrthographicOffCenter(
                0, vp.Width, vp.Height, 0, -1f, 1f);
        }

        public void BakeWorld(World.World world)
            => _worldRenderer.Bake(world);

        public void BakeFoodOverlay(World.World world)
            => _worldRenderer.BakeFoodOverlay(world);

        public void Draw(
            IReadOnlyList<Character> characters,
            Camera                   camera,
            TimeSystem               time,
            WeatherSystem            weather)
        {
            _effect.View = camera.GetMatrix();
            _worldRenderer.Draw(_effect);
            _entityRenderer.Draw(characters, _effect);
            _lightingRenderer.Draw(time, weather, _gd);
        }

        public void Dispose()
        {
            _effect.Dispose();
            _worldRenderer.Dispose();
            _lightingRenderer.Dispose();
        }
    }
}