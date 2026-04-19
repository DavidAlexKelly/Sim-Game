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

        // ── Simulation state ────────────────────────────────────────────────
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        // ── Visual state ────────────────────────────────────────────────────
        public Vector2 RenderPos { get; private set; }

        private Vector2                    _targetRenderPos;
        private readonly WanderBehaviour   _wander = new();
        private readonly SeekTileBehaviour _seeker = new();

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

            Hunger = new Random(id * 13).NextSingle() * 0.30f;

            var startPx = GridToPixel(startX, startY, tileSize);
            RenderPos        = startPx;
            _targetRenderPos = startPx;
        }

        public void Tick(World.World world, Random rng, int tileSize)
        {
            // 1. Update need
            Hunger = Math.Min(1f, Hunger + HungerRisePerTick);

            // 2. Select goal
            Goal = SelectGoal(world);

            // 3. Execute goal
            switch (Goal)
            {
                case CharacterGoal.Eating:
                    // Consume one portion from the tile this tick, restore hunger
                    world.ConsumeFood(GridX, GridY);
                    Hunger = Math.Max(0f, Hunger - HungerEatPerTick);
                    State  = CharacterState.Idle;
                    break;

                case CharacterGoal.SeekingFood:
                    ExecuteSeekFood(world, rng, tileSize);
                    break;

                default:
                    ExecuteWander(world, rng, tileSize);
                    break;
            }
        }

        // ── Goal selection ───────────────────────────────────────────────────

        private CharacterGoal SelectGoal(World.World world)
        {
            // Keep eating while tile still has food and character still hungry
            if (Goal == CharacterGoal.Eating
                && Hunger > HungerSatisfied
                && world.HasFood(GridX, GridY))
                return CharacterGoal.Eating;

            // Stepped onto an available food tile — start eating
            if (Hunger > HungerSatisfied && world.HasFood(GridX, GridY))
                return CharacterGoal.Eating;

            // Hunger is urgent — seek nearest food source
            if (Hunger >= HungerSeekThreshold)
                return CharacterGoal.SeekingFood;

            return CharacterGoal.Idle;
        }

        // ── Behaviour execution ──────────────────────────────────────────────

        private void ExecuteSeekFood(World.World world, Random rng, int tileSize)
        {
            if (_seeker.TryStepToward(GridX, GridY, world, rng,
                    static (w, x, y) => w.HasFood(x, y),
                    out int nx, out int ny))
            {
                MoveTo(nx, ny, tileSize);
            }
            else
            {
                // No food reachable in range — fall back to wandering
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
            GridX            = nx;
            GridY            = ny;
            State            = CharacterState.Moving;
            _targetRenderPos = GridToPixel(GridX, GridY, tileSize);
        }

        // ── Visual lerp ─────────────────────────────────────────────────────

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