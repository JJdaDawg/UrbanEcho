using UrbanEcho.Sim;

namespace UrbanEcho.Tests;

/// <summary>
/// Unit tests for SimClock: spawn-timing intervals, time-of-day math,
/// traffic demand fractions, and observation-window duration.
/// </summary>
public class SimClockTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a clock with the default 1:1 real-time ratio used by Sim.cs
    /// (1 simulated minute per 60 real seconds).
    /// </summary>
    private static SimClock RealTimeClockAt(int startHour) =>
        new SimClock(startHourOfDay: startHour, simMinutesPerRealSecond: 1f / 60f);

    // ── CurrentHour ──────────────────────────────────────────────────────────

    [Test]
    public void CurrentHour_AtSimTimeZero_ReturnsStartHour()
    {
        var clock = RealTimeClockAt(7);
        Assert.That(clock.CurrentHour(0f), Is.EqualTo(7));
    }

    [TestCase(3600f, 8)]   // +1 real hour  → +1 sim hour
    [TestCase(7200f, 9)]   // +2 real hours → +2 sim hours
    [TestCase(61200f, 0)]  // +17 real hours → (7+17) % 24 = 0 (midnight)
    public void CurrentHour_AfterElapsedTime_ReturnsCorrectHour(float simTime, int expectedHour)
    {
        var clock = RealTimeClockAt(7);
        Assert.That(clock.CurrentHour(simTime), Is.EqualTo(expectedHour));
    }

    // ── CurrentMinute ─────────────────────────────────────────────────────────

    [TestCase(0f, 0)]
    [TestCase(1800f, 30)]  // 1800 * (1/60) % 60 = 30
    [TestCase(3540f, 59)]  // one minute before the next hour
    public void CurrentMinute_ReturnsMinutesWithinCurrentHour(float simTime, int expectedMinute)
    {
        var clock = RealTimeClockAt(7);
        Assert.That(clock.CurrentMinute(simTime), Is.EqualTo(expectedMinute));
    }

    // ── IsRushHour ────────────────────────────────────────────────────────────

    [TestCase(0f,     true,  "AM rush start (hour 7)")]
    [TestCase(3600f,  true,  "AM rush peak (hour 8)")]
    [TestCase(7200f,  false, "Post-AM rush (hour 9)")]
    [TestCase(32400f, true,  "PM rush start (hour 16)")]
    [TestCase(36000f, true,  "PM rush peak (hour 17)")]
    [TestCase(39600f, false, "Post-PM rush (hour 18)")]
    [TestCase(10800f, false, "Mid-morning (hour 10)")]
    public void IsRushHour_ReturnsExpectedValue(float simTime, bool expected, string description)
    {
        var clock = RealTimeClockAt(7);
        Assert.That(clock.IsRushHour(simTime), Is.EqualTo(expected), description);
    }

    // ── GetSpawnIntervalSeconds ───────────────────────────────────────────────

    [Test]
    public void GetSpawnIntervalSeconds_DuringRushHour_Returns2Seconds()
    {
        var clock = RealTimeClockAt(7);
        Assert.That(clock.GetSpawnIntervalSeconds(0f), Is.EqualTo(2f));
    }

    [Test]
    public void GetSpawnIntervalSeconds_LateNight_Returns15Seconds()
    {
        // simTime=54000 → hour = (7 + 15) % 24 = 22, which triggers off-peak night
        var clock = RealTimeClockAt(7);
        Assert.That(clock.GetSpawnIntervalSeconds(54000f), Is.EqualTo(15f));
    }

    [Test]
    public void GetSpawnIntervalSeconds_NormalDaytime_Returns5Seconds()
    {
        // simTime=10800 → hour = (7 + 3) % 24 = 10
        var clock = RealTimeClockAt(7);
        Assert.That(clock.GetSpawnIntervalSeconds(10800f), Is.EqualTo(5f));
    }

    // ── ShouldSpawn ───────────────────────────────────────────────────────────

    [Test]
    public void ShouldSpawn_BeforeIntervalElapsed_ReturnsFalse()
    {
        var clock = RealTimeClockAt(7);
        // Rush-hour interval is 2 s; after 1 s it should not yet fire.
        Assert.That(clock.ShouldSpawn(1f), Is.False);
    }

    [Test]
    public void ShouldSpawn_ExactlyAtInterval_ReturnsTrue()
    {
        var clock = RealTimeClockAt(7);
        Assert.That(clock.ShouldSpawn(2f), Is.True);
    }

    [Test]
    public void ShouldSpawn_AfterFiring_ResetsTimerAndReturnsFalse()
    {
        var clock = RealTimeClockAt(7);
        clock.ShouldSpawn(2f); // fires and resets _lastSpawnTime to 2
        // Still at simTime=2 — interval hasn't elapsed again
        Assert.That(clock.ShouldSpawn(2f), Is.False);
    }

    [Test]
    public void ShouldSpawn_AfterReset_FiresAgainFromZero()
    {
        var clock = RealTimeClockAt(7);
        clock.ShouldSpawn(2f); // first fire
        clock.Reset();
        // After reset _lastSpawnTime=0, so 2 s ≥ interval of 2 s → fires again
        Assert.That(clock.ShouldSpawn(2f), Is.True);
    }

    // ── GetTrafficDemandFraction ──────────────────────────────────────────────

    [Test]
    public void GetTrafficDemandFraction_AmRushWindow_ReturnsOne()
    {
        // DemandByHour[7] = 1.0, DemandByHour[8] = 1.0 → avg = 1.0
        var clock = RealTimeClockAt(7);
        Assert.That(clock.GetTrafficDemandFraction(7, 9), Is.EqualTo(1.0f).Within(0.001f));
    }

    [Test]
    public void GetTrafficDemandFraction_DeepNightWindow_ReturnsLowDemand()
    {
        // DemandByHour[0..3] all = 0.10 → avg = 0.10
        var clock = RealTimeClockAt(0);
        Assert.That(clock.GetTrafficDemandFraction(0, 4), Is.EqualTo(0.10f).Within(0.001f));
    }

    [Test]
    public void GetTrafficDemandFraction_SameStartAndEnd_ReturnsThatHoursDemand()
    {
        // When start == end, return DemandByHour[start]
        var clock = RealTimeClockAt(7);
        Assert.That(clock.GetTrafficDemandFraction(7, 7), Is.EqualTo(1.0f).Within(0.001f));
    }

    [Test]
    public void GetTrafficDemandFraction_MidnightWrap_AveragesCorrectly()
    {
        // 22→2 wraps midnight: hours 22(0.20), 23(0.15), 0(0.10), 1(0.10) → avg = 0.55/4 = 0.1375
        var clock = RealTimeClockAt(22);
        Assert.That(clock.GetTrafficDemandFraction(22, 2), Is.EqualTo(0.1375f).Within(0.001f));
    }

    // ── GetWindowDurationSeconds ──────────────────────────────────────────────

    [Test]
    public void GetWindowDurationSeconds_TwoHourWindow_Returns7200Seconds()
    {
        // 2 hours * 60 min / (1/60 simMin/s) = 7200 real seconds
        var clock = RealTimeClockAt(7);
        Assert.That(clock.GetWindowDurationSeconds(7, 9), Is.EqualTo(7200f).Within(0.01f));
    }

    [Test]
    public void GetWindowDurationSeconds_MidnightWrap_Returns25200Seconds()
    {
        // 22→05 = 7 hours → 7*60*60 = 25200 s
        var clock = RealTimeClockAt(22);
        Assert.That(clock.GetWindowDurationSeconds(22, 5), Is.EqualTo(25200f).Within(0.01f));
    }

    [Test]
    public void GetWindowDurationSeconds_SameHour_TreatedAs24HourWindow()
    {
        // same start/end → hours=0 → coerced to 24 → 86400 s
        var clock = RealTimeClockAt(7);
        Assert.That(clock.GetWindowDurationSeconds(7, 7), Is.EqualTo(86400f).Within(0.01f));
    }

    // ── FormatObservationWindow ───────────────────────────────────────────────

    [TestCase(7, 9,  "07:00\u201309:00")]
    [TestCase(0, 23, "00:00\u201323:00")]
    public void FormatObservationWindow_FormatsCorrectly(int start, int end, string expected)
    {
        Assert.That(SimClock.FormatObservationWindow(start, end), Is.EqualTo(expected));
    }
}
