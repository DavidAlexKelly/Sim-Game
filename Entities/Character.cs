using System;
using Microsoft.Xna.Framework;
using SimGame.Entities.AI;
using SimGame.World;

namespace SimGame.Entities
{
    /// <summary>
    /// The character's high-level goal. Drives which behaviour runs each tick.
    /// Idle    – no urgent need; wanders.
    /// SeekingFood – hunger is critical; moving toward the nearest food tile.
    /// Eating  – arrived at a food tile; restoring hunger over several ticks.
    /// </summary>
    public enum CharacterGoal
    {
        Idle,
        SeekingFood,
        Eating
    }

    /// <summary>
    /// Low-level movement state, kept separate from the goal so the HUD
    /// can show both independently.
    /// </summary>
    public enum CharacterState { Idle, Moving }

    public class Character
    {
        // ── Identity ────────────────────────────────────────────────────────
        public int    Id   { get; }
        public string Name { get; }

        // ── Needs ───────────────────────────────────────────────────────────
        /// <summary>
        /// 0 = full, 1 = starving. Rises every tick; eating restores it.
        /// </summary>
        public float Hunger { get; private set; }

        private const float HungerRisePerTick  = 0.004f;  // ~250 ticks to starve
        private const float HungerEatPerTick   = 0.05f;   // ~20 ticks to recover fully
        private const float HungerSeekThreshold = 0.40f;  // start seeking food here
        private const float HungerSatisfied     = 0.10f;  // stop eating here

        // ── Goal / State ────────────────────────────────────────────────────
        public CharacterGoal  Goal  { get; private set; } = CharacterGoal.Idle;
        public CharacterState State { get; private set; } = CharacterState.Idle;

        // ── Simulation state (grid space) ───────────────────────────────────
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        // ── Visual state (pixel space, interpolated between ticks) ──────────
        public Vector2 RenderPos { get; private set; }

        private Vector2              _targetRenderPos;
        private readonly WanderBehaviour   _wander = new();
        private readonly SeekTileBehaviour _seeker = new();

        // ── Name pool ───────────────────────────────────────────────────────
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

            GridX = startX;
            GridY = startY;

            // Stagger starting hunger so characters don't all seek food at once
            Hunger = new Random(id * 13).NextSingle() * 0.30f;

            var startPx = GridToPixel(startX, startY, tileSize);
            RenderPos        = startPx;
            _targetRenderPos = startPx;
        }

        /// <summary>Advance one simulation tick.</summary>
        public void Tick(World.World world, Random rng, int tileSize)
        {
            // ── 1. Update need ───────────────────────────────────────────────
            Hunger = Math.Min(1f, Hunger + HungerRisePerTick);

            // ── 2. Select goal ───────────────────────────────────────────────
            Goal = SelectGoal(world);

            // ── 3. Execute goal ──────────────────────────────────────────────
            switch (Goal)
            {
                case CharacterGoal.Eating:
                    Hunger = Math.Max(0f, Hunger - HungerEatPerTick);
                    State  = CharacterState.Idle;
                    break;

                case CharacterGoal.SeekingFood:
                    ExecuteSeekFood(world, rng, tileSize);
                    break;

                case CharacterGoal.Idle:
                default:
                    ExecuteWander(world, rng, tileSize);
                    break;
            }
        }

        // ── Goal selection ───────────────────────────────────────────────────

        private CharacterGoal SelectGoal(World.World world)
        {
            // If already eating and not yet satisfied, keep eating
            if (Goal == CharacterGoal.Eating && Hunger > HungerSatisfied)
                return CharacterGoal.Eating;

            // Standing on a food tile and hungry enough to eat
            if (Hunger > HungerSatisfied && IsFoodTile(world, GridX, GridY))
                return CharacterGoal.Eating;

            // Hunger is urgent – go find food
            if (Hunger >= HungerSeekThreshold)
                return CharacterGoal.SeekingFood;

            return CharacterGoal.Idle;
        }

        // ── Behaviour execution ──────────────────────────────────────────────

        private void ExecuteSeekFood(World.World world, Random rng, int tileSize)
        {
            // Ask the seeker to step toward the nearest food tile
            if (_seeker.TryStepToward(GridX, GridY, world, rng,
                    static (w, x, y) => IsFoodTile(w, x, y),
                    out int nx, out int ny))
            {
                MoveTo(nx, ny, tileSize);
            }
            else
            {
                // No food reachable – fall back to wandering
                ExecuteWander(world, rng, tileSize);
            }
        }

        private void ExecuteWander(World.World world, Random rng, int tileSize)
        {
            if (_wander.TryGetNextMove(GridX, GridY, world, rng, out int nx, out int ny))
                MoveTo(nx, ny, tileSize);
            else
                State = CharacterState.Idle;
        }

        private void MoveTo(int nx, int ny, int tileSize)
        {
            GridX = nx;
            GridY = ny;
            State = CharacterState.Moving;
            _targetRenderPos = GridToPixel(GridX, GridY, tileSize);
        }

        // ── Tile queries ─────────────────────────────────────────────────────

        private static bool IsFoodTile(World.World world, int x, int y)
        {
            if (!world.InBounds(x, y)) return false;
            var t = world.GetTile(x, y).Type;
            return t == TileType.Grass || t == TileType.Forest;
        }

        // ── Visual lerp ─────────────────────────────────────────────────────

        /// <summary>Smooth render position toward target — called every frame.</summary>
        public void UpdateRenderPos(float deltaSeconds, float lerpSpeed = 0.18f)
        {
            float t   = 1f - MathF.Pow(1f - lerpSpeed, deltaSeconds * 60f);
            RenderPos = Vector2.Lerp(RenderPos, _targetRenderPos, t);
        }

        private static Vector2 GridToPixel(int gx, int gy, int tileSize)
            => new Vector2(gx * tileSize + tileSize * 0.5f,
                           gy * tileSize + tileSize * 0.5f);
    }
}