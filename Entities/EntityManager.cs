using System;
using System.Collections.Generic;
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

        /// <summary>Called once per sim tick.</summary>
        public void Tick(World.World world)
        {
            foreach (var c in _characters)
                c.Tick(world, _rng, _tileSize);
        }

        /// <summary>Called every frame to smooth visual positions.</summary>
        public void UpdateRenderPositions(float deltaSeconds)
        {
            foreach (var c in _characters)
                c.UpdateRenderPos(deltaSeconds);
        }
    }
}
