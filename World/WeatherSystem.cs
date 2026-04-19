using System;
using Microsoft.Xna.Framework;
using SimGame.Core;

namespace SimGame.World
{
    public enum WeatherState
    {
        Clear,
        Cloudy,
        Rain,
        Storm,
        Snow
    }

    /// <summary>
    /// Manages global weather state as a probabilistic state machine.
    /// Transitions are season-weighted and influenced by wind speed.
    /// Drives rainfall amount, lighting modifier, and snow accumulation.
    /// </summary>
    public class WeatherSystem
    {
        // ── Public state ──────────────────────────────────────────────────────

        public WeatherState Current     { get; private set; } = WeatherState.Clear;
        public WeatherState Previous    { get; private set; } = WeatherState.Clear;

        /// <summary>
        /// How long the current weather state has been active in ticks.
        /// </summary>
        public int StateDurationTicks { get; private set; }

        /// <summary>
        /// Rainfall intensity this tick. 0 = no rain, 1 = heaviest rain.
        /// Only > 0 during Rain or Storm.
        /// </summary>
        public float RainfallIntensity { get; private set; }

        /// <summary>
        /// Snowfall intensity this tick. Only > 0 during Snow.
        /// </summary>
        public float SnowfallIntensity { get; private set; }

        /// <summary>
        /// Lighting multiplier applied on top of the day/night cycle.
        /// Clear = 1.0, Storm = 0.5 (dark clouds).
        /// </summary>
        public float LightingModifier { get; private set; } = 1f;

        /// <summary>
        /// Wind speed boost applied during storms.
        /// </summary>
        public float WindBoost { get; private set; }

        public string WeatherLabel => Current switch
        {
            WeatherState.Clear  => "Clear",
            WeatherState.Cloudy => "Cloudy",
            WeatherState.Rain   => "Raining",
            WeatherState.Storm  => "Storm",
            WeatherState.Snow   => "Snowing",
            _                   => "Unknown"
        };

        // ── Private state ─────────────────────────────────────────────────────

        private readonly Random _rng;

        // Minimum ticks a weather state must last before transitioning
        private const int MinStateDuration = 60;   // 1 in-game hour
        private const int MaxStateDuration = 1440; // 1 in-game day

        // ── Season transition matrices ────────────────────────────────────────
        // [fromState][toState] = relative weight
        // Rows:    Clear, Cloudy, Rain, Storm, Snow
        // Columns: Clear, Cloudy, Rain, Storm, Snow

        private static readonly float[,] SpringWeights =
        {
            // To:  Clear  Cloudy  Rain  Storm  Snow
            /* Clear  */ { 6f,    3f,    2f,   0.5f,  0f   },
            /* Cloudy */ { 3f,    4f,    3f,   1f,    0f   },
            /* Rain   */ { 2f,    3f,    4f,   1f,    0f   },
            /* Storm  */ { 1f,    2f,    3f,   2f,    0f   },
            /* Snow   */ { 2f,    2f,    2f,   0f,    0f   },
        };

        private static readonly float[,] SummerWeights =
        {
            // To:  Clear  Cloudy  Rain  Storm  Snow
            /* Clear  */ { 8f,    3f,    1f,   0.5f,  0f   },
            /* Cloudy */ { 4f,    5f,    2f,   1f,    0f   },
            /* Rain   */ { 2f,    3f,    3f,   2f,    0f   },
            /* Storm  */ { 1f,    2f,    2f,   3f,    0f   },
            /* Snow   */ { 0f,    0f,    0f,   0f,    0f   },
        };

        private static readonly float[,] AutumnWeights =
        {
            // To:  Clear  Cloudy  Rain  Storm  Snow
            /* Clear  */ { 4f,    4f,    3f,   0.5f,  0f   },
            /* Cloudy */ { 2f,    4f,    4f,   1f,    0.5f },
            /* Rain   */ { 1f,    3f,    5f,   1f,    0.5f },
            /* Storm  */ { 1f,    2f,    3f,   2f,    0f   },
            /* Snow   */ { 1f,    2f,    2f,   0f,    2f   },
        };

