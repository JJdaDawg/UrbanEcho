using UrbanEcho.Graph;

namespace UrbanEcho.Tests;

/// <summary>
/// Unit tests for RoadType enum, RoutingCostMultiplier values, and
/// ParseCartoClass shapefile string parsing.
/// </summary>
public class RoadTypeTests
{
    [SetUp]
    public void Setup()
    {
        Helpers.Helper.TestMode = true;
    }

    // ── RoutingCostMultiplier ────────────────────────────────────────────────

    [TestCase(RoadType.Freeway, 0.9)]
    [TestCase(RoadType.Expressway, 0.9)]
    [TestCase(RoadType.Arterial, 1.0)]
    [TestCase(RoadType.Collector, 2.0)]
    [TestCase(RoadType.Ramp, 1.0)]
    [TestCase(RoadType.Roundabout, 1.1)]
    [TestCase(RoadType.LocalStreet, 3.0)]
    [TestCase(RoadType.AlleywayLane, 5.0)]
    [TestCase(RoadType.CulDeSac, 5.0)]
    [TestCase(RoadType.Private, 6.0)]
    [TestCase(RoadType.Unknown, 2.0)]
    public void RoutingCostMultiplier_ReturnsExpectedValue(RoadType roadType, double expected)
    {
        Assert.That(roadType.RoutingCostMultiplier(), Is.EqualTo(expected).Within(0.001));
    }

    [Test]
    public void RoutingCostMultiplier_HighwayTypesLowerThanLocalStreet()
    {
        Assert.That(RoadType.Freeway.RoutingCostMultiplier(),
            Is.LessThan(RoadType.LocalStreet.RoutingCostMultiplier()));
    }

    [Test]
    public void RoutingCostMultiplier_AllValuesPositive()
    {
        foreach (RoadType rt in Enum.GetValues<RoadType>())
        {
            Assert.That(rt.RoutingCostMultiplier(), Is.GreaterThan(0),
                $"{rt} should have a positive routing cost multiplier");
        }
    }

    // ── ParseCartoClass ──────────────────────────────────────────────────────

    [TestCase("Freeway", RoadType.Freeway)]
    [TestCase("Expressway / Highway", RoadType.Expressway)]
    [TestCase("Arterial", RoadType.Arterial)]
    [TestCase("Collector", RoadType.Collector)]
    [TestCase("Local Street", RoadType.LocalStreet)]
    [TestCase("Ramp", RoadType.Ramp)]
    [TestCase("Roundabout", RoadType.Roundabout)]
    [TestCase("Alleyway / Lane", RoadType.AlleywayLane)]
    [TestCase("Cul-de-Sac", RoadType.CulDeSac)]
    [TestCase("Private", RoadType.Private)]
    public void ParseCartoClass_KnownValues_ReturnsCorrectType(string input, RoadType expected)
    {
        Assert.That(RoadTypeExtensions.ParseCartoClass(input), Is.EqualTo(expected));
    }

    [Test]
    public void ParseCartoClass_UnknownString_ReturnsUnknown()
    {
        Assert.That(RoadTypeExtensions.ParseCartoClass("SomethingElse"), Is.EqualTo(RoadType.Unknown));
    }

    [Test]
    public void ParseCartoClass_Null_ReturnsUnknown()
    {
        Assert.That(RoadTypeExtensions.ParseCartoClass(null), Is.EqualTo(RoadType.Unknown));
    }

    [Test]
    public void ParseCartoClass_EmptyString_ReturnsUnknown()
    {
        Assert.That(RoadTypeExtensions.ParseCartoClass(""), Is.EqualTo(RoadType.Unknown));
    }

    [Test]
    public void ParseCartoClass_WhitespaceOnly_ReturnsUnknown()
    {
        Assert.That(RoadTypeExtensions.ParseCartoClass("   "), Is.EqualTo(RoadType.Unknown));
    }

    [Test]
    public void ParseCartoClass_ValueWithLeadingTrailingSpaces_TrimsAndMatches()
    {
        Assert.That(RoadTypeExtensions.ParseCartoClass("  Arterial  "), Is.EqualTo(RoadType.Arterial));
    }
}
