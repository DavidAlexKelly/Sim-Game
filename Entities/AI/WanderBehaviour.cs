using System;
using SimGame.World;

namespace SimGame.Entities.AI
{
    /// <summary>
    /// Picks a random adjacent walkable tile each tick.
    /// Self-contained so it can be swapped out for pathfinding later
    /// without touching Character.
    /// </summary>
    public class WanderBehaviour
    {
        private static readonly (int dx, int dy)[] Directions =
        {
            (0, -1), (1, 0), (0, 1), (-1, 0)
        };

        public bool TryGetNextMove(int x, int y, World.World world, Random rng,
                                   out int nextX, out int nextY)
        {
            // Copy directions and shuffle to avoid directional bias
            var dirs = (Span<(int, int)>)stackalloc (int, int)[4];
            for (int i = 0; i < 4; i++) dirs[i] = Directions[i];

            for (int i = 3; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
            }

            foreach (var (dx, dy) in dirs)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (world.IsWalkable(nx, ny))
                {
                    nextX = nx;
                    nextY = ny;
                    return true;
                }
            }

            nextX = x;
            nextY = y;
            return false;
        }
    }
}
