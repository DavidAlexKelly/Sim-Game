using System;

namespace SimGame.Core
{
    /// <summary>
    /// Tracks in-game time. Driven by sim ticks from TickSystem.
    /// 1 tick  = 1 in-game minute
    /// 60 ticks = 1 hour
    /// 24 hours = 1 day
    /// 30 days  = 1 month
    /// 12 months = 1 year
    /// Lighting updates every hour (every 60 ticks).
    /// </summary>
    public class TimeSystem
    {
        public const int MinutesPerHour = 60;
        public const int HoursPerDay    = 24;
        public const int DaysPerMonth   = 30;
        public const int MonthsPerYear  = 12;

        public int Minute { get; private set; } = 0;
        public int Hour   { get; private set; } = 6; // start at dawn
        public int Day    { get; private set; } = 1;
        public int Month  { get; private set; } = 1;
        public int Year   { get; private set; } = 1;

        public int TotalTicks { get; private set; }

        /// <summary>
        /// Advance time by the number of ticks that fired this frame.
        /// Each tick = 1 minute.
        /// </summary>
        public void Advance(int ticks)
        {
            for (int i = 0; i < ticks; i++)
                AdvanceOneMinute();
        }

        public void Reset()
        {
            Minute = 0;
            Hour   = 6;
            Day    = 1;
            Month  = 1;
            Year   = 1;
            TotalTicks = 0;
        }

        private void AdvanceOneMinute()
        {
            TotalTicks++;
            Minute++;
            if (Minute < MinutesPerHour) return;

            Minute = 0;
            Hour++;
            if (Hour < HoursPerDay) return;

            Hour = 0;
            Day++;
            if (Day <= DaysPerMonth) return;

            Day = 1;
            Month++;
            if (Month <= MonthsPerYear) return;

            Month = 1;
            Year++;
        }

        // ── Derived helpers ───────────────────────────────────────────

        /// <summary>
        /// 0.0 = midnight, 1.0 = noon.
        /// Smooth sinusoidal curve that only steps on the hour,
        /// so lighting changes are gradual hour-by-hour rather than
        /// updating every tick.
        /// </summary>
        public float LightFactor
        {
            get
            {
                // Only use Hour (not Minute) so lighting steps per hour
                float radians = ((Hour - 6f) / HoursPerDay) * MathF.Tau;
                float raw = MathF.Sin(radians); // -1 to 1
                return (raw + 1f) * 0.5f;       // 0 to 1
            }
        }

        /// <summary>
        /// Smoothly interpolated light factor that transitions between
        /// hours using the current minute as a sub-step.
        /// Use this if you want silky smooth lighting instead of hourly steps.
        /// </summary>
        public float LightFactorSmooth
        {
            get
            {
                float t = Minute / (float)MinutesPerHour; // 0..1 within the hour

                float radiansNow  = ((Hour       - 6f) / HoursPerDay) * MathF.Tau;
                float radiansNext = ((Hour + 1f  - 6f) / HoursPerDay) * MathF.Tau;

                float rawNow  = (MathF.Sin(radiansNow)  + 1f) * 0.5f;
                float rawNext = (MathF.Sin(radiansNext) + 1f) * 0.5f;

                return Microsoft.Xna.Framework.MathHelper.Lerp(rawNow, rawNext, t);
            }
        }

        /// <summary>
        /// 0-based season index: 0=Spring, 1=Summer, 2=Autumn, 3=Winter.
        /// </summary>
        public int Season => (Month - 1) / 3;

        public string SeasonName => Season switch
        {
            0 => "Spring",
            1 => "Summer",
            2 => "Autumn",
            _ => "Winter"
        };

        public string MonthName => Month switch
        {
            1  => "January",
            2  => "February",
            3  => "March",
            4  => "April",
            5  => "May",
            6  => "June",
            7  => "July",
            8  => "August",
            9  => "September",
            10 => "October",
            11 => "November",
            _  => "December"
        };

        public string TimeOfDayLabel => Hour switch
        {
            >= 5  and < 8  => "Dawn",
            >= 8  and < 12 => "Morning",
            >= 12 and < 14 => "Noon",
            >= 14 and < 18 => "Afternoon",
            >= 18 and < 21 => "Dusk",
            _              => "Night"
        };
    }
}