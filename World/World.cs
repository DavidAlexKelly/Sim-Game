namespace SimGame.World
{
    /// <summary>
    /// Owns the tile grid and exposes spatial queries.
    /// Does not know how to generate itself — use WorldGen for that.
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
    }
}
