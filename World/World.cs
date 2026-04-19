namespace SimGame.World
{
    /// <summary>
    /// Owns the tile grid and exposes spatial queries.
    /// Also ticks food source respawn cooldowns each sim tick.
    /// </summary>
    public class World
    {
        public int Width  { get; }
        public int Height { get; }
        public int Seed   { get; }

        private readonly Tile[,] _tiles;

        public World(int width, int height, int seed = 0)
        {
            Width  = width;
            Height = height;

            var gen = new WorldGen(seed);
            Seed    = gen.Seed;
            _tiles  = gen.Generate(width, height);
        }

        public ref Tile GetTile(int x, int y)   => ref _tiles[x, y];
        public bool InBounds(int x, int y)       => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool IsWalkable(int x, int y)     => InBounds(x, y) && _tiles[x, y].IsWalkable;

        /// <summary>
        /// True if the tile has an available food source (yield > 0, not respawning).
        /// </summary>
        public bool HasFood(int x, int y)
            => InBounds(x, y) && _tiles[x, y].Food.IsAvailable;

        /// <summary>
        /// A character eats one portion from the tile. If yield hits 0,
        /// the source enters its respawn cooldown.
        /// </summary>
        public void ConsumeFood(int x, int y)
        {
            if (!InBounds(x, y)) return;
            ref var food = ref _tiles[x, y].Food;
            if (!food.IsAvailable) return;

            food.Yield--;
            if (food.Yield <= 0)
                food.RespawnTicksRemaining = FoodSource.RespawnTicks(food.Type);
        }

        /// <summary>
        /// Call once per sim tick. Counts down respawn timers and restores
        /// food sources when they're ready.
        /// </summary>
        public void Tick()
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