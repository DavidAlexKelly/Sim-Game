using System;
using Microsoft.Xna.Framework;
using SimGame.World;

namespace SimGame.Entities.AI
{
    /// <summary>
    /// Picks a random walkable tile within a short radius and returns its
    /// world-space centre. The character then moves freely toward it.
    /// Self-contained so it can be swapped without touching Character.
    /// </summary>
    public class WanderBehaviour
    {
        // How many tiles away (Chebyshev) the character will consider wandering to
        private const int WanderRadius = 4;

        public bool TryGetTarget(int tileX, int tileY, World.World world, Random rng,
                                 int tileSize, out Vector2 target)
        {
            // Collect candidate tiles in a square radius, then pick one at random
            // to avoid any directional bias.
            Span<(int x, int y)> candidates = stackalloc (int, int)[WanderRadius * WanderRadius * 4];
            int count = 0;

            for (int dx = -WanderRadius; dx <= WanderRadius; dx++)
            {
                for (int dy = -WanderRadius; dy <= WanderRadius; dy++)
                {
                    if (dx == 0 && dy == 0) continue;   // don't stand still
                    int nx = tileX + dx;
                    int ny = tileY + dy;
                    if (world.IsWalkable(nx, ny) && count < candidates.Length)
                        candidates[count++] = (nx, ny);
                }
            }

            if (count == 0)
            {
                target = default;
                return false;
            }

            var (cx, cy) = candidates[rng.Next(count)];
            target = new Vector2(cx * tileSize + tileSize * 0.5f,
                                 cy * tileSize + tileSize * 0.5f);
            return true;
        }
    }
}