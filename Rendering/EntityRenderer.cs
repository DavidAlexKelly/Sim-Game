using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimGame.Entities;

namespace SimGame.Rendering
{
    /// <summary>
    /// Draws character entities as coloured diamonds.
    /// Rebuilds geometry every frame since positions change continuously.
    /// </summary>
    public class EntityRenderer
    {
        private readonly GraphicsDevice _gd;
        private readonly int            _tileSize;

        private static readonly Color ColourBody   = new Color(220, 60,  60);
        private static readonly Color ColourOutline = new Color(20,  20,  20);

        public EntityRenderer(GraphicsDevice gd, int tileSize)
        {
            _gd       = gd;
            _tileSize = tileSize;
        }

        public void Draw(IReadOnlyList<Character> characters, BasicEffect effect)
        {
            if (characters.Count == 0) return;

            // Each character: 5 verts (4 diamond points + centre), 4 triangles (12 indices)
            var verts   = new VertexPositionColor[characters.Count * 5];
            var indices = new int[characters.Count * 12];

            int vi = 0, ii = 0;
            float r  = _tileSize * 0.30f;   // inner diamond radius
            float ro = r + 1.5f;            // slightly larger for outline

            foreach (var ch in characters)
            {
                var p    = ch.RenderPos;
                int base0 = vi;

                // Outline diamond (rendered as slightly larger, dark coloured)
                verts[vi++] = new VertexPositionColor(new Vector3(p.X,      p.Y - ro, 0), ColourOutline);
                verts[vi++] = new VertexPositionColor(new Vector3(p.X + ro, p.Y,      0), ColourOutline);
                verts[vi++] = new VertexPositionColor(new Vector3(p.X,      p.Y + ro, 0), ColourOutline);
                verts[vi++] = new VertexPositionColor(new Vector3(p.X - ro, p.Y,      0), ColourOutline);
                verts[vi++] = new VertexPositionColor(new Vector3(p.X,      p.Y,      0), ColourBody);

                // 4 triangles fanning from centre
                indices[ii++] = base0;     indices[ii++] = base0 + 1; indices[ii++] = base0 + 4;
                indices[ii++] = base0 + 1; indices[ii++] = base0 + 2; indices[ii++] = base0 + 4;
                indices[ii++] = base0 + 2; indices[ii++] = base0 + 3; indices[ii++] = base0 + 4;
                indices[ii++] = base0 + 3; indices[ii++] = base0;     indices[ii++] = base0 + 4;
            }

            using var vb = new VertexBuffer(_gd, typeof(VertexPositionColor), verts.Length, BufferUsage.None);
            using var ib = new IndexBuffer(_gd, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.None);
            vb.SetData(verts);
            ib.SetData(indices);

            _gd.SetVertexBuffer(vb);
            _gd.Indices = ib;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, ii / 3);
            }
        }
    }
}
