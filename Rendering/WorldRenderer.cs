using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimGame.World;

namespace SimGame.Rendering
{
    /// <summary>
    /// Bakes the tile grid into a static VertexBuffer once and draws it cheaply
    /// every frame. Rebuild only when the world changes (e.g. regeneration).
    /// </summary>
    public class WorldRenderer
    {
        private readonly GraphicsDevice _gd;
        private readonly int            _tileSize;

        private VertexBuffer? _vb;
        private IndexBuffer?  _ib;
        private int           _indexCount;

        public WorldRenderer(GraphicsDevice gd, int tileSize)
        {
            _gd       = gd;
            _tileSize = tileSize;
        }

        /// <summary>Call once after world generation (or regeneration).</summary>
        public void Bake(World.World world)
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
                    float s  = _tileSize - 1;   // 1px gap creates subtle grid lines
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

            _vb?.Dispose();
            _ib?.Dispose();

            _vb = new VertexBuffer(_gd, typeof(VertexPositionColor), verts.Length, BufferUsage.WriteOnly);
            _vb.SetData(verts);

            _ib = new IndexBuffer(_gd, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
            _ib.SetData(indices);

            _indexCount = indices.Length;
        }

        public void Draw(BasicEffect effect)
        {
            if (_vb == null || _ib == null) return;

            _gd.SetVertexBuffer(_vb);
            _gd.Indices = _ib;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexCount / 3);
            }
        }

        public void Dispose()
        {
            _vb?.Dispose();
            _ib?.Dispose();
        }
    }
}
