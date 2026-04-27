using UrbanEcho.Reporting;

namespace UrbanEcho.Tests;

/// <summary>
/// Unit tests for RecordedStats and Stats: vehicle recording, running averages,
/// reset behavior, and SetClosed flag.
/// </summary>
public class RecordedStatsTests
{
    [SetUp]
    public void Setup()
    {
        Helpers.Helper.TestMode = true;
    }

    // ── Initial state ────────────────────────────────────────────────────────

    [Test]
    public void NewRecordedStats_AllZero()
    {
        var rs = new RecordedStats();

        Assert.That(rs.VehicleCount, Is.EqualTo(0));
        Assert.That(rs.AverageTimeSpent, Is.EqualTo(0));
        Assert.That(rs.TotalTimeSpent, Is.EqualTo(0));
        Assert.That(rs.AverageSpeed, Is.EqualTo(0));
        Assert.That(rs.AverageWaitTime, Is.EqualTo(0));
        Assert.That(rs.TotalWaitTime, Is.EqualTo(0));
        Assert.That(rs.Closed, Is.EqualTo(0));
    }

    // ── RecordVehicle ────────────────────────────────────────────────────────

    [Test]
    public void RecordVehicle_SingleVehicle_SetsCorrectValues()
    {
        var rs = new RecordedStats();
        var stats = new Stats { ElaspedTime = 10.0, AverageSpeed = 30.0, WaitTime = 2.0 };

        rs.RecordVehicle(stats);

        Assert.That(rs.VehicleCount, Is.EqualTo(1));
        Assert.That(rs.TotalTimeSpent, Is.EqualTo(10.0).Within(0.001));
        Assert.That(rs.AverageTimeSpent, Is.EqualTo(10.0).Within(0.001));
        Assert.That(rs.AverageSpeed, Is.EqualTo(30.0).Within(0.001));
        Assert.That(rs.TotalWaitTime, Is.EqualTo(2.0).Within(0.001));
        Assert.That(rs.AverageWaitTime, Is.EqualTo(2.0).Within(0.001));
    }

    [Test]
    public void RecordVehicle_TwoVehicles_AveragesCorrectly()
    {
        var rs = new RecordedStats();

        rs.RecordVehicle(new Stats { ElaspedTime = 10.0, AverageSpeed = 20.0, WaitTime = 4.0 });
        rs.RecordVehicle(new Stats { ElaspedTime = 20.0, AverageSpeed = 40.0, WaitTime = 6.0 });

        Assert.That(rs.VehicleCount, Is.EqualTo(2));
        Assert.That(rs.TotalTimeSpent, Is.EqualTo(30.0).Within(0.001));
        Assert.That(rs.AverageTimeSpent, Is.EqualTo(15.0).Within(0.001));
        Assert.That(rs.AverageSpeed, Is.EqualTo(30.0).Within(0.001));
        Assert.That(rs.TotalWaitTime, Is.EqualTo(10.0).Within(0.001));
        Assert.That(rs.AverageWaitTime, Is.EqualTo(5.0).Within(0.001));
    }

    [Test]
    public void RecordVehicle_IncrementingCount()
    {
        var rs = new RecordedStats();

        for (int i = 1; i <= 5; i++)
        {
            rs.RecordVehicle(new Stats { ElaspedTime = 1.0, AverageSpeed = 10.0, WaitTime = 0.5 });
            Assert.That(rs.VehicleCount, Is.EqualTo(i));
        }
    }

    // ── SetClosed ────────────────────────────────────────────────────────────

    [Test]
    public void SetClosed_FlagsSetsToOne()
    {
        var rs = new RecordedStats();

        rs.SetClosed();

        Assert.That(rs.Closed, Is.EqualTo(1));
    }

    [Test]
    public void SetClosed_CalledTwice_StillOne()
    {
        var rs = new RecordedStats();

        rs.SetClosed();
        rs.SetClosed();

        Assert.That(rs.Closed, Is.EqualTo(1));
    }

    // ── SetPosition ──────────────────────────────────────────────────────────

    [Test]
    public void SetPosition_StoresLatLon()
    {
        var rs = new RecordedStats();

        rs.SetPosition(43.45, -80.49);

        Assert.That(rs.Lat, Is.EqualTo(43.45).Within(0.001));
        Assert.That(rs.Lon, Is.EqualTo(-80.49).Within(0.001));
    }

    // ── Reset ────────────────────────────────────────────────────────────────

    [Test]
    public void Reset_ClearsAllValues()
    {
        var rs = new RecordedStats();
        rs.RecordVehicle(new Stats { ElaspedTime = 10.0, AverageSpeed = 30.0, WaitTime = 2.0 });
        rs.SetClosed();
        rs.SetPosition(43.45, -80.49);

        rs.Reset();

        Assert.That(rs.VehicleCount, Is.EqualTo(0));
        Assert.That(rs.AverageTimeSpent, Is.EqualTo(0));
        Assert.That(rs.TotalTimeSpent, Is.EqualTo(0));
        Assert.That(rs.AverageSpeed, Is.EqualTo(0));
        Assert.That(rs.AverageWaitTime, Is.EqualTo(0));
        Assert.That(rs.TotalWaitTime, Is.EqualTo(0));
        Assert.That(rs.Closed, Is.EqualTo(0));
    }

    [Test]
    public void Reset_ThenRecord_StartsFromScratch()
    {
        var rs = new RecordedStats();
        rs.RecordVehicle(new Stats { ElaspedTime = 100.0, AverageSpeed = 50.0, WaitTime = 20.0 });

        rs.Reset();
        rs.RecordVehicle(new Stats { ElaspedTime = 5.0, AverageSpeed = 10.0, WaitTime = 1.0 });

        Assert.That(rs.VehicleCount, Is.EqualTo(1));
        Assert.That(rs.TotalTimeSpent, Is.EqualTo(5.0).Within(0.001));
        Assert.That(rs.AverageSpeed, Is.EqualTo(10.0).Within(0.001));
    }

    // ── Stats.Reset ──────────────────────────────────────────────────────────

    [Test]
    public void StatsReset_ClearsAllFields()
    {
        var stats = new Stats { ElaspedTime = 10.0, WaitTime = 3.0, AverageSpeed = 25.0 };

        stats.Reset();

        Assert.That(stats.ElaspedTime, Is.EqualTo(0));
        Assert.That(stats.WaitTime, Is.EqualTo(0));
        Assert.That(stats.AverageSpeed, Is.EqualTo(0));
    }
}
