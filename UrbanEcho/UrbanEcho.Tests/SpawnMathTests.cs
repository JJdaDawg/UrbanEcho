namespace UrbanEcho.Tests;

public class SpawnMathTests
{
    [SetUp]
    public void Setup()
    {
        Helpers.Helper.TestMode = true;
    }

    private static int BurstShare(int spawnerVpm, int totalVpm, int targetCount) =>
        totalVpm > 0
            ? Math.Max(1, (int)Math.Round((double)spawnerVpm / totalVpm * targetCount))
            : 1;

    [TestCase(5, 10, 20, 10)]
    [TestCase(3, 10, 10, 3)]
    [TestCase(7, 10, 10, 7)]
    [TestCase(1, 10, 10, 1)]
    [TestCase(10, 10, 10, 10)]
    public void BurstShare_ProportionalToVpm(int vpm, int totalVpm, int target, int expected)
    {
        Assert.That(BurstShare(vpm, totalVpm, target), Is.EqualTo(expected));
    }

    [Test]
    public void BurstShare_ZeroVpm_ReturnsMinimumOfOne()
    {
        Assert.That(BurstShare(0, 10, 20), Is.EqualTo(1));
    }

    [Test]
    public void BurstShare_ZeroTotalVpm_ReturnsOne()
    {
        // Guard: totalVpm=0 avoids division by zero and returns 1
        Assert.That(BurstShare(5, 0, 20), Is.EqualTo(1));
    }

    [Test]
    public void BurstShare_TwoEqualSpawners_SumsToTargetCount()
    {
        // Each of two equal spawners gets half; together they must equal the target.
        int target = 100;
        int share = BurstShare(5, 10, target);
        Assert.That(share * 2, Is.EqualTo(target));
    }

    [Test]
    public void BurstShare_RoundsCorrectly()
    {
        // 1/3 * 10 = 3.33… → rounds to 3
        Assert.That(BurstShare(1, 3, 10), Is.EqualTo(3));
    }

    private const int MaxVehicles = 5000;

    private static int TargetCount(int nodeCount, float demand) =>
        Math.Max(1, (int)(Math.Min(nodeCount, MaxVehicles) * demand));

    [TestCase(100, 1.00f, 100)]
    [TestCase(100, 0.50f, 50)]
    [TestCase(100, 0.10f, 10)]
    [TestCase(10000, 1.00f, 5000)]
    [TestCase(5000, 1.00f, 5000)]
    [TestCase(5001, 1.00f, 5000)]
    public void TargetCount_ScalesAndCapsCorrectly(int nodeCount, float demand, int expected)
    {
        Assert.That(TargetCount(nodeCount, demand), Is.EqualTo(expected));
    }

    [Test]
    public void TargetCount_NearZeroDemand_ReturnsMinimumOfOne()
    {
        Assert.That(TargetCount(10, 0.05f), Is.EqualTo(1));
    }

    [Test]
    public void TargetCount_ZeroNodes_ReturnsOne()
    {
        // An empty graph should never request 0 vehicles
        Assert.That(TargetCount(0, 1.0f), Is.EqualTo(1));
    }

    [Test]
    public void TargetCount_AmRushDemandWith1000Nodes_Returns1000()
    {
        Assert.That(TargetCount(1000, 1.0f), Is.EqualTo(1000));
    }

    [Test]
    public void TargetCount_NightDemandWith1000Nodes_Returns100()
    {
        Assert.That(TargetCount(1000, 0.10f), Is.EqualTo(100));
    }

    [Test]
    public void DemandFractionToTargetCount_AmRushPeakReturnsFullNodeCount()
    {
        var clock = new UrbanEcho.Sim.SimClock();
        float demand = clock.GetTrafficDemandFraction(7, 9); // = 1.0
        int target = TargetCount(500, demand);
        Assert.That(target, Is.EqualTo(500));
    }

    [Test]
    public void DemandFractionToTargetCount_DeepNightReducesFleet()
    {
        var clock = new UrbanEcho.Sim.SimClock();
        float demand = clock.GetTrafficDemandFraction(1, 4); // = 0.10
        int target = TargetCount(500, demand);
        Assert.That(target, Is.EqualTo(50));
    }
}