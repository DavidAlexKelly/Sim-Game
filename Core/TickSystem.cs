namespace SimGame.Core
{
    /// <summary>
    /// Decouples simulation speed from frame rate.
    /// Accumulates elapsed time and fires ticks at a fixed interval.
    /// </summary>
    public class TickSystem
    {
        public bool  Paused           { get; private set; }
        public float SpeedMultiplier  { get; private set; } = 1f;
        public int   TotalTicks       { get; private set; }

        private readonly float _tickInterval;   // seconds between ticks
        private float          _accumulator;

        public TickSystem(float tickIntervalSeconds = 0.20f)
        {
            _tickInterval = tickIntervalSeconds;
        }

        public void TogglePause()             => Paused = !Paused;
        public void SetSpeed(float multiplier) => SpeedMultiplier = multiplier;

        /// <summary>
        /// Advance the accumulator by deltaSeconds.
        /// Returns how many ticks should fire this frame (usually 0 or 1).
        /// </summary>
        public int Update(float deltaSeconds)
        {
            if (Paused) return 0;

            _accumulator += deltaSeconds * SpeedMultiplier;

            int ticks = 0;
            while (_accumulator >= _tickInterval)
            {
                _accumulator -= _tickInterval;
                ticks++;
                TotalTicks++;
            }

            return ticks;
        }
    }
}