        private static readonly float[,] WinterWeights =
        {
            // To:  Clear  Cloudy  Rain  Storm  Snow
            /* Clear  */ { 3f,    3f,    1f,   0f,    2f   },
            /* Cloudy */ { 2f,    3f,    1f,   0.5f,  3f   },
            /* Rain   */ { 1f,    2f,    2f,   1f,    2f   },
            /* Storm  */ { 0.5f,  1f,    2f,   2f,    1f   },
            /* Snow   */ { 1f,    2f,    1f,   0.5f,  5f   },
        };

        public WeatherSystem(int seed)
        {
            _rng    = new Random(seed + 9000);
            Current = WeatherState.Clear;
        }

        // ── Update ────────────────────────────────────────────────────────────

        /// <summary>
        /// Called once per sim tick.
        /// </summary>
        public void Tick(TimeSystem time, WindSystem wind)
        {
            StateDurationTicks++;

            MaybeTransition(time, wind);
            UpdateDerivedValues(wind);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void MaybeTransition(TimeSystem time, WindSystem wind)
        {
            // Don't transition before minimum duration
            if (StateDurationTicks < MinStateDuration) return;

            // Transition probability increases as state ages
            float ageFactor = MathHelper.Clamp(
                (float)(StateDurationTicks - MinStateDuration) /
                (MaxStateDuration - MinStateDuration),
                0f, 1f);

            // Base 0.5% chance per tick, rising to 3% as state ages
            float transitionChance = MathHelper.Lerp(0.005f, 0.03f, ageFactor);

            // Storms are more likely to transition when wind is high
            if (Current == WeatherState.Storm && wind.Speed > 0.7f)
                transitionChance *= 1.5f;

            if (_rng.NextDouble() > transitionChance) return;

            // Pick next state from weighted table
            var weights = GetWeights(time.Season);
            int from    = (int)Current;

            float total = 0f;
            for (int i = 0; i < 5; i++) total += weights[from, i];

            float roll = (float)_rng.NextDouble() * total;
            float acc  = 0f;

            for (int i = 0; i < 5; i++)
            {
                acc += weights[from, i];
                if (roll <= acc)
                {
                    TransitionTo((WeatherState)i);
                    return;
                }
            }
        }

        private void TransitionTo(WeatherState next)
        {
            Previous           = Current;
            Current            = next;
            StateDurationTicks = 0;
        }

        private void UpdateDerivedValues(WindSystem wind)
        {
            switch (Current)
            {
                case WeatherState.Clear:
                    RainfallIntensity = 0f;
                    SnowfallIntensity = 0f;
                    LightingModifier  = 1.0f;
                    WindBoost         = 0f;
                    break;

                case WeatherState.Cloudy:
                    RainfallIntensity = 0f;
                    SnowfallIntensity = 0f;
                    LightingModifier  = 0.80f;
                    WindBoost         = 0.05f;
                    break;

                case WeatherState.Rain:
                    // Intensity varies slightly tick to tick for realism
                    RainfallIntensity = 0.4f + (float)_rng.NextDouble() * 0.3f;
                    SnowfallIntensity = 0f;
                    LightingModifier  = 0.65f;
                    WindBoost         = 0.10f;
                    break;

                case WeatherState.Storm:
                    RainfallIntensity = 0.7f + (float)_rng.NextDouble() * 0.3f;
                    SnowfallIntensity = 0f;
                    LightingModifier  = 0.45f;
                    WindBoost         = 0.35f;
                    break;

                case WeatherState.Snow:
                    RainfallIntensity = 0f;
                    SnowfallIntensity = 0.3f + (float)_rng.NextDouble() * 0.4f;
                    LightingModifier  = 0.85f; // snow is bright
                    WindBoost         = 0.10f;
                    break;
            }
        }

        private static float[,] GetWeights(int season) => season switch
        {
            0 => SpringWeights,
            1 => SummerWeights,
            2 => AutumnWeights,
            _ => WinterWeights
        };
    }
}