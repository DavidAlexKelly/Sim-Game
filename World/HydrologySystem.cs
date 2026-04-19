using System;
using Microsoft.Xna.Framework;
using SimGame.Core;

namespace SimGame.World
{
    /// <summary>
    /// Manages surface moisture per tile.
    ///
    /// Each tick:
    ///   1. Rainfall deposits moisture on exposed tiles (rain shadow applied)
    ///   2. Snowmelt adds moisture when snow cover melts
    ///   3. Evaporation removes moisture (rate varies by tile type + temperature)
    ///   4. Flow: moisture moves downhill via ElevationMap flow direction
    ///   5. Drought counter increments on dry tiles, resets on wet ones
    ///
    /// Initialisation uses ClimateMap precipitation so wetter climate
    /// regions start with more surface moisture.
    /// </summary>
    public class HydrologySystem
    {
        private readonly int          _width;
        private readonly int          _height;
        private readonly ElevationMap _elevation;
        private readonly float[]      _flowBuffer;

        // Evaporation rates per tile type (fraction of moisture lost per tick)
        private static readonly float[] EvapRate =
        {
            0.001f, // DeepWater
            0.002f, // ShallowWater
            0.015f, // Sand
            0.006f, // Grass
            0.004f, // Forest
            0.008f, // Stone
            0.010f, // Mountain
            0.018f, // DesertSand
            0.016f, // DesertRock
            0.003f, // Oasis
            0.002f, // SwampWater
            0.004f, // SwampGrass
            0.003f, // SwampForest
            0.007f, // Tundra
            0.009f, // TundraRock
            0.001f, // FrozenWater
            0.004f, // JungleFloor
            0.003f, // JungleUndergrowth
            0.002f, // JungleCanopy
            0.009f, // Alpine
            0.005f, // Snowcap
        };

        // How much rainfall each tile type absorbs vs runs off
        // 0 = all runoff, 1 = all absorbed
        private static readonly float[] AbsorptionRate =
        {
            0.10f, // DeepWater
            0.15f, // ShallowWater
            0.30f, // Sand
            0.70f, // Grass
            0.85f, // Forest
            0.40f, // Stone
            0.25f, // Mountain
            0.10f, // DesertSand
            0.15f, // DesertRock
            0.80f, // Oasis
            0.20f, // SwampWater
            0.75f, // SwampGrass
            0.90f, // SwampForest
            0.40f, // Tundra
            0.30f, // TundraRock
            0.05f, // FrozenWater
            0.90f, // JungleFloor
            0.92f, // JungleUndergrowth
            0.88f, // JungleCanopy
            0.35f, // Alpine
            0.10f, // Snowcap
        };

        // Tiles that should never evaporate below their natural baseline
        private static bool IsWaterTile(TileType t) =>
            t == TileType.DeepWater    ||
            t == TileType.ShallowWater ||
            t == TileType.SwampWater   ||
            t == TileType.FrozenWater;

        public HydrologySystem(int width, int height, ElevationMap elevation)
        {
            _width      = width;
            _height     = height;
            _elevation  = elevation;
            _flowBuffer = new float[width * height];
        }

        // ── Initialisation ────────────────────────────────────────────────────

        /// <summary>
        /// Initialises surface moisture from biome base moisture blended
        /// with the climate precipitation value so wetter climate regions
        /// start with more moisture.
        /// </summary>
        public void Initialise(Tile[,] tiles, ClimateMap climate)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    float biomeMoisture = BiomeData.BaseMoisture(tiles[x, y].Biome);
                    float climatePrec   = climate.GetPrecipitation(x, y);

                    // 60% biome character, 40% climate precipitation
                    tiles[x, y].SurfaceMoisture = MathHelper.Clamp(
                        biomeMoisture * 0.6f + climatePrec * 0.4f,
                        0f, 1f);

