using System;
using System.Collections.Generic;
using SimGame.World;

namespace SimGame.Entities.AI
{
    /// <summary>
    /// Finds the nearest tile matching a predicate via BFS and returns its
    /// tile coordinates. The caller (Character) converts to a world-space
    /// position and moves freely toward it at their own speed.
    ///
    /// Previously this returned only the *first step* toward the target,
    /// forcing grid-snapped movement. Now it returns the *goal tile* so
    /// the character can travel in a straight line.
    /// </summary>
    public class SeekTileBehaviour
    {
        private const int MaxSearchRadius = 40;

        public bool TryGetTargetTile(
            int startX, int startY,
            World.World world,
            Random rng,
            Func<World.World, int, int, bool> isTarget,
            out int goalX, out int goalY)
        {
            var visited = new HashSet<(int, int)>();
            var queue   = new Queue<(int x, int y)>();

            visited.Add((startX, startY));

            var dirs = new (int dx, int dy)[] { (0,-1),(1,0),(0,1),(-1,0) };
            Shuffle(dirs, rng);

            foreach (var (dx, dy) in dirs)
            {
                int nx = startX + dx, ny = startY + dy;
                if (!world.IsWalkable(nx, ny)) continue;
                if (visited.Add((nx, ny)))
                    queue.Enqueue((nx, ny));
            }

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();

                if (Math.Abs(cx - startX) + Math.Abs(cy - startY) > MaxSearchRadius)
                    continue;

                if (isTarget(world, cx, cy))
                {
                    goalX = cx;
                    goalY = cy;
                    return true;
                }

                foreach (var (dx, dy) in dirs)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if (!world.IsWalkable(nx, ny)) continue;
                    if (visited.Add((nx, ny)))
                        queue.Enqueue((nx, ny));
                }
            }

            goalX = startX;
            goalY = startY;
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