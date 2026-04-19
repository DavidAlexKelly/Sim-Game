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
    ///   16x  → 16 ticks / 1 s
    ///   32x  → 32 ticks / 1 s
    /// </summary>
    public class TickSystem
    {
        public bool  Paused          { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;
        public int   TotalTicks      { get; private set; }

        private const float BaseTickInterval = 1.0f;

        /// <summary>
        /// Safety cap on ticks per frame. At 32x and 60 fps this would be
        /// ~0.53 ticks/frame so the cap of 64 is never hit in normal play
        /// but prevents a spiral of death if the frame rate drops badly.
        /// </summary>
        private const int MaxTicksPerFrame = 64;

        private float _accumulator;

        public void TogglePause() => Paused = !Paused;
        public void SetSpeed(float multiplier) => SpeedMultiplier = multiplier;

        public int Update(float deltaSeconds)
        {
            if (Paused) return 0;

            _accumulator += deltaSeconds * SpeedMultiplier;
            float interval = BaseTickInterval;
            int   ticks    = 0;

            while (_accumulator >= interval && ticks < MaxTicksPerFrame)
            {
                _accumulator -= interval;
                ticks++;
                TotalTicks++;
            }

            // If we hit the cap, drain the excess so we don't
            // accumulate a backlog that fires all at once later
            if (ticks >= MaxTicksPerFrame)
                _accumulator = 0f;

            return ticks;
        }
    }
}