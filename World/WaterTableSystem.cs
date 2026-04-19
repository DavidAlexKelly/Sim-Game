using Microsoft.Xna.Framework;
using SimGame.Core;

namespace SimGame.World
{
    /// <summary>
    /// Manages underground water table per tile (0-1).
    ///
    /// The water table is a hidden subsurface moisture value.
    /// It recharges slowly from surface moisture percolating down and
    /// depletes through evapotranspiration and plant uptake.
    ///
    /// High water table:
    ///   - Keeps surface moisture from dropping too low (capillary rise)
    ///   - Supports food growth even during dry spells
    ///   - Creates natural wetland / oasis areas
    ///
    /// Low water table (drought):
    ///   - Surface dries out faster
    ///   - Food sources struggle to regrow
    ///   - Will trigger terrain mutation in Stage 3
    ///
    /// Initialisation uses ClimateMap precipitation so wetter climate
    /// regions start with a higher water table.
    /// </summary>
    public class WaterTableSystem
    {
        private readonly int     _width;
        private readonly int     _height;
        private readonly float[] _lateralBuffer;

        // How fast surface moisture percolates into the water table per tick
        private static readonly float[] PercolationRate =
        {
            0.000f, // DeepWater    — already saturated
            0.000f, // ShallowWater — already saturated
            0.008f, // Sand
            0.004f, // Grass
            0.003f, // Forest
            0.002f, // Stone
            0.001f, // Mountain
            0.010f, // DesertSand   — fast percolation, drains quickly
            0.006f, // DesertRock
            0.005f, // Oasis
            0.000f, // SwampWater   — already saturated
            0.002f, // SwampGrass
            0.002f, // SwampForest
            0.003f, // Tundra
            0.002f, // TundraRock
            0.000f, // FrozenWater  — frozen, no percolation
            0.004f, // JungleFloor
            0.003f, // JungleUndergrowth
            0.002f, // JungleCanopy
            0.002f, // Alpine
            0.001f, // Snowcap
        };

        // Natural depletion per tick (evapotranspiration + plant uptake)
        private static readonly float[] DepletionRate =
        {
            0.000f, // DeepWater
            0.000f, // ShallowWater
            0.002f, // Sand
            0.003f, // Grass        — plants draw water up
            0.004f, // Forest       — trees draw heavily
            0.001f, // Stone
            0.001f, // Mountain
            0.003f, // DesertSand   — high depletion, little recharge
            0.002f, // DesertRock
            0.005f, // Oasis        — high plant uptake
            0.000f, // SwampWater
            0.002f, // SwampGrass
            0.003f, // SwampForest
            0.001f, // Tundra       — minimal plant activity
            0.001f, // TundraRock
            0.000f, // FrozenWater
            0.006f, // JungleFloor  — very high plant uptake
            0.005f, // JungleUndergrowth
            0.004f, // JungleCanopy
            0.001f, // Alpine
            0.000f, // Snowcap
        };

        // Fraction of water table difference that equalises between
        // neighbours per tick — creates gradients from water bodies outward
        private const float LateralFlowRate = 0.001f;

        public WaterTableSystem(int width, int height)
        {
            _width         = width;
            _height        = height;
            _lateralBuffer = new float[width * height];
        }

        // ── Initialisation ────────────────────────────────────────────────────

        /// <summary>
        /// Initialises water table from biome base blended with climate
        /// precipitation, then runs lateral flow iterations to smooth
        /// the initial state so water bodies raise their neighbours.
        /// </summary>
        public void Initialise(Tile[,] tiles, ClimateMap climate)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    float biomeWt  = BiomeData.BaseWaterTable(tiles[x, y].Biome);
                    float climPrec = climate.GetPrecipitation(x, y);

