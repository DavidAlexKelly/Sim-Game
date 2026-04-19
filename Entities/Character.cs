using System;
using Microsoft.Xna.Framework;
using SimGame.Entities.AI;
using SimGame.World;

namespace SimGame.Entities
{
    public enum CharacterGoal
    {
        Idle,
        SeekingFood,
        Eating
    }

    public enum CharacterState { Idle, Moving }

    public class Character
    {
        // ── Identity ────────────────────────────────────────────────────────
        public int    Id   { get; }
        public string Name { get; }

        // ── Needs ───────────────────────────────────────────────────────────
        public float Hunger { get; private set; }

        private const float HungerRisePerTick   = 0.004f;
        private const float HungerEatPerTick    = 0.05f;
        private const float HungerSeekThreshold = 0.40f;
        private const float HungerSatisfied     = 0.10f;

        // ── Goal / State ────────────────────────────────────────────────────
        public CharacterGoal  Goal  { get; private set; } = CharacterGoal.Idle;
        public CharacterState State { get; private set; } = CharacterState.Idle;

        // ── Position & movement ──────────────────────────────────────────────
        /// <summary>World-pixel position (centre of character). Updated every frame.</summary>
        public Vector2 Position { get; private set; }

        /// <summary>
        /// Base movement speed in world-pixels per second at 1x tick speed.
        /// Scaled by the tick speed multiplier each frame so the simulation
        /// looks faster/slower as expected.
        /// </summary>
        public float BasePixelsPerSecond { get; }

        /// <summary>Current destination in world pixels, set each tick by AI.</summary>
        private Vector2 _targetPos;

        private const float ArrivalThreshold = 1.5f;

        // ── Behaviours ───────────────────────────────────────────────────────
        private readonly WanderBehaviour   _wander = new();
        private readonly SeekTileBehaviour _seeker = new();

        /// <summary>Equals Position — kept so EntityRenderer doesn't need changing.</summary>
        public Vector2 RenderPos => Position;

        private static readonly string[] NamePool =
        {
            "Aldric", "Berra", "Corvin", "Dwyn",  "Erlan",
            "Fara",   "Gorm",  "Hilde",  "Ilvar", "Joryn",
            "Kael",   "Lyra",  "Morn",   "Neva",  "Osric"
        };

        public Character(int id, int startX, int startY, int tileSize)
        {
            Id   = id;
            Name = NamePool[new Random(id * 7).Next(NamePool.Length)] + " " + id;

            // 1–3 tiles per second at 1x speed, varied per character
            BasePixelsPerSecond = tileSize * (1f + new Random(id * 31).NextSingle() * 2f);

            Hunger = new Random(id * 13).NextSingle() * 0.30f;

            var startPx = TileCentre(startX, startY, tileSize);
            Position   = startPx;
            _targetPos = startPx;
        }

        // ── Tile helpers ─────────────────────────────────────────────────────
        public int TileX(int tileSize) => (int)(Position.X / tileSize);
        public int TileY(int tileSize) => (int)(Position.Y / tileSize);

        // ── Sim tick: AI decisions only ───────────────────────────────────────
        public void Tick(World.World world, Random rng, int tileSize)
        {
            Hunger = Math.Min(1f, Hunger + HungerRisePerTick);

            int tx = TileX(tileSize);
            int ty = TileY(tileSize);

            Goal = SelectGoal(world, tx, ty);

            switch (Goal)
            {
                case CharacterGoal.Eating:
                    world.ConsumeFood(tx, ty);
                    Hunger = Math.Max(0f, Hunger - HungerEatPerTick);
                    State  = CharacterState.Idle;
                    break;

                case CharacterGoal.SeekingFood:
                    ExecuteSeekFood(world, rng, tileSize, tx, ty);
                    break;

                default:
                    ExecuteWander(world, rng, tileSize, tx, ty);
                    break;
            }
        }

        // ── Frame update: smooth continuous movement ──────────────────────────
        /// <summary>
        /// Called every frame. Moves Position toward _targetPos at
        /// BasePixelsPerSecond * speedMultiplier, so the simulation looks
        /// proportionally faster or slower as tick speed changes.
        /// </summary>
        public void UpdateRenderPos(float deltaSeconds, float speedMultiplier)
        {
            Vector2 delta = _targetPos - Position;
            float   dist  = delta.Length();

            if (dist <= ArrivalThreshold)
            {
                Position = _targetPos;
                if (Goal != CharacterGoal.Eating)
                    State = CharacterState.Idle;
                return;
            }

            float step = BasePixelsPerSecond * speedMultiplier * deltaSeconds;
            Position = dist <= step
                ? _targetPos
                : Position + Vector2.Normalize(delta) * step;

            State = CharacterState.Moving;
        }

        // ── Goal selection ────────────────────────────────────────────────────
        private CharacterGoal SelectGoal(World.World world, int tx, int ty)
        {
            if (Goal == CharacterGoal.Eating
                && Hunger > HungerSatisfied
                && world.HasFood(tx, ty))
                return CharacterGoal.Eating;

            if (Hunger > HungerSatisfied && world.HasFood(tx, ty))
                return CharacterGoal.Eating;

            if (Hunger >= HungerSeekThreshold)
                return CharacterGoal.SeekingFood;

            return CharacterGoal.Idle;
        }

        // ── Behaviour execution ───────────────────────────────────────────────
        private void ExecuteSeekFood(World.World world, Random rng, int tileSize, int tx, int ty)
        {
            if (_seeker.TryGetTargetTile(tx, ty, world, rng,
                    static (w, x, y) => w.HasFood(x, y),
                    out int goalX, out int goalY))
            {
                _targetPos = TileCentre(goalX, goalY, tileSize);
                State      = CharacterState.Moving;
            }
            else
            {
                ExecuteWander(world, rng, tileSize, tx, ty);
            }
        }

        private void ExecuteWander(World.World world, Random rng, int tileSize, int tx, int ty)
        {
            if (Vector2.Distance(Position, _targetPos) < ArrivalThreshold)
            {
                if (_wander.TryGetTarget(tx, ty, world, rng, tileSize, out Vector2 target))
                {
                    _targetPos = target;
                    State      = CharacterState.Moving;
                }
                else
                {
                    State = CharacterState.Idle;
                }
            }
        }

        // ── Utility ──────────────────────────────────────────────────────────
        private static Vector2 TileCentre(int gx, int gy, int tileSize)
            => new Vector2(gx * tileSize + tileSize * 0.5f,
                           gy * tileSize + tileSize * 0.5f);
    }
}