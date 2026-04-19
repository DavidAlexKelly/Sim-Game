using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimGame.Core;
using SimGame.World;

namespace SimGame.UI
{
    /// <summary>
    /// Draws a bottom-right info panel when a tile or character is selected.
    /// Shows full tile data including biome, climate, elevation, temperature,
    /// moisture, water table, wind, food state, and snow cover.
    /// </summary>
    public class InfoPanel
    {
        private readonly SpriteBatch _sb;
        private readonly SpriteFont  _font;
        private readonly Texture2D   _pixel;

        private const int PanelWidth   = 320;
        private const int PanelPadding = 12;
        private const int LineHeight   = 22;
        private const int MarginRight  = 12;
        private const int MarginBottom = 12;

        private static readonly Color BgColour    = new Color(10,  10,  10,  220);
        private static readonly Color BorderColour = new Color(80,  80,  80,  255);
        private static readonly Color HeaderColour = new Color(255, 220, 100);
        private static readonly Color LabelColour  = new Color(160, 160, 160);
        private static readonly Color ValueColour  = Color.White;

        private static readonly Color ColdColour   = new Color(120, 180, 255);
        private static readonly Color HotColour    = new Color(255, 120, 60);
        private static readonly Color WetColour    = new Color(80,  160, 220);
        private static readonly Color DryColour    = new Color(210, 170, 80);
        private static readonly Color GoodColour   = new Color(120, 220, 120);
        private static readonly Color WarnColour   = new Color(220, 180, 60);
        private static readonly Color DangerColour = new Color(220, 80,  80);

