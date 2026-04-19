using System;
using Microsoft.Xna.Framework;

namespace SimGame.World
{
    /// <summary>
    /// Generates the Tile[,] grid using:
    ///   - Domain-warped elevation with hydraulic erosion
    ///   - Voronoi biome regions with border blending
    ///   - Physically-derived climate from planetary position
    ///   - Water boundaries are always hard elevation cuts, never blended
    /// </summary>
    public class WorldGen
    {
        private readonly FastNoiseLite _detailNoise;
        private readonly Random        _rng;

        public int               Seed              { get; }
        public ElevationMap      ElevationMap      { get; private set; } = null!;
        public BiomeMap          BiomeMap          { get; private set; } = null!;
        public PlanetarySettings PlanetarySettings { get; private set; } = null!;

        private const float BerryBushChance    = 0.06f;
        private const float FruitTreeChance    = 0.08f;
        private const float CactusFruitChance  = 0.04f;
        private const float SwampBerryChance   = 0.07f;
        private const float TundraLichenChance = 0.05f;
        private const float JungleFruitChance  = 0.12f;

        public WorldGen(int seed = 0)
        {
            Seed = seed == 0 ? new Random().Next(1, int.MaxValue) : seed;
            _rng = new Random(Seed);

            _detailNoise = new FastNoiseLite(Seed + 100);
            _detailNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            _detailNoise.SetFrequency(0.045f);
            _detailNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            _detailNoise.SetFractalOctaves(3);
        }

        public Tile[,] Generate(int width, int height)
        {
            ElevationMap      = new ElevationMap(width, height, Seed);
            PlanetarySettings = new PlanetarySettings(height, Seed);
            BiomeMap          = new BiomeMap(
                width, height, Seed, ElevationMap, PlanetarySettings);

            var tiles = new Tile[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float     elev   = ElevationMap.Get(x, y);
                    float     detail = (_detailNoise.GetNoise(x, y) + 1f) * 0.5f;
                    float     prec   = BiomeMap.Climate.GetPrecipitation(x, y);

                    var (primaryBiome, secondaryBiome, blend) =
                        BiomeMap.GetBlend(x, y);

                    TileType type = SelectTileBlended(
                        primaryBiome, secondaryBiome, blend,
                        elev, detail, prec);

                    tiles[x, y]           = Tile.FromType(type, primaryBiome);
                    tiles[x, y].Elevation = elev;
                    tiles[x, y].Food      = SpawnFood(type, primaryBiome);
                }
            }

            return tiles;
        }

        // ── Tile selection ────────────────────────────────────────────────────

        private TileType SelectTileBlended(
            BiomeType primaryBiome,
            BiomeType secondaryBiome,
            float     blend,
            float     elev,
            float     detail,
            float     prec)
        {
            // Hard water floor — elevation always wins, never blended
            if (elev < 0.28f) return TileType.DeepWater;
            if (elev < 0.35f) return TileType.ShallowWater;

            TileType primary = SelectTile(primaryBiome, elev, detail, prec);

            // If primary resolved to a water tile, return immediately
            // Water boundaries are always hard — never blend
            if (IsWaterTile(primary)) return primary;

            // No blend needed
            if (blend <= 0f || primaryBiome == secondaryBiome)
                return primary;

            TileType secondary = SelectTile(secondaryBiome, elev, detail, prec);

            // Never blend toward a water tile type
            if (IsWaterTile(secondary)) return primary;

            // Blend at border — randomly pick secondary based on blend weight
            if (_rng.NextSingle() < blend * 0.6f)
                return secondary;

            return primary;
        }

        private static bool IsWaterTile(TileType t) =>
            t == TileType.DeepWater    ||
            t == TileType.ShallowWater ||
            t == TileType.SwampWater   ||
            t == TileType.FrozenWater;

        private static TileType SelectTile(
            BiomeType biome,
            float     elev,
            float     detail,
            float     prec)
        {
            // Hard water floors
            if (elev < 0.28f) return TileType.DeepWater;
            if (elev < 0.35f) return TileType.ShallowWater;

            return biome switch
            {
                BiomeType.Ocean     => SelectOcean(elev),
                BiomeType.Temperate => SelectTemperate(elev, detail, prec),
                BiomeType.Desert    => SelectDesert(elev, detail),
                BiomeType.Swamp     => SelectSwamp(elev, detail),
                BiomeType.Tundra    => SelectTundra(elev, detail),
                BiomeType.Jungle    => SelectJungle(elev, detail),
                BiomeType.Mountain  => SelectMountain(elev, detail),
                _                   => TileType.Grass
            };
        }

        private static TileType SelectOcean(float elev) => elev switch
        {
            < 0.30f => TileType.DeepWater,
            _       => TileType.ShallowWater
        };

        private static TileType SelectTemperate(
            float elev,
            float detail,
            float prec)
        {
            if (elev < 0.42f) return TileType.Sand;

            if (elev < 0.62f)
            {
                // Wetter areas have more forest
                float forestThreshold = MathHelper.Lerp(0.70f, 0.35f, prec);
                return detail > forestThreshold ? TileType.Forest : TileType.Grass;
            }

            if (elev < 0.72f)
                return detail < 0.45f ? TileType.Forest : TileType.Stone;

            return elev < 0.82f ? TileType.Stone : TileType.Mountain;
        }

        private static TileType SelectDesert(float elev, float detail) => elev switch
        {
            < 0.42f => detail < 0.12f ? TileType.Oasis      : TileType.DesertSand,
            < 0.65f => detail < 0.22f ? TileType.DesertRock : TileType.DesertSand,
            < 0.78f => TileType.DesertRock,
            _       => TileType.Mountain
        };

        private static TileType SelectSwamp(float elev, float detail) => elev switch
        {
            < 0.40f => TileType.SwampWater,
            < 0.48f => detail < 0.45f ? TileType.SwampWater  : TileType.SwampGrass,
            < 0.65f => detail < 0.38f ? TileType.SwampGrass  : TileType.SwampForest,
            < 0.74f => TileType.SwampForest,
            _       => TileType.Stone
        };

        private static TileType SelectTundra(float elev, float detail) => elev switch
        {
            < 0.38f => TileType.FrozenWater,
            < 0.50f => detail < 0.28f ? TileType.FrozenWater : TileType.Tundra,
            < 0.68f => detail < 0.22f ? TileType.TundraRock  : TileType.Tundra,
            < 0.78f => TileType.TundraRock,
            _       => TileType.Snowcap
        };

        private static TileType SelectJungle(float elev, float detail) => elev switch
        {
            < 0.40f => TileType.ShallowWater,
            < 0.50f => detail < 0.28f ? TileType.JungleFloor       : TileType.JungleUndergrowth,
            < 0.68f => detail < 0.38f ? TileType.JungleUndergrowth : TileType.JungleCanopy,
            < 0.76f => TileType.JungleCanopy,
            _       => TileType.Stone
        };

        private static TileType SelectMountain(float elev, float detail) => elev switch
        {
            < 0.42f => TileType.Sand,
            < 0.55f => detail < 0.42f ? TileType.Alpine : TileType.Stone,
            < 0.70f => TileType.Stone,
            < 0.80f => TileType.Alpine,
            _       => TileType.Snowcap
        };

        // ── Food spawning ─────────────────────────────────────────────────────

        private FoodSource SpawnFood(TileType type, BiomeType biome) => type switch
        {
            // Temperate
            TileType.Grass when _rng.NextSingle() < BerryBushChance
                => FoodSource.Create(FoodSourceType.BerryBush),
            TileType.Forest when _rng.NextSingle() < FruitTreeChance
                => FoodSource.Create(FoodSourceType.FruitTree),

            // Desert
            TileType.DesertSand when _rng.NextSingle() < CactusFruitChance
                => FoodSource.Create(FoodSourceType.CactusFruit),
            TileType.Oasis when _rng.NextSingle() < 0.35f
                => FoodSource.Create(FoodSourceType.FruitTree),

            // Swamp
            TileType.SwampGrass when _rng.NextSingle() < SwampBerryChance
                => FoodSource.Create(FoodSourceType.SwampBerry),
            TileType.SwampForest when _rng.NextSingle() < FruitTreeChance
                => FoodSource.Create(FoodSourceType.FruitTree),

            // Tundra
            TileType.Tundra when _rng.NextSingle() < TundraLichenChance
                => FoodSource.Create(FoodSourceType.TundraLichen),

            // Jungle
            TileType.JungleFloor when _rng.NextSingle() < JungleFruitChance
                => FoodSource.Create(FoodSourceType.JungleFruit),
            TileType.JungleUndergrowth when _rng.NextSingle() < JungleFruitChance
                => FoodSource.Create(FoodSourceType.JungleFruit),
            TileType.JungleCanopy when _rng.NextSingle() < FruitTreeChance
                => FoodSource.Create(FoodSourceType.FruitTree),

            // Mountain
            TileType.Alpine when _rng.NextSingle() < BerryBushChance * 0.5f
                => FoodSource.Create(FoodSourceType.BerryBush),

            _ => FoodSource.Empty
        };
    }
}