                    // 65% biome character, 35% climate precipitation
                    tiles[x, y].WaterTable = MathHelper.Clamp(
                        biomeWt * 0.65f + climPrec * 0.35f,
                        0f, 1f);
                }
            }

            // Run lateral flow iterations to smooth the initial state
            // so water tiles raise their neighbours naturally
            for (int iter = 0; iter < 20; iter++)
                ApplyLateralFlow(tiles);
        }

        // ── Tick ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Called once per sim tick.
        /// </summary>
        public void Tick(Tile[,] tiles, TimeSystem time)
        {
            ApplyPercolation(tiles);
            ApplyDepletion(tiles, time);
            ApplyLateralFlow(tiles);
            ApplyCapillaryRise(tiles);
        }

        // ── Private steps ─────────────────────────────────────────────────────

        /// <summary>
        /// Surface moisture percolates down into the water table when
        /// the surface has excess moisture above a threshold.
        /// </summary>
        private void ApplyPercolation(Tile[,] tiles)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    ref var tile      = ref tiles[x, y];
                    int     typeIndex = (int)tile.Type;
                    float   rate      = PercolationRate[typeIndex];
                    if (rate <= 0f) continue;

                    // Only percolate when surface has excess moisture
                    float excess     = MathHelper.Max(0f, tile.SurfaceMoisture - 0.3f);
                    float percolated = excess * rate;

                    tile.SurfaceMoisture = MathHelper.Clamp(
                        tile.SurfaceMoisture - percolated, 0f, 1f);
                    tile.WaterTable = MathHelper.Clamp(
                        tile.WaterTable + percolated, 0f, 1f);
                }
            }
        }

        /// <summary>
        /// Plants and evapotranspiration deplete the water table.
        /// Depletion is faster in summer and when the surface is dry
        /// (plants draw harder from underground when surface is dry).
        /// </summary>
        private void ApplyDepletion(Tile[,] tiles, TimeSystem time)
        {
            // Season modifier
            float seasonMod = time.Season switch
            {
                1 => 1.5f, // Summer — maximum plant activity
                3 => 0.3f, // Winter — minimal plant activity
                0 => 0.8f, // Spring — moderate
                _ => 1.0f  // Autumn
            };

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    ref var tile      = ref tiles[x, y];
                    int     typeIndex = (int)tile.Type;
                    float   rate      = DepletionRate[typeIndex] * seasonMod;
                    if (rate <= 0f) continue;

                    // Plants draw harder from underground when surface is dry
                    float surfaceDryFactor = 1f + (1f - tile.SurfaceMoisture);
                    rate *= surfaceDryFactor;

                    // Jungle draws even more aggressively
                    if (tile.Biome == BiomeType.Jungle)
                        rate *= 1.3f;

                    // Tundra has minimal plant uptake year-round
                    if (tile.Biome == BiomeType.Tundra)
                        rate *= 0.4f;

                    tile.WaterTable = MathHelper.Clamp(
                        tile.WaterTable - rate, 0f, 1f);
                }
            }
        }

        /// <summary>
        /// Water table equalises slowly between neighbours.
        /// Creates natural gradients from water bodies outward into land.
        /// Uses a two-pass approach so all tiles equalise simultaneously.
        /// </summary>
        private void ApplyLateralFlow(Tile[,] tiles)
        {
            // Copy into buffer
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                    _lateralBuffer[x * _height + y] = tiles[x, y].WaterTable;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    float wt    = tiles[x, y].WaterTable;
                    float delta = 0f;
                    int   count = 0;

                    Accumulate(tiles, x - 1, y, wt, ref delta, ref count);
                    Accumulate(tiles, x + 1, y, wt, ref delta, ref count);
                    Accumulate(tiles, x, y - 1, wt, ref delta, ref count);
                    Accumulate(tiles, x, y + 1, wt, ref delta, ref count);

                    if (count > 0)
                        _lateralBuffer[x * _height + y] +=
                            delta * LateralFlowRate;
                }
            }

            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                    tiles[x, y].WaterTable = MathHelper.Clamp(
                        _lateralBuffer[x * _height + y], 0f, 1f);
        }

        /// <summary>
        /// When the water table is high, it pushes moisture back up to
        /// the surface (capillary rise). This keeps wetland areas moist
        /// even without rain and creates natural oasis effects.
        /// </summary>
        private void ApplyCapillaryRise(Tile[,] tiles)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    ref var tile = ref tiles[x, y];

                    // Only rises when water table is high and surface is dry
                    float excess  = MathHelper.Max(0f, tile.WaterTable - 0.6f);
                    float deficit = MathHelper.Max(0f, 0.4f - tile.SurfaceMoisture);
                    float rise    = MathHelper.Min(excess, deficit) * 0.01f;

                    // Swamp tiles have stronger capillary rise
                    if (tile.Biome == BiomeType.Swamp)
                        rise *= 2f;

                    // Desert tiles have weaker capillary rise (sandy soil)
                    if (tile.Biome == BiomeType.Desert)
                        rise *= 0.5f;

                    tile.WaterTable      = MathHelper.Clamp(tile.WaterTable - rise, 0f, 1f);
                    tile.SurfaceMoisture = MathHelper.Clamp(tile.SurfaceMoisture + rise, 0f, 1f);
                }
            }
        }

        private void Accumulate(
            Tile[,] tiles,
            int     nx,
            int     ny,
            float   myWt,
            ref float delta,
            ref int   count)
        {
            if (nx < 0 || ny < 0 || nx >= _width || ny >= _height) return;
            delta += tiles[nx, ny].WaterTable - myWt;
            count++;
        }
    }
}