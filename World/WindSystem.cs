using System;
using Microsoft.Xna.Framework;
using SimGame.Core;

namespace SimGame.World
{
    public enum WindDirection
    {
        North, NorthEast, East, SouthEast,
        South, SouthWest, West, NorthWest
    }

    /// <summary>
    /// Tracks global wind direction and speed.
    /// Now initialised with the prevailing wind direction for the map's
    /// latitude from PlanetarySettings. Wind still shifts over time but
    /// is biased back toward the prevailing direction.
    /// </summary>
    public class WindSystem
    {
        public WindDirection Direction { get; private set; }
        public float         Speed     { get; private set; }

        public string SpeedLabel => Speed switch
        {
            < 0.10f => "Calm",
            < 0.25f => "Light breeze",
            < 0.45f => "Moderate",
            < 0.65f => "Strong",
            < 0.85f => "Very strong",
            _       => "Storm force"
        };

        private readonly Random        _rng;
        private readonly ElevationMap  _elevation;
        private readonly WindDirection _prevailingDirection;

        private const int   DirectionShiftInterval = 240;
        private const float SpeedChangeRate        = 0.002f;

        private int   _ticksSinceShift;
        private float _targetSpeed;

        private static readonly float[] SeasonSpeedBias = { 0.3f, 0.4f, 0.35f, 0.55f };

        public WindSystem(ElevationMap elevation, int seed, PlanetarySettings planet)
        {
            _elevation = elevation;
            _rng       = new Random(seed + 3000);

            // Set prevailing direction from planetary latitude
            _prevailingDirection = PlanetarySettings.PrevailingWind(
                planet.CentreLatitude);

            // Start at prevailing direction with slight random offset
            int offset = _rng.Next(-1, 2); // -1, 0, or +1
            Direction    = (WindDirection)(((int)_prevailingDirection + offset + 8) % 8);
            Speed        = 0.2f + (float)_rng.NextDouble() * 0.3f;
            _targetSpeed = Speed;
        }

        public void Tick(TimeSystem time)
        {
            UpdateSpeed(time);
            MaybeShiftDirection(time);
            _ticksSinceShift++;
        }

        public float GetExposure(int x, int y)
        {
            var (dx, dy) = DirectionVector();
            float myElevation    = _elevation.Get(x, y);
            float maxUpwindElev  = myElevation;

            for (int step = 1; step <= 8; step++)
            {
                int ux = x - dx * step;
                int uy = y - dy * step;
                if (ux < 0 || uy < 0 || ux >= _elevation.Width || uy >= _elevation.Height)
                    break;
                float upwindElev = _elevation.Get(ux, uy);
                if (upwindElev > maxUpwindElev)
                    maxUpwindElev = upwindElev;
            }

            float elevDiff = maxUpwindElev - myElevation;
            float shelter  = MathHelper.Clamp(elevDiff / 0.15f, 0f, 1f);
            return 1f - shelter;
        }

        public float GetWindChill(int x, int y)
        {
            float exposure = GetExposure(x, y);
            return -8f * Speed * exposure;
        }

        public (int dx, int dy) DirectionVector() => Direction switch
        {
            WindDirection.North     => ( 0, -1),
            WindDirection.NorthEast => ( 1, -1),
            WindDirection.East      => ( 1,  0),
            WindDirection.SouthEast => ( 1,  1),
            WindDirection.South     => ( 0,  1),
            WindDirection.SouthWest => (-1,  1),
            WindDirection.West      => (-1,  0),
            WindDirection.NorthWest => (-1, -1),
            _                       => ( 1,  0)
        };

        private void UpdateSpeed(TimeSystem time)
        {
            if (_rng.Next(120) == 0)
            {
                float bias   = SeasonSpeedBias[time.Season];
                _targetSpeed = MathHelper.Clamp(
                    bias + ((float)_rng.NextDouble() - 0.5f) * 0.4f, 0f, 1f);
            }

            float delta = _targetSpeed - Speed;
            Speed = MathHelper.Clamp(
                Speed + MathF.Sign(delta) * SpeedChangeRate, 0f, 1f);
        }

        private void MaybeShiftDirection(TimeSystem time)
        {
            if (_ticksSinceShift < DirectionShiftInterval) return;

            float shiftChance = 0.1f + SeasonSpeedBias[time.Season] * 0.2f;
            if (_rng.NextDouble() > shiftChance) return;

            // Bias toward prevailing direction — 60% chance to move toward it,
            // 40% chance to shift randomly
            int prevailingInt = (int)_prevailingDirection;
            int currentInt    = (int)Direction;
            int diff          = ((prevailingInt - currentInt + 8) % 8);

            int steps;
            if (_rng.NextDouble() < 0.60f && diff != 0)
            {
                // Step toward prevailing direction
                steps = diff <= 4 ? 1 : -1;
            }
            else
            {
                // Random shift of 1-2 steps
                steps = _rng.Next(1, 3) * (_rng.Next(2) == 0 ? 1 : -1);
            }

            Direction        = (WindDirection)(((int)Direction + steps + 8) % 8);
            _ticksSinceShift = 0;
        }
    }
}