namespace UrbanEcho.Sim
{
    /// <summary>
    /// Maps simulation time (real elapsed seconds at 60 fps) to a
    /// time-of-day clock that starts at the observation window's start hour.
    ///
    /// At the default SimMinutesPerRealSecond = 1/60, the clock runs 1:1
    /// with real time: one real second equals one simulated second.
    /// </summary>
    public class SimClock
    {
        /// <summary>How many simulated minutes pass per real simulation second.</summary>
        public float SimMinutesPerRealSecond { get; set; }

        /// <summary>The hour of day (0–23) at which the simulation begins.</summary>
        public int StartHourOfDay { get; set; }

        private float _lastSpawnTime = 0f;

        public SimClock(int startHourOfDay = 6, float simMinutesPerRealSecond = 1f)
        {
            StartHourOfDay = startHourOfDay;
            SimMinutesPerRealSecond = simMinutesPerRealSecond;
        }

        /// <summary>Returns the current simulated hour of day (0–23).</summary>
        public int CurrentHour(float simTime) =>
            (StartHourOfDay + (int)(simTime * SimMinutesPerRealSecond / 60f)) % 24;

        /// <summary>Returns the current simulated minute within the hour (0–59).</summary>
        public int CurrentMinute(float simTime) =>
            (int)(simTime * SimMinutesPerRealSecond % 60f);

        /// <summary>Returns the formatted time-of-day string, e.g. "07:45".</summary>
        public string FormatTimeOfDay(float simTime) =>
            $"{CurrentHour(simTime):D2}:{CurrentMinute(simTime):D2}";

        /// <summary>True during AM rush (07:00–09:00) or PM rush (16:00–18:00).</summary>
        public bool IsRushHour(float simTime)
        {
            int h = CurrentHour(simTime);
            return (h >= 7 && h < 9) || (h >= 16 && h < 18);
        }

        /// <summary>
        /// Returns the number of real simulation seconds between vehicle spawns,
        /// scaled by time of day. Lower value = more frequent spawning.
        /// </summary>
        public float GetSpawnIntervalSeconds(float simTime)
        {
            int h = CurrentHour(simTime);
            if (h >= 22 || h < 5) return 15f;  // off-peak night
            if (IsRushHour(simTime)) return 2f;  // rush hour
            return 5f;                            // normal daytime
        }

        /// <summary>
        /// Returns true when enough simulation time has elapsed to spawn the
        /// next vehicle based on the current time-of-day rate.
        /// Resets the internal timer when true is returned.
        /// </summary>
        public bool ShouldSpawn(float simTime)
        {
            if (simTime - _lastSpawnTime >= GetSpawnIntervalSeconds(simTime))
            {
                _lastSpawnTime = simTime;
                return true;
            }
            return false;
        }

        // Demand profile approximates typical urban diurnal patterns.
        // Values are estimates;
        private static readonly float[] DemandByHour =
        {
            0.10f, 0.10f, 0.10f, 0.10f, 0.10f, // 00–04  deep night
            0.25f,                                // 05     early morning
            0.50f,                                // 06     morning commute begins
            1.00f, 1.00f,                         // 07–08  AM rush
            0.75f,                                // 09     post-rush tapering
            0.60f, 0.60f,                         // 10–11  mid-morning
            0.65f, 0.65f,                         // 12–13  lunch
            0.60f,                                // 14     early afternoon
            0.75f,                                // 15     pre-rush ramp
            1.00f, 1.00f,                         // 16–17  PM rush
            0.75f,                                // 18     post-rush tapering
            0.55f,                                // 19     evening
            0.40f,                                // 20     late evening
            0.30f,                                // 21
            0.20f,                                // 22
            0.15f                                 // 23
        };

        /// <summary>
        /// Returns the average 0.0–1.0 demand fraction for the given observation
        /// window.  The window wraps around midnight (e.g. 22–05 is valid).
        /// Because the fraction is constant for the entire run, the target
        /// vehicle count stays fixed — no ramp-up / ramp-down.
        /// </summary>
        public float GetTrafficDemandFraction(int observationStartHour, int observationEndHour)
        {
            int start = observationStartHour % 24;
            int end   = observationEndHour   % 24;
            if (start == end)
                return DemandByHour[start];

            float total = 0;
            int   count = 0;
            int   h     = start;
            while (h != end)
            {
                total += DemandByHour[h];
                count++;
                h = (h + 1) % 24;
            }
            return count > 0 ? total / count : DemandByHour[start];
        }

        /// <summary>
        /// Returns the wall-clock duration of the observation window in real
        /// simulation seconds, taking <see cref="SimMinutesPerRealSecond"/> into
        /// account.  Wraps correctly across midnight (e.g. 22–05 = 7 hours).
        /// </summary>
        public float GetWindowDurationSeconds(int startHour, int endHour)
        {
            int hours = ((endHour - startHour) % 24 + 24) % 24;
            if (hours == 0) hours = 24;
            float simMinutes = hours * 60f;
            return simMinutes / SimMinutesPerRealSecond;
        }

        /// <summary>Returns a display label such as "07:00–09:00".</summary>
        public static string FormatObservationWindow(int startHour, int endHour) =>
            $"{startHour:D2}:00\u2013{endHour:D2}:00";

        /// <summary>Resets the clock back to time zero.</summary>
        public void Reset()
        {
            _lastSpawnTime = 0f;
        }
    }
}
