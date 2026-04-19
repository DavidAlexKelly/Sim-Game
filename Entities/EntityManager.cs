using System;
using System.Collections.Generic;
using SimGame.Core;
using SimGame.Rendering;
using SimGame.World;

namespace SimGame.Entities
{
    /// <summary>
    /// Owns all entities. Responsible for spawning, ticking, and
    /// updating render positions each frame.
    /// </summary>
    public class EntityManager
    {
        private readonly List<Character> _characters = new();
        private readonly Random          _rng;
        private readonly int             _tileSize;

        public IReadOnlyList<Character> Characters => _characters;

        public EntityManager(int tileSize, int seed = 42)
        {
            _tileSize = tileSize;
            _rng      = new Random(seed);
        }

        public void SpawnCharacters(int count, World.World world)
        {
            _characters.Clear();
            int placed = 0, attempts = 0;
            while (placed < count && attempts < count * 20)
            {
                int x = _rng.Next(world.Width);
                int y = _rng.Next(world.Height);
                if (world.IsWalkable(x, y))
                {
                    _characters.Add(new Character(placed + 1, x, y, _tileSize));
                    placed++;
                }
                attempts++;
            }
        }

        /// <summary>
        /// Advances the world and all characters by one sim tick.
        /// TimeSystem is passed through so World can update temperature.
        /// </summary>
        public void Tick(World.World world, Renderer renderer, TimeSystem time)
        {
            world.Tick(time);

            foreach (var c in _characters)
                c.Tick(world, _rng, _tileSize);

            renderer.BakeFoodOverlay(world);
        }

        public void UpdateRenderPositions(float deltaSeconds, float speedMultiplier)
        {
            foreach (var c in _characters)
                c.UpdateRenderPos(deltaSeconds, speedMultiplier);
        }
    }
}