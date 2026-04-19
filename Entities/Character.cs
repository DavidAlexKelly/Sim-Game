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

        // ── Free movement ────────────────────────────────────────────────────
        /// <summary>World-pixel position (centre of character).</summary>
        public Vector2 Position { get; private set; }

        /// <summary>Tiles per tick the character can travel.</summary>
        public float Speed { get; }

        /// <summary>Current destination in world pixels.</summary>
        private Vector2 _targetPos;

        /// <summary>How close (pixels) before we consider the target reached.</summary>
        private const float ArrivalThreshold = 1.5f;

        // ── Behaviours ───────────────────────────────────────────────────────
        private readonly WanderBehaviour   _wander = new();
        private readonly SeekTileBehaviour _seeker = new();

        // ── Render ───────────────────────────────────────────────────────────
        /// <summary>Smoothed visual position — lerped toward Position each frame.</summary>
        public Vector2 RenderPos { get; private set; }

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

            // Speed: 1.5–3.5 tiles per tick, varied per character
            Speed = 1.5f + new Random(id * 31).NextSingle() * 2f;

            Hunger = new Random(id * 13).NextSingle() * 0.30f;

            var startPx = TileCentre(startX, startY, tileSize);
            Position    = startPx;
            _targetPos  = startPx;
            RenderPos   = startPx;
        }

        // ── Tile helpers (read-only, used by behaviours) ─────────────────────

        /// <summary>Tile column the character's centre currently occupies.</summary>
        public int TileX(int tileSize) => (int)(Position.X / tileSize);

        /// <summary>Tile row the character's centre currently occupies.</summary>
        public int TileY(int tileSize) => (int)(Position.Y / tileSize);

        // ── Sim tick ─────────────────────────────────────────────────────────

        public void Tick(World.World world, Random rng, int tileSize)
        {
            // 1. Update need
            Hunger = Math.Min(1f, Hunger + HungerRisePerTick);

            int tx = TileX(tileSize);
            int ty = TileY(tileSize);

            // 2. Select goal
            Goal = SelectGoal(world, tx, ty);

            // 3. Execute goal
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

            // 4. Move toward target by up to Speed tiles this tick
            MoveTowardTarget(tileSize);
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
            // Only pick a new wander target when we've arrived at the current one
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

        // ── Movement ─────────────────────────────────────────────────────────

        private void MoveTowardTarget(int tileSize)
        {
            float maxDistance = Speed * tileSize;
            Vector2 delta     = _targetPos - Position;
            float   dist      = delta.Length();

            if (dist <= ArrivalThreshold)
            {
                Position = _targetPos;
                if (Goal != CharacterGoal.Eating)
                    State = CharacterState.Idle;
                return;
            }

            Position = dist <= maxDistance
                ? _targetPos
                : Position + Vector2.Normalize(delta) * maxDistance;

            State = CharacterState.Moving;
        }

        // ── Visual lerp ──────────────────────────────────────────────────────

        public void UpdateRenderPos(float deltaSeconds, float lerpSpeed = 0.20f)
        {
            float t   = 1f - MathF.Pow(1f - lerpSpeed, deltaSeconds * 60f);
            RenderPos = Vector2.Lerp(RenderPos, Position, t);
        }

        // ── Utility ──────────────────────────────────────────────────────────

        private static Vector2 TileCentre(int gx, int gy, int tileSize)
            => new Vector2(gx * tileSize + tileSize * 0.5f,
                           gy * tileSize + tileSize * 0.5f);
    }
}