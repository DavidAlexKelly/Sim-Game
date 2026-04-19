using System;
using System.Collections.Generic;
using SimGame.World;

namespace SimGame.Entities.AI
{
    /// <summary>
    /// Finds the nearest tile matching a predicate via BFS, then returns
    /// the first step toward it. Stateless — recalculates each tick, which
    /// is fine at this scale and means it reacts instantly to world changes.
    ///
    /// Swap out the BFS for A* here later without touching Character at all.
    /// </summary>
    public class SeekTileBehaviour
    {
        private const int MaxSearchRadius = 40; // tiles; keeps BFS cheap

        public bool TryStepToward(
            int startX, int startY,
            World.World world,
            Random rng,
            Func<World.World, int, int, bool> isTarget,
            out int nextX, out int nextY)
        {
            // ── BFS to find nearest target tile ──────────────────────────────
            var visited = new HashSet<(int, int)>();
            var queue   = new Queue<(int x, int y, int firstStepX, int firstStepY)>();

            visited.Add((startX, startY));

            // Seed the queue with immediate neighbours, shuffled to avoid
            // directional bias when multiple targets are equidistant
            var dirs = new (int dx, int dy)[] { (0,-1),(1,0),(0,1),(-1,0) };
            Shuffle(dirs, rng);

            foreach (var (dx, dy) in dirs)
            {
                int nx = startX + dx, ny = startY + dy;
                if (!world.IsWalkable(nx, ny)) continue;
                if (visited.Add((nx, ny)))
                    queue.Enqueue((nx, ny, nx, ny));
            }

            while (queue.Count > 0)
            {
                var (cx, cy, fx, fy) = queue.Dequeue();

                // Manhattan distance check — keep search bounded
                if (Math.Abs(cx - startX) + Math.Abs(cy - startY) > MaxSearchRadius)
                    continue;

                if (isTarget(world, cx, cy))
                {
                    nextX = fx;
                    nextY = fy;
                    return true;
                }

                foreach (var (dx, dy) in dirs)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if (!world.IsWalkable(nx, ny)) continue;
                    if (visited.Add((nx, ny)))
                        queue.Enqueue((nx, ny, fx, fy));
                }
            }

            nextX = startX;
            nextY = startY;
            return false;
        }

        private static void Shuffle<T>(T[] arr, Random rng)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }
}