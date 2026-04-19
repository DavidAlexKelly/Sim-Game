using Microsoft.Xna.Framework;
using SimGame.Core;

namespace SimGame.World
{
    /// <summary>
    /// Owns the tile grid and all world simulation systems.
    /// </summary>
    public class World
    {
        public int Width  { get; }
        public int Height { get; }
        public int Seed   { get; }

        private readonly Tile[,]           _tiles;
        private readonly TemperatureSystem _temperature;
        private readonly HydrologySystem   _hydrology;
        private readonly WaterTableSystem  _waterTable;

        public ElevationMap      ElevationMap      { get; }
        public BiomeMap          BiomeMap          { get; }
        public WindSystem        Wind              { get; }
        public WeatherSystem     Weather           { get; }
        public PlanetarySettings PlanetarySettings { get; }

        /// <summary>Human-readable terrain profile for HUD display.</summary>
        public string TerrainProfileLabel => ElevationMap.Profile.ToString();

        public World(int width, int height, TimeSystem time, int seed = 0)
        {
            Width  = width;
            Height = height;

            var gen = new WorldGen(seed);
            Seed    = gen.Seed;
            _tiles  = gen.Generate(width, height);

            ElevationMap      = gen.ElevationMap;
            BiomeMap          = gen.BiomeMap;
            PlanetarySettings = gen.PlanetarySettings;

            Wind    = new WindSystem(ElevationMap, Seed, PlanetarySettings);
            Weather = new WeatherSystem(Seed);

            _temperature = new TemperatureSystem(width, height);
            _hydrology   = new HydrologySystem(width, height, ElevationMap);
            _waterTable  = new WaterTableSystem(width, height);

            var climate = gen.BiomeMap.Climate;
            _temperature.Initialise(_tiles, time);
            _hydrology.Initialise(_tiles, climate);
            _waterTable.Initialise(_tiles, climate);
        }

        // ── Tile access ───────────────────────────────────────────────────────

        public ref Tile GetTile(int x, int y) => ref _tiles[x, y];

        public bool InBounds(int x, int y)
            => x >= 0 && y >= 0 && x < Width && y < Height;

        public bool IsWalkable(int x, int y)
            => InBounds(x, y) && _tiles[x, y].IsWalkable;

        public bool HasFood(int x, int y)
            => InBounds(x, y) && _tiles[x, y].Food.IsAvailable;

        public float GetTemperature(int x, int y)
            => InBounds(x, y) ? _tiles[x, y].Temperature : 0f;

        public float GetElevation(int x, int y)
            => InBounds(x, y) ? _tiles[x, y].Elevation : 0f;

        public float GetWindChill(int x, int y)
            => Wind.GetWindChill(x, y);

        public float GetSurfaceMoisture(int x, int y)
            => InBounds(x, y) ? _tiles[x, y].SurfaceMoisture : 0f;

        public float GetWaterTable(int x, int y)
            => InBounds(x, y) ? _tiles[x, y].WaterTable : 0f;

        public BiomeType GetBiome(int x, int y)
            => InBounds(x, y) ? _tiles[x, y].Biome : BiomeType.Ocean;

        // ── Food ──────────────────────────────────────────────────────────────

        public void ConsumeFood(int x, int y)
        {
            if (!InBounds(x, y)) return;
            ref var food = ref _tiles[x, y].Food;
            if (!food.IsAvailable) return;
            food.Yield--;
            if (food.Yield <= 0)
                food.RespawnTicksRemaining = FoodSource.RespawnTicks(food.Type);
        }

        // ── Tick ──────────────────────────────────────────────────────────────

        public void Tick(TimeSystem time)
        {
            TickFood();
            Wind.Tick(time);
            Weather.Tick(time, Wind);
            _temperature.Tick(_tiles, time, Wind);
            _hydrology.Tick(_tiles, time, Weather, Wind);
            _waterTable.Tick(_tiles, time);
            TickSnow();
        }

        private void TickSnow()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    ref var tile = ref _tiles[x, y];

                    if (Weather.SnowfallIntensity > 0f && tile.Temperature <= 0f)
                        tile.SnowCover = MathHelper.Clamp(
                            tile.SnowCover + Weather.SnowfallIntensity * 0.001f,
                            0f, 1f);

                    if (tile.Temperature > 0f && tile.SnowCover > 0f)
                        tile.SnowCover = MathHelper.Clamp(
                            tile.SnowCover - tile.Temperature * 0.0005f,
                            0f, 1f);
                }
            }
        }

        private void TickFood()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    ref var food = ref _tiles[x, y].Food;
                    if (food.Type == FoodSourceType.None) continue;
                    if (food.RespawnTicksRemaining <= 0)  continue;
                    food.RespawnTicksRemaining--;
                    if (food.RespawnTicksRemaining == 0)
                        food.Yield = FoodSource.MaxYield(food.Type);
                }
            }
        }
    }
}