                    tiles[x, y].DroughtTicks = 0;
                }
            }
        }

        // ── Tick ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Called once per sim tick.
        /// </summary>
        public void Tick(
            Tile[,]       tiles,
            TimeSystem    time,
            WeatherSystem weather,
            WindSystem    wind)
        {
            ApplyRainfall(tiles, weather, wind);
            ApplySnowmelt(tiles, time, weather);
            ApplyEvaporation(tiles, weather);
            ApplyFlow(tiles);
            UpdateDroughtCounters(tiles, weather);
        }

        // ── Private steps ─────────────────────────────────────────────────────

        private void ApplyRainfall(
            Tile[,]       tiles,
            WeatherSystem weather,
            WindSystem    wind)
        {
            if (weather.RainfallIntensity <= 0f) return;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    // Rain shadow: tiles sheltered from wind get less rain
                    float exposure   = wind.GetExposure(x, y);
                    float rainAmount = weather.RainfallIntensity
                                     * MathHelper.Lerp(0.2f, 1.0f, exposure);

                    int   typeIndex = (int)tiles[x, y].Type;
                    float absorbed  = rainAmount * AbsorptionRate[typeIndex];

                    tiles[x, y].SurfaceMoisture = MathHelper.Clamp(
                        tiles[x, y].SurfaceMoisture + absorbed, 0f, 1f);
                }
            }
        }

        private void ApplySnowmelt(
            Tile[,]       tiles,
            TimeSystem    time,
            WeatherSystem weather)
        {
            // Snow melts in spring or when weather is not Snow and temp > 0
            if (time.Season != 0 && weather.Current == WeatherState.Snow) return;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (tiles[x, y].SnowCover <= 0f) continue;

                    // Melt rate scales with temperature above freezing
                    float meltRate = MathHelper.Clamp(
                        tiles[x, y].Temperature * 0.001f, 0f, 0.05f);

                    float melted = MathHelper.Min(tiles[x, y].SnowCover, meltRate);

                    tiles[x, y].SnowCover       -= melted;
                    tiles[x, y].SurfaceMoisture  = MathHelper.Clamp(
                        tiles[x, y].SurfaceMoisture + melted * 0.5f, 0f, 1f);
                }
            }
        }

        private void ApplyEvaporation(Tile[,] tiles, WeatherSystem weather)
        {
            // Weather reduces evaporation rate
            float evapModifier = weather.Current switch
            {
                WeatherState.Rain   => 0.3f,
                WeatherState.Storm  => 0.2f,
                WeatherState.Snow   => 0.1f,
                WeatherState.Cloudy => 0.7f,
                _                   => 1.0f
            };

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    ref var tile = ref tiles[x, y];

                    // Water tiles don't evaporate below their baseline
                    if (IsWaterTile(tile.Type)) continue;

                    int   typeIndex  = (int)tile.Type;
                    float rate       = EvapRate[typeIndex] * evapModifier;

                    // Higher temperature = faster evaporation
                    float tempFactor = MathHelper.Clamp(tile.Temperature / 40f, 0f, 2f);
                    rate *= (1f + tempFactor);

                    // Desert tiles evaporate even faster in hot conditions
                    if (tile.Biome == BiomeType.Desert)
                        rate *= 1.4f;

                    // Jungle canopy retains moisture — slower evaporation
                    if (tile.Biome == BiomeType.Jungle)
                        rate *= 0.6f;

                    tile.SurfaceMoisture = MathHelper.Clamp(
                        tile.SurfaceMoisture - rate, 0f, 1f);
                }
            }
        }

        private void ApplyFlow(Tile[,] tiles)
        {
            // Copy current moisture into buffer
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                    _flowBuffer[x * _height + y] = tiles[x, y].SurfaceMoisture;

            // Flow rate — fraction of excess moisture that moves downhill per tick
            const float FlowRate    = 0.02f;
            const float FlowMinimum = 0.10f; // don't flow if below this level

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    float moisture = tiles[x, y].SurfaceMoisture;
                    if (moisture < FlowMinimum) continue;

                    var dir = _elevation.GetFlowDirection(x, y);
                    if (!dir.HasValue) continue; // local minimum — water pools here

                    int nx = x + dir.Value.dx;
                    int ny = y + dir.Value.dy;
                    if (nx < 0 || ny < 0 || nx >= _width || ny >= _height) continue;

                    // Swamp tiles resist flow — they hold water
                    float tileFlowRate = tiles[x, y].Biome == BiomeType.Swamp
                        ? FlowRate * 0.3f
                        : FlowRate;

                    float flowAmount = moisture * tileFlowRate;
                    _flowBuffer[x  * _height + y ] -= flowAmount;
                    _flowBuffer[nx * _height + ny] += flowAmount;
                }
            }

            // Write buffer back, clamped
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    tiles[x, y].SurfaceMoisture = MathHelper.Clamp(
                        _flowBuffer[x * _height + y], 0f, 1f);
                }
            }
        }

        private void UpdateDroughtCounters(Tile[,] tiles, WeatherSystem weather)
        {
            bool isRaining = weather.RainfallIntensity > 0f;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    ref var tile = ref tiles[x, y];

                    if (isRaining && tile.SurfaceMoisture > 0.2f)
                    {
                        // Reset drought counter when it rains and tile is moist
                        tile.DroughtTicks = 0;
                    }
                    else if (tile.SurfaceMoisture < 0.15f)
                    {
                        // Increment drought counter when surface is dry
                        // Desert tiles accumulate drought faster
                        tile.DroughtTicks += tile.Biome == BiomeType.Desert ? 2 : 1;
                    }
                }
            }
        }
    }
}