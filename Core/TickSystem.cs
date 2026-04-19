namespace SimGame.Core
{
    /// <summary>
    /// Decouples simulation speed from frame rate.
    /// Accumulates elapsed time and fires ticks at a fixed interval.
    ///
    /// Speed multipliers:
    ///   0.5x →  1 tick / 2 s
    ///   1x   →  1 tick / 1 s
    ///   3x   →  3 ticks / 1 s
    ///   8x   →  8 ticks / 1 s
    /// </summary>
    public class TickSystem
    {
        public bool  Paused          { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;
        public int   TotalTicks      { get; private set; }

        // Base interval at 1x speed: one tick per second
        private const float BaseTickInterval = 1.0f;

        private float _accumulator;

        public void TogglePause()              => Paused = !Paused;
        public void SetSpeed(float multiplier) => SpeedMultiplier = multiplier;

        /// <summary>
        /// Advance the accumulator by deltaSeconds.
        /// Returns how many ticks should fire this frame (usually 0 or 1).
        /// </summary>
        public int Update(float deltaSeconds)
        {
            if (Paused) return 0;

            _accumulator += deltaSeconds * SpeedMultiplier;

            float interval = BaseTickInterval;
            int ticks = 0;
            while (_accumulator >= interval)
            {
                _accumulator -= interval;
                ticks++;
                TotalTicks++;
            }

            return ticks;
        }
    }
}