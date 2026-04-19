using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimGame.World;

namespace SimGame.Rendering
{
    /// <summary>
    /// Bakes the tile grid into a static VertexBuffer once and draws it
    /// cheaply every frame.
    ///
    /// Two passes are baked:
    ///   1. Terrain layer  — full tile quads, one colour per TileType.
    ///   2. Food overlay   — smaller centred quads drawn on tiles that have
    ///                       an available food source, tinted by source type.
    ///
    /// Both buffers are rebuilt together when the world changes (R to regen).
    /// The food overlay is rebuilt each tick by calling BakeFoodOverlay() from
    /// EntityManager so depleted / respawned sources update in real time.
    /// </summary>
    public class WorldRenderer
    {
        private readonly GraphicsDevice _gd;
        private readonly int            _tileSize;

        // Terrain (static after world gen)
        private VertexBuffer? _terrainVb;
        private IndexBuffer?  _terrainIb;
        private int           _terrainIndexCount;

        // Food overlay (rebuilt each tick)
        private VertexBuffer? _foodVb;
        private IndexBuffer?  _foodIb;
        private int           _foodIndexCount;

        // Fraction of tile size for the food indicator square
        private const float FoodIndicatorScale = 0.45f;

        public WorldRenderer(GraphicsDevice gd, int tileSize)
        {
            _gd       = gd;
            _tileSize = tileSize;
        }

        /// <summary>
        /// Call once after world generation. Bakes terrain and initial food overlay.
        /// </summary>
        public void Bake(World.World world)
        {
            BakeTerrain(world);
            BakeFoodOverlay(world);
        }

        /// <summary>
        /// Call once per sim tick to reflect consumed / respawned food sources.
        /// </summary>
        public void BakeFoodOverlay(World.World world)
        {
            // Count food tiles first so we allocate exactly the right buffer size
            int foodCount = 0;
            for (int x = 0; x < world.Width; x++)
                for (int y = 0; y < world.Height; y++)
                    if (world.GetTile(x, y).Food.IsAvailable) foodCount++;

            _foodVb?.Dispose();
            _foodIb?.Dispose();
            _foodIndexCount = 0;

            if (foodCount == 0) return;

            var verts   = new VertexPositionColor[foodCount * 4];
            var indices = new int[foodCount * 6];
            int vi = 0, ii = 0;

            float margin = _tileSize * (1f - FoodIndicatorScale) * 0.5f;
            float s      = _tileSize * FoodIndicatorScale;

            for (int x = 0; x < world.Width; x++)
            {
                for (int y = 0; y < world.Height; y++)
                {
                    ref var tile = ref world.GetTile(x, y);
                    if (!tile.Food.IsAvailable) continue;

                    float px = x * _tileSize + margin;
                    float py = y * _tileSize + margin;
                    var   c  = FoodSource.OverlayColor(tile.Food.Type);

                    int baseV = vi;
                    verts[vi++] = new VertexPositionColor(new Vector3(px,     py,     0), c);
                    verts[vi++] = new VertexPositionColor(new Vector3(px + s, py,     0), c);
                    verts[vi++] = new VertexPositionColor(new Vector3(px + s, py + s, 0), c);
                    verts[vi++] = new VertexPositionColor(new Vector3(px,     py + s, 0), c);

                    indices[ii++] = baseV;     indices[ii++] = baseV + 1; indices[ii++] = baseV + 2;
                    indices[ii++] = baseV;     indices[ii++] = baseV + 2; indices[ii++] = baseV + 3;
                }
            }

            _foodVb = new VertexBuffer(_gd, typeof(VertexPositionColor), verts.Length, BufferUsage.WriteOnly);
            _foodVb.SetData(verts);

            _foodIb = new IndexBuffer(_gd, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
            _foodIb.SetData(indices);

            _foodIndexCount = indices.Length;
        }

        public void Draw(BasicEffect effect)
        {
            DrawBuffer(_terrainVb, _terrainIb, _terrainIndexCount, effect);
            DrawBuffer(_foodVb,    _foodIb,    _foodIndexCount,    effect);
        }

        // ── Internals ────────────────────────────────────────────────────────

        private void BakeTerrain(World.World world)
        {
            int count   = world.Width * world.Height;
            var verts   = new VertexPositionColor[count * 4];
            var indices = new int[count * 6];
            int vi = 0, ii = 0;

            for (int x = 0; x < world.Width; x++)
            {
                for (int y = 0; y < world.Height; y++)
                {
                    ref var tile = ref world.GetTile(x, y);
                    float px = x * _tileSize;
                    float py = y * _tileSize;
                    float s  = _tileSize - 1;
                    var   c  = tile.Color;

                    int baseV = vi;
                    verts[vi++] = new VertexPositionColor(new Vector3(px,     py,     0), c);
                    verts[vi++] = new VertexPositionColor(new Vector3(px + s, py,     0), c);
                    verts[vi++] = new VertexPositionColor(new Vector3(px + s, py + s, 0), c);
                    verts[vi++] = new VertexPositionColor(new Vector3(px,     py + s, 0), c);

                    indices[ii++] = baseV;     indices[ii++] = baseV + 1; indices[ii++] = baseV + 2;
                    indices[ii++] = baseV;     indices[ii++] = baseV + 2; indices[ii++] = baseV + 3;
                }
            }

            _terrainVb?.Dispose();
            _terrainIb?.Dispose();

            _terrainVb = new VertexBuffer(_gd, typeof(VertexPositionColor), verts.Length, BufferUsage.WriteOnly);
            _terrainVb.SetData(verts);

            _terrainIb = new IndexBuffer(_gd, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
            _terrainIb.SetData(indices);

            _terrainIndexCount = indices.Length;
        }

        private void DrawBuffer(VertexBuffer? vb, IndexBuffer? ib, int indexCount, BasicEffect effect)
        {
            if (vb == null || ib == null || indexCount == 0) return;

            _gd.SetVertexBuffer(vb);
            _gd.Indices = ib;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, indexCount / 3);
            }
        }

        public void Dispose()
        {
            _terrainVb?.Dispose();
            _terrainIb?.Dispose();
            _foodVb?.Dispose();
            _foodIb?.Dispose();
        }
    }
}