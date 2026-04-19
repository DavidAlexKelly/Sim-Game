using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimGame.Entities;
using SimGame.World;

namespace SimGame.Rendering
{
    /// <summary>
    /// Top-level renderer. Owns the shared BasicEffect and orchestrates
    /// WorldRenderer and EntityRenderer in the correct draw order.
    /// </summary>
    public class Renderer
    {
        private readonly GraphicsDevice  _gd;
        private readonly BasicEffect     _effect;
        private readonly WorldRenderer   _worldRenderer;
        private readonly EntityRenderer  _entityRenderer;

        public Renderer(GraphicsDevice gd, int tileSize)
        {
            _gd = gd;

            _effect = new BasicEffect(gd)
            {
                VertexColorEnabled = true,
                LightingEnabled    = false,
                TextureEnabled     = false
            };

            _worldRenderer  = new WorldRenderer(gd, tileSize);
            _entityRenderer = new EntityRenderer(gd, tileSize);

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

        /// <summary>
        /// Exposed so EntityManager can trigger a food overlay rebuild
        /// after each sim tick without holding a reference to WorldRenderer directly.
        /// </summary>
        public void BakeFoodOverlay(World.World world)
            => _worldRenderer.BakeFoodOverlay(world);

        public void Draw(IReadOnlyList<Character> characters, Camera camera)
        {
            _effect.View = camera.GetMatrix();

            _worldRenderer.Draw(_effect);
            _entityRenderer.Draw(characters, _effect);
        }

        public void Dispose()
        {
            _effect.Dispose();
            _worldRenderer.Dispose();
        }
    }
}