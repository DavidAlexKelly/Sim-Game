using System;
using Microsoft.Xna.Framework;
using SimGame.Entities.AI;
using SimGame.World;

namespace SimGame.Entities
{
    public enum CharacterState { Idle, Moving }

    public class Character
    {
        // ── Identity ────────────────────────────────────────────────────────
        public int    Id   { get; }
        public string Name { get; }

        // ── Simulation state (grid space) ───────────────────────────────────
        public int            GridX  { get; private set; }
        public int            GridY  { get; private set; }
        public CharacterState State  { get; private set; } = CharacterState.Idle;

        // ── Visual state (pixel space, interpolated between ticks) ──────────
        public Vector2 RenderPos { get; private set; }

        private Vector2            _targetRenderPos;
        private readonly WanderBehaviour _ai = new();

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

            var startPx = GridToPixel(startX, startY, tileSize);
            RenderPos        = startPx;
            _targetRenderPos = startPx;
        }

        /// <summary>Advance one simulation tick.</summary>
        public void Tick(World.World world, Random rng, int tileSize)
        {
            if (_ai.TryGetNextMove(GridX, GridY, world, rng, out int nx, out int ny))
            {
                GridX = nx;
                GridY = ny;
                State = CharacterState.Moving;
            }
            else
            {
                State = CharacterState.Idle;
            }

            _targetRenderPos = GridToPixel(GridX, GridY, tileSize);
        }

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
