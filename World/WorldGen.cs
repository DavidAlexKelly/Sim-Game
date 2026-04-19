using System;

namespace SimGame.World
{
    /// <summary>
    /// Responsible only for generating a Tile[,] grid.
    /// Knows nothing about rendering or entities.
    /// </summary>
    public class WorldGen
    {
        private readonly FastNoiseLite _noise;
        private readonly Random        _rng;

        public int Seed { get; }

        // Probability that a qualifying terrain tile gets a food source
        private const float BerryBushChance = 0.06f;   // 6% of Grass tiles
        private const float FruitTreeChance = 0.08f;   // 8% of Forest tiles

        public WorldGen(int seed = 0)
        {
            Seed = seed == 0 ? new Random().Next(1, int.MaxValue) : seed;

            _rng = new Random(Seed);

            _noise = new FastNoiseLite(Seed);
            _noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            _noise.SetFrequency(0.035f);
            _noise.SetFractalType(FastNoiseLite.FractalType.FBm);
            _noise.SetFractalOctaves(4);
        }

        public Tile[,] Generate(int width, int height)
        {
            var tiles = new Tile[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float n = (_noise.GetNoise(x, y) + 1f) * 0.5f;
                    tiles[x, y] = Tile.FromType(HeightToTile(n));

                    // Scatter food sources on walkable terrain
                    ref var tile = ref tiles[x, y];
                    tile.Food = tile.Type switch
                    {
                        TileType.Grass  when _rng.NextSingle() < BerryBushChance
                            => FoodSource.Create(FoodSourceType.BerryBush),
                        TileType.Forest when _rng.NextSingle() < FruitTreeChance
                            => FoodSource.Create(FoodSourceType.FruitTree),
                        _ => FoodSource.Empty
                    };
                }
            }

            return tiles;
        }

        private static TileType HeightToTile(float h) => h switch
        {
            < 0.25f => TileType.DeepWater,
            < 0.35f => TileType.ShallowWater,
            < 0.42f => TileType.Sand,
            < 0.62f => TileType.Grass,
            < 0.72f => TileType.Forest,
            < 0.82f => TileType.Stone,
            _        => TileType.Mountain
        };
    }
}