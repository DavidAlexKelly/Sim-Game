using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimGame.Core;
using SimGame.World;

namespace SimGame.UI
{
    /// <summary>
    /// Draws a bottom-right info panel when a tile or character is selected.
    /// </summary>
    public class InfoPanel
    {
        private readonly SpriteBatch _sb;
        private readonly SpriteFont  _font;
        private readonly Texture2D   _pixel;

        private const int PanelWidth   = 280;
        private const int PanelPadding = 12;
        private const int LineHeight   = 22;
        private const int MarginRight  = 12;
        private const int MarginBottom = 12;

        private static readonly Color BgColour      = new Color(10, 10, 10, 210);
        private static readonly Color BorderColour   = new Color(80, 80, 80, 255);
        private static readonly Color HeaderColour   = new Color(255, 220, 100);
        private static readonly Color LabelColour    = new Color(160, 160, 160);
        private static readonly Color ValueColour    = Color.White;

        public InfoPanel(SpriteBatch spriteBatch, SpriteFont font, GraphicsDevice gd)
        {
            _sb    = spriteBatch;
            _font  = font;
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        public void Draw(SelectionState selection, World.World world, GraphicsDevice gd)
        {
            if (selection.Kind == SelectionKind.None) return;

            var lines = BuildLines(selection, world);
            if (lines == null) return;

            int panelHeight = PanelPadding * 2 + lines.Length * LineHeight;
            int vp          = gd.Viewport.Height;
            int vpw         = gd.Viewport.Width;

            var panelRect = new Rectangle(
                vpw - PanelWidth - MarginRight,
                vp  - panelHeight - MarginBottom,
                PanelWidth,
                panelHeight);

            _sb.Begin();

            // Background
            DrawRect(panelRect, BgColour);

            // Border
            DrawBorder(panelRect, BorderColour);

            // Text lines
            int x = panelRect.X + PanelPadding;
            int y = panelRect.Y + PanelPadding;

            for (int i = 0; i < lines.Length; i++)
            {
                var (label, value, isHeader) = lines[i];

                if (isHeader)
                {
                    _sb.DrawString(_font, label, new Vector2(x, y), HeaderColour);
                }
                else
                {
                    _sb.DrawString(_font, label + ": ", new Vector2(x, y), LabelColour);
                    float labelWidth = _font.MeasureString(label + ": ").X;
                    _sb.DrawString(_font, value, new Vector2(x + labelWidth, y), ValueColour);
                }

                y += LineHeight;
            }

            _sb.End();
        }

        // ── Line builders ─────────────────────────────────────────────────────

        private (string label, string value, bool isHeader)[]? BuildLines(
            SelectionState selection, World.World world)
        {
            if (selection.Kind == SelectionKind.Character && selection.Character != null)
            {
                var c = selection.Character;
                return new[]
                {
                    (c.Name,                    "",                        true),
                    ("Hunger",                  $"{c.Hunger:P0}",          false),
                    ("Goal",                    c.Goal.ToString(),         false),
                    ("State",                   c.State.ToString(),        false),
                    ("Speed",                   $"{c.BasePixelsPerSecond / 16f:F1} tiles/s", false),
                    ("Position",                $"{c.Position.X:F0}, {c.Position.Y:F0}", false),
                };
            }

            if (selection.Kind == SelectionKind.Tile)
            {
                int tx = selection.TileCoord.X;
                int ty = selection.TileCoord.Y;

                if (!world.InBounds(tx, ty)) return null;

                ref var tile = ref world.GetTile(tx, ty);
                string foodStr = tile.Food.Type == FoodSourceType.None
                    ? "None"
                    : tile.Food.IsAvailable
                        ? $"{tile.Food.Type} ({tile.Food.Yield} left)"
                        : $"{tile.Food.Type} (respawning in {tile.Food.RespawnTicksRemaining})";

                return new[]
                {
                    ($"Tile ({tx}, {ty})",  "",                          true),
                    ("Type",                tile.Type.ToString(),        false),
                    ("Walkable",            tile.IsWalkable ? "Yes":"No",false),
                    ("Food",                foodStr,                     false),
                };
            }

            return null;
        }

        // ── Drawing helpers ───────────────────────────────────────────────────

        private void DrawRect(Rectangle rect, Color colour)
            => _sb.Draw(_pixel, rect, colour);

        private void DrawBorder(Rectangle rect, Color colour)
        {
            _sb.Draw(_pixel, new Rectangle(rect.X,                 rect.Y,                  rect.Width, 1),           colour);
            _sb.Draw(_pixel, new Rectangle(rect.X,                 rect.Bottom - 1,         rect.Width, 1),           colour);
            _sb.Draw(_pixel, new Rectangle(rect.X,                 rect.Y,                  1,          rect.Height), colour);
            _sb.Draw(_pixel, new Rectangle(rect.Right - 1,         rect.Y,                  1,          rect.Height), colour);
        }

        public void Dispose() => _pixel.Dispose();
    }
}