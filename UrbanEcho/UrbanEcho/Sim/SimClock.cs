namespace UrbanEcho.Sim
{
    /// <summary>
    /// Maps simulation time (real elapsed seconds at 60 fps) to a configurable
    /// time-of-day and drives vehicle spawn interval timing.
    ///
    /// Time compression: at SimMinutesPerRealSecond = 1, one real simulation
    /// second equals one simulated minute, so a full 24-hour day plays out in
    /// 24 real minutes of simulation time.
    /// </summary>
    public class SimClock
    {
        /// <summary>How many simulated minutes pass per real simulation second.</summary>
        public float SimMinutesPerRealSecond { get; set; }

        /// <summary>The hour of day (0–23) at which the simulation begins.</summary>
        public int StartHourOfDay { get; }

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

        /// <summary>Resets the clock back to time zero.</summary>
        public void Reset()
        {
            _lastSpawnTime = 0f;
        }
    }
}