        public InfoPanel(SpriteBatch spriteBatch, SpriteFont font, GraphicsDevice gd)
        {
            _sb    = spriteBatch;
            _font  = font;
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        public void Draw(
            SelectionState selection,
            World.World    world,
            GraphicsDevice gd)
        {
            if (selection.Kind == SelectionKind.None) return;

            var lines = BuildLines(selection, world);
            if (lines == null) return;

            int panelHeight = PanelPadding * 2 + lines.Length * LineHeight;
            int vpH = gd.Viewport.Height;
            int vpW = gd.Viewport.Width;

            var panelRect = new Rectangle(
                vpW - PanelWidth - MarginRight,
                vpH - panelHeight - MarginBottom,
                PanelWidth,
                panelHeight);

            _sb.Begin();
            DrawRect(panelRect,   BgColour);
            DrawBorder(panelRect, BorderColour);

            int x = panelRect.X + PanelPadding;
            int y = panelRect.Y + PanelPadding;

            foreach (var (label, value, labelCol, valueCol, isHeader) in lines)
            {
                if (isHeader)
                {
                    _sb.DrawString(_font, label, new Vector2(x, y), labelCol);
                }
                else
                {
                    _sb.DrawString(_font, label + ": ", new Vector2(x, y), labelCol);
                    float lw = _font.MeasureString(label + ": ").X;
                    _sb.DrawString(_font, value, new Vector2(x + lw, y), valueCol);
                }
                y += LineHeight;
            }

            _sb.End();
        }

        // ── Line builders ─────────────────────────────────────────────────────

        private (string label, string value, Color labelCol, Color valueCol, bool isHeader)[]?
            BuildLines(SelectionState selection, World.World world)
        {
            if (selection.Kind == SelectionKind.Character
                && selection.Character != null)
                return BuildCharacterLines(selection.Character, world);

            if (selection.Kind == SelectionKind.Tile)
            {
                int tx = selection.TileCoord.X;
                int ty = selection.TileCoord.Y;
                if (!world.InBounds(tx, ty)) return null;
                return BuildTileLines(tx, ty, world);
            }

            return null;
        }

        // ── Character panel ───────────────────────────────────────────────────

        private (string, string, Color, Color, bool)[] BuildCharacterLines(
            Entities.Character c,
            World.World        world)
        {
            int   tx    = (int)(c.Position.X / 16);
            int   ty    = (int)(c.Position.Y / 16);
            float temp  = world.GetTemperature(tx, ty);
            float elev  = world.GetElevation(tx, ty);
            float chill = world.GetWindChill(tx, ty);
            float moist = world.GetSurfaceMoisture(tx, ty);
            float wt    = world.GetWaterTable(tx, ty);
            var   biome = world.GetBiome(tx, ty);

            float climTemp = world.BiomeMap.Climate.GetTemperature(tx, ty);
            float climPrec = world.BiomeMap.Climate.GetPrecipitation(tx, ty);

            Color tempCol   = TempColour(temp);
            Color chillCol  = chill < -2f ? ColdColour : ValueColour;
            Color moistCol  = MoistureColour(moist);
            Color hungerCol = c.Hunger > 0.7f ? DangerColour
                            : c.Hunger > 0.4f ? WarnColour
                            : GoodColour;

            return new[]
            {
                // Header
                (c.Name,         "",                                            HeaderColour, HeaderColour,              true),

                // Identity
                ("ID",           $"{c.Id}",                                    LabelColour,  ValueColour,               false),
                ("Biome",        BiomeData.Name(biome),                        LabelColour,  BiomeColour(biome),        false),

                // Needs
                ("Hunger",       $"{c.Hunger:P0}",                             LabelColour,  hungerCol,                 false),

                // Behaviour
                ("Goal",         c.Goal.ToString(),                            LabelColour,  GoalColour(c.Goal),        false),
                ("State",        c.State.ToString(),                           LabelColour,  ValueColour,               false),
                ("Speed",        $"{c.BasePixelsPerSecond / 16f:F1} tiles/s",  LabelColour,  ValueColour,               false),

                // Position
                ("Position",     $"{c.Position.X:F0}, {c.Position.Y:F0}",     LabelColour,  ValueColour,               false),
                ("Tile",         $"{tx}, {ty}",                                LabelColour,  ValueColour,               false),
                ("Elevation",    $"{elev * 3000f:F0}m",                        LabelColour,  ValueColour,               false),

                // Climate
                ("Climate temp", $"{climTemp:F2}",                             LabelColour,  ClimTempColour(climTemp),  false),
                ("Climate prec", $"{climPrec:F2}",                             LabelColour,  ClimPrecColour(climPrec),  false),

                // Environment
                ("Temp",         $"{temp:F1}°C  {TempLabel(temp)}",            LabelColour,  tempCol,                   false),
                ("Wind chill",   $"{chill:F1}°C",                              LabelColour,  chillCol,                  false),
                ("Moisture",     $"{moist:F2}  {MoistureLabel(moist)}",        LabelColour,  moistCol,                  false),
                ("Water table",  $"{wt:F2}  {WaterTableLabel(wt)}",            LabelColour,  WaterTableColour(wt),      false),
            };
        }

        // ── Tile panel ────────────────────────────────────────────────────────

        private (string, string, Color, Color, bool)[] BuildTileLines(
            int         tx,
            int         ty,
            World.World world)
        {
            ref var tile      = ref world.GetTile(tx, ty);
            float windChill   = world.GetWindChill(tx, ty);
            float exposure    = world.Wind.GetExposure(tx, ty);
            float slope       = world.ElevationMap.GetSlope(tx, ty);
            var   flowDir     = world.ElevationMap.GetFlowDirection(tx, ty);
            float climTemp    = world.BiomeMap.Climate.GetTemperature(tx, ty);
            float climPrec    = world.BiomeMap.Climate.GetPrecipitation(tx, ty);

            string flowStr = flowDir.HasValue
                ? FlowLabel(flowDir.Value.dx, flowDir.Value.dy)
                : "Pool (local min)";

            string foodStr = tile.Food.Type == FoodSourceType.None
                ? "None"
                : tile.Food.IsAvailable
                    ? $"{tile.Food.Type} ({tile.Food.Yield} left)"
                    : $"{tile.Food.Type} (respawns in {tile.Food.RespawnTicksRemaining})";

            string snowStr = tile.SnowCover > 0.01f
                ? $"{tile.SnowCover:P0}"
                : "None";

            // Value colours
            Color tempCol     = TempColour(tile.Temperature);
            Color chillCol    = windChill < -2f ? ColdColour : ValueColour;
            Color moistCol    = MoistureColour(tile.SurfaceMoisture);
            Color wtCol       = WaterTableColour(tile.WaterTable);
            Color droughtCol  = tile.DroughtTicks > 500 ? DangerColour
                              : tile.DroughtTicks > 200 ? WarnColour
                              : GoodColour;
            Color foodCol     = tile.Food.Type == FoodSourceType.None ? LabelColour
                              : tile.Food.IsAvailable                 ? GoodColour
                              : WarnColour;
            Color snowCol     = tile.SnowCover > 0.5f  ? ColdColour
                              : tile.SnowCover > 0.01f ? new Color(180, 210, 230)
                              : ValueColour;
            Color slopeCol    = slope > 0.15f ? WarnColour
                              : slope > 0.08f ? ValueColour
                              : GoodColour;
            Color exposureCol = exposure > 0.8f ? WarnColour
                              : exposure < 0.3f ? GoodColour
                              : ValueColour;
            Color walkCol     = tile.IsWalkable ? GoodColour : DangerColour;

            return new[]
            {
                // Header
                ($"Tile ({tx}, {ty})",  "",                                                  HeaderColour, HeaderColour,              true),

                // Biome / type
                ("Biome",       BiomeData.Name(tile.Biome),                                  LabelColour,  BiomeColour(tile.Biome),   false),
                ("Type",        tile.Type.ToString(),                                        LabelColour,  ValueColour,               false),
                ("Walkable",    tile.IsWalkable ? "Yes" : "No",                             LabelColour,  walkCol,                   false),
                
                ("Latitude",    $"{world.PlanetarySettings.GetLatitude(ty, world.Height):F1}°", LabelColour, new Color(200, 180, 255), false),

                // Elevation
                ("Elevation",   $"{tile.Elevation * 3000f:F0}m  ({tile.Elevation:F2})",     LabelColour,  ValueColour,               false),
                ("Slope",       $"{slope:F3}",                                               LabelColour,  slopeCol,                  false),
                ("Flow",        flowStr,                                                     LabelColour,  ValueColour,               false),

                // Climate (static world-gen values)
                ("Climate temp",$"{climTemp:F2}  {ClimTempLabel(climTemp)}",                 LabelColour,  ClimTempColour(climTemp),  false),
                ("Climate prec",$"{climPrec:F2}  {ClimPrecLabel(climPrec)}",                 LabelColour,  ClimPrecColour(climPrec),  false),

                // Live temperature
                ("Temp",        $"{tile.Temperature:F1}°C  {TempLabel(tile.Temperature)}",  LabelColour,  tempCol,                   false),
                ("Base temp",   $"{tile.BaseTemperature:F1}°C",                             LabelColour,  ValueColour,               false),
                ("Wind chill",  $"{windChill:F1}°C",                                        LabelColour,  chillCol,                  false),
                ("Exposure",    $"{exposure:P0}",                                           LabelColour,  exposureCol,               false),

                // Hydrology
                ("Moisture",    $"{tile.SurfaceMoisture:F2}  {MoistureLabel(tile.SurfaceMoisture)}", LabelColour, moistCol,          false),
                ("Water table", $"{tile.WaterTable:F2}  {WaterTableLabel(tile.WaterTable)}", LabelColour, wtCol,                     false),
                ("Drought",     $"{tile.DroughtTicks} ticks",                               LabelColour,  droughtCol,                false),
                ("Snow",        snowStr,                                                    LabelColour,  snowCol,                   false),

                // Food
                ("Food",        foodStr,                                                    LabelColour,  foodCol,                   false),
            };
        }

        // ── Text label helpers ────────────────────────────────────────────────

        private static string TempLabel(float t) => t switch
        {
            < -15f => "Extreme cold",
            < -10f => "Freezing",
            <   0f => "Very cold",
            <  10f => "Cold",
            <  18f => "Cool",
            <  24f => "Comfortable",
            <  30f => "Warm",
            <  38f => "Hot",
            <  45f => "Very hot",
            _      => "Scorching"
        };

        private static string MoistureLabel(float m) => m switch
        {
            < 0.05f => "Bone dry",
            < 0.15f => "Parched",
            < 0.30f => "Dry",
            < 0.50f => "Moderate",
            < 0.65f => "Moist",
            < 0.80f => "Wet",
            _       => "Saturated"
        };

        private static string WaterTableLabel(float wt) => wt switch
        {
            < 0.10f => "Depleted",
            < 0.25f => "Low",
            < 0.50f => "Moderate",
            < 0.70f => "High",
            _       => "Saturated"
        };

        private static string ClimTempLabel(float t) => t switch
        {
            < 0.20f => "Polar",
            < 0.35f => "Cold",
            < 0.50f => "Cool",
            < 0.65f => "Warm",
            < 0.80f => "Hot",
            _       => "Equatorial"
        };

        private static string ClimPrecLabel(float p) => p switch
        {
            < 0.15f => "Hyper-arid",
            < 0.30f => "Arid",
            < 0.45f => "Semi-arid",
            < 0.60f => "Moderate",
            < 0.75f => "Humid",
            _       => "Very humid"
        };

        private static string FlowLabel(int dx, int dy)
        {
            string h = dx switch { > 0 => "E", < 0 => "W", _ => "" };
            string v = dy switch { > 0 => "S", < 0 => "N", _ => "" };
            string dir = $"{v}{h}";
            return string.IsNullOrEmpty(dir) ? "None" : dir;
        }

        // ── Colour helpers ────────────────────────────────────────────────────

        private static Color TempColour(float t) => t switch
        {
            < -10f => ColdColour,
            <   0f => new Color(160, 200, 255),
            <  10f => new Color(200, 220, 255),
            <  18f => ValueColour,
            <  24f => GoodColour,
            <  30f => new Color(255, 220, 120),
            <  38f => new Color(255, 160, 60),
            _      => HotColour
        };

        private static Color MoistureColour(float m) => m switch
        {
            < 0.10f => DryColour,
            < 0.30f => new Color(200, 180, 100),
            < 0.50f => ValueColour,
            < 0.70f => new Color(120, 200, 220),
            _       => WetColour
        };

        private static Color WaterTableColour(float wt) => wt switch
        {
            < 0.10f => DangerColour,
            < 0.25f => WarnColour,
            < 0.50f => ValueColour,
            _       => WetColour
        };

        private static Color ClimTempColour(float t) => t switch
        {
            < 0.25f => ColdColour,
            < 0.45f => new Color(180, 210, 255),
            < 0.60f => ValueColour,
            < 0.75f => new Color(255, 220, 120),
            _       => HotColour
        };

        private static Color ClimPrecColour(float p) => p switch
        {
            < 0.20f => DryColour,
            < 0.40f => new Color(200, 180, 100),
            < 0.55f => ValueColour,
            _       => WetColour
        };

        private static Color BiomeColour(BiomeType b) => b switch
        {
            BiomeType.Ocean     => new Color(60,  120, 200),
            BiomeType.Temperate => new Color(100, 180, 80),
            BiomeType.Desert    => new Color(220, 180, 60),
            BiomeType.Swamp     => new Color(80,  140, 80),
            BiomeType.Tundra    => new Color(180, 210, 230),
            BiomeType.Jungle    => new Color(40,  160, 60),
            BiomeType.Mountain  => new Color(180, 175, 170),
            _                   => ValueColour
        };

        private static Color GoalColour(Entities.CharacterGoal goal) => goal switch
        {
            Entities.CharacterGoal.Eating      => GoodColour,
            Entities.CharacterGoal.SeekingFood => WarnColour,
            _                                  => ValueColour
        };

        // ── Drawing helpers ───────────────────────────────────────────────────

        private void DrawRect(Rectangle rect, Color colour)
            => _sb.Draw(_pixel, rect, colour);

        private void DrawBorder(Rectangle rect, Color colour)
        {
            _sb.Draw(_pixel, new Rectangle(rect.X,         rect.Y,          rect.Width,  1),           colour);
            _sb.Draw(_pixel, new Rectangle(rect.X,         rect.Bottom - 1, rect.Width,  1),           colour);
            _sb.Draw(_pixel, new Rectangle(rect.X,         rect.Y,          1,           rect.Height), colour);
            _sb.Draw(_pixel, new Rectangle(rect.Right - 1, rect.Y,          1,           rect.Height), colour);
        }

        public void Dispose() => _pixel.Dispose();
    }
}