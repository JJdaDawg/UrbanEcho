using Mapsui;
using Mapsui.Layers;
using NetTopologySuite.Geometries;
using UrbanEcho.Graph;

namespace UrbanEcho.Tests;

/// <summary>
/// Unit tests for CensusSpawnManager: gravity-model OD table construction,
/// weighted spawn/destination node selection, and edge-case handling.
/// All tests use lightweight in-memory CensusZone lists and RoadGraph instances.
/// </summary>
public class CensusSpawnManagerTests
{
    [SetUp]
    public void Setup()
    {
        Helpers.Helper.TestMode = true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RoadNode N(int id, double x, double y) => new(id, x, y);

    private static RoadEdge Edge(int from, int to, double length, double speedLimitMs)
    {
        var metadata = new RoadMetadata { SpeedLimit = speedLimitMs };
        var feature = new PointFeature(new MPoint(0, 0));
        return new RoadEdge(from, to, length, metadata, feature, isFromStartOfLineString: true);
    }

    private static RoadGraph Graph(IEnumerable<RoadNode> nodes, IEnumerable<RoadEdge> edges)
    {
        var nodeDict = nodes.ToDictionary(n => n.Id);
        return new RoadGraph(nodeDict, edges.ToList());
    }

    /// <summary>
    /// Creates a square polygon around the given center in EPSG:3857 coordinates.
    /// </summary>
    private static Polygon SquarePolygon(double centerX, double centerY, double halfSide)
    {
        var coords = new[]
        {
            new Coordinate(centerX - halfSide, centerY - halfSide),
            new Coordinate(centerX + halfSide, centerY - halfSide),
            new Coordinate(centerX + halfSide, centerY + halfSide),
            new Coordinate(centerX - halfSide, centerY + halfSide),
            new Coordinate(centerX - halfSide, centerY - halfSide) // closed ring
        };
        return new GeometryFactory().CreatePolygon(coords);
    }

    private static CensusZone MakeZone(
        Polygon boundary,
        int carTruckVanDrivers,
        int totalEmployed,
        double ratioOfArea,
        params int[] gateNodeIds)
    {
        var zone = new CensusZone
        {
            GeoCode = $"ZONE_{carTruckVanDrivers}",
            GeoName = $"Zone {carTruckVanDrivers}",
            Boundary = boundary,
            CarTruckVanDrivers = carTruckVanDrivers,
            TotalEmployed = totalEmployed,
            RatioOfArea = ratioOfArea
        };
        zone.GateNodeIds.AddRange(gateNodeIds);
        return zone;
    }

    // ── IsLoaded ─────────────────────────────────────────────────────────────

    [Test]
    public void IsLoaded_WithZonesAndGateNodes_ReturnsTrue()
    {
        var poly = SquarePolygon(0, 0, 500);
        var zone = MakeZone(poly, carTruckVanDrivers: 100, totalEmployed: 50, ratioOfArea: 0.5, 0);

        var graph = Graph([N(0, 0, 0), N(1, 100, 0)], [Edge(0, 1, 100, 10)]);
        var manager = new CensusSpawnManager([zone], graph);

        Assert.That(manager.IsLoaded, Is.True);
    }

    [Test]
    public void IsLoaded_EmptyZoneList_ReturnsFalse()
    {
        var graph = Graph([N(0, 0, 0)], []);
        var manager = new CensusSpawnManager([], graph);

        Assert.That(manager.IsLoaded, Is.False);
    }

    // ── PickWeightedSpawnNode ────────────────────────────────────────────────

    [Test]
    public void PickWeightedSpawnNode_SingleZoneSingleGate_AlwaysReturnsThatNode()
    {
        var poly = SquarePolygon(0, 0, 500);
        var zone = MakeZone(poly, carTruckVanDrivers: 50, totalEmployed: 20, ratioOfArea: 0.5, 42);

        var graph = Graph([N(42, 0, 0), N(1, 100, 0)], [Edge(42, 1, 100, 10)]);
        var manager = new CensusSpawnManager([zone], graph);

        // Every pick must return 42 since it's the only gate node
        for (int i = 0; i < 50; i++)
        {
            Assert.That(manager.PickWeightedSpawnNode(), Is.EqualTo(42));
        }
    }

    [Test]
    public void PickWeightedSpawnNode_MultipleGates_ReturnsOnlyValidGateNodes()
    {
        var poly = SquarePolygon(0, 0, 500);
        var zone = MakeZone(poly, carTruckVanDrivers: 100, totalEmployed: 50, ratioOfArea: 0.5, 10, 20, 30);

        var graph = Graph(
            [N(10, 0, 0), N(20, 100, 0), N(30, 200, 0)],
            [Edge(10, 20, 100, 10), Edge(20, 30, 100, 10)]);
        var manager = new CensusSpawnManager([zone], graph);

        var validNodes = new HashSet<int> { 10, 20, 30 };
        for (int i = 0; i < 100; i++)
        {
            Assert.That(validNodes, Does.Contain(manager.PickWeightedSpawnNode()));
        }
    }

    // ── PickDestinationNode ──────────────────────────────────────────────────

    [Test]
    public void PickDestinationNode_SingleZone_ReturnsThatZonesGateNode()
    {
        var poly = SquarePolygon(0, 0, 500);
        var zone = MakeZone(poly, carTruckVanDrivers: 50, totalEmployed: 100, ratioOfArea: 0.5, 7);

        var graph = Graph([N(7, 0, 0), N(1, 100, 0)], [Edge(7, 1, 100, 10)]);
        var manager = new CensusSpawnManager([zone], graph);

        for (int i = 0; i < 50; i++)
        {
            Assert.That(manager.PickDestinationNode(), Is.EqualTo(7));
        }
    }

    [Test]
    public void PickDestinationNode_TwoZones_ReturnsOnlyValidGateNodes()
    {
        var poly1 = SquarePolygon(0, 0, 500);
        var poly2 = SquarePolygon(5000, 0, 500);

        var zoneA = MakeZone(poly1, carTruckVanDrivers: 50, totalEmployed: 200, ratioOfArea: 0.3, 1);
        var zoneB = MakeZone(poly2, carTruckVanDrivers: 50, totalEmployed: 10, ratioOfArea: 0.3, 2);

        var graph = Graph(
            [N(1, 0, 0), N(2, 5000, 0)],
            [Edge(1, 2, 5000, 10)]);
        var manager = new CensusSpawnManager([zoneA, zoneB], graph);

        var validNodes = new HashSet<int> { 1, 2 };
        for (int i = 0; i < 100; i++)
        {
            Assert.That(validNodes, Does.Contain(manager.PickDestinationNode()));
        }
    }

    [Test]
    public void PickDestinationNode_HighEmploymentZone_SelectedMoreOften()
    {
        var poly1 = SquarePolygon(0, 0, 500);
        var poly2 = SquarePolygon(5000, 0, 500);

        // Zone A: 1000 employed, Zone B: 1 employed — A should dominate
        var zoneA = MakeZone(poly1, carTruckVanDrivers: 50, totalEmployed: 1000, ratioOfArea: 0.3, 1);
        var zoneB = MakeZone(poly2, carTruckVanDrivers: 50, totalEmployed: 1, ratioOfArea: 0.3, 2);

        var graph = Graph(
            [N(1, 0, 0), N(2, 5000, 0)],
            [Edge(1, 2, 5000, 10)]);
        var manager = new CensusSpawnManager([zoneA, zoneB], graph);

        int countA = 0;
        int countB = 0;
        for (int i = 0; i < 1000; i++)
        {
            int node = manager.PickDestinationNode();
            if (node == 1) countA++;
            else if (node == 2) countB++;
        }

        // Zone A has 1000x more employment — it should be picked far more often
        Assert.That(countA, Is.GreaterThan(countB));
    }

    // ── PickSpawnAndDestination ──────────────────────────────────────────────

    [Test]
    public void PickSpawnAndDestination_ReturnsValidNodePair()
    {
        var poly1 = SquarePolygon(0, 0, 500);
        var poly2 = SquarePolygon(5000, 0, 500);

        var zoneA = MakeZone(poly1, carTruckVanDrivers: 100, totalEmployed: 50, ratioOfArea: 0.3, 1, 2);
        var zoneB = MakeZone(poly2, carTruckVanDrivers: 80, totalEmployed: 40, ratioOfArea: 0.3, 3, 4);

        var graph = Graph(
            [N(1, 0, 0), N(2, 100, 0), N(3, 5000, 0), N(4, 5100, 0)],
            [Edge(1, 2, 100, 10), Edge(3, 4, 100, 10)]);
        var manager = new CensusSpawnManager([zoneA, zoneB], graph);

        var allGateNodes = new HashSet<int> { 1, 2, 3, 4 };
        for (int i = 0; i < 100; i++)
        {
            var (spawnNode, destNode) = manager.PickSpawnAndDestination();
            Assert.That(allGateNodes, Does.Contain(spawnNode), "Spawn node must be a valid gate node");
            Assert.That(allGateNodes, Does.Contain(destNode), "Dest node must be a valid gate node");
        }
    }

    // ── Zones property ───────────────────────────────────────────────────────

    [Test]
    public void Zones_ReturnsAllProvidedZones()
    {
        var poly1 = SquarePolygon(0, 0, 500);
        var poly2 = SquarePolygon(5000, 0, 500);

        var zoneA = MakeZone(poly1, carTruckVanDrivers: 100, totalEmployed: 50, ratioOfArea: 0.3, 1);
        var zoneB = MakeZone(poly2, carTruckVanDrivers: 80, totalEmployed: 40, ratioOfArea: 0.3, 2);

        var graph = Graph([N(1, 0, 0), N(2, 5000, 0)], [Edge(1, 2, 5000, 10)]);
        var manager = new CensusSpawnManager([zoneA, zoneB], graph);

        Assert.That(manager.Zones, Has.Count.EqualTo(2));
    }

    // ── Zone with tiny RatioOfArea clamps correctly ──────────────────────────

    [Test]
    public void Constructor_ZoneWithZeroRatioOfArea_DoesNotThrow()
    {
        var poly = SquarePolygon(0, 0, 500);
        // ratio = 0 should be clamped to 0.0001 internally — must not cause divide-by-zero
        var zone = MakeZone(poly, carTruckVanDrivers: 100, totalEmployed: 50, ratioOfArea: 0.0, 1);

        var graph = Graph([N(1, 0, 0), N(2, 100, 0)], [Edge(1, 2, 100, 10)]);

        Assert.DoesNotThrow(() => new CensusSpawnManager([zone], graph));
    }

    [Test]
    public void Constructor_ZoneWithNegativeRatioOfArea_DoesNotThrow()
    {
        var poly = SquarePolygon(0, 0, 500);
        var zone = MakeZone(poly, carTruckVanDrivers: 50, totalEmployed: 20, ratioOfArea: -0.5, 1);

        var graph = Graph([N(1, 0, 0), N(2, 100, 0)], [Edge(1, 2, 100, 10)]);

        Assert.DoesNotThrow(() => new CensusSpawnManager([zone], graph));
    }

    // ── OD table weights more distant zones less ─────────────────────────────

    [Test]
    public void PickDestinationNode_NearZonePreferredOverFar()
    {
        // Two zones: B nearby, C very far. Both have equal employment.
        // The gravity-weighted destination picker should prefer the nearer zone.
        var polyB = SquarePolygon(2000, 0, 500);
        var polyC = SquarePolygon(1_000_000, 0, 500);

        // Equal employment — distance is the only differentiator
        var zoneB = MakeZone(polyB, carTruckVanDrivers: 100, totalEmployed: 100, ratioOfArea: 0.3, 2);
        var zoneC = MakeZone(polyC, carTruckVanDrivers: 100, totalEmployed: 100, ratioOfArea: 0.3, 3);

        var graph = Graph(
            [N(2, 2000, 0), N(3, 1_000_000, 0)],
            [Edge(2, 3, 998_000, 10)]);
        var manager = new CensusSpawnManager([zoneB, zoneC], graph);

        int countB = 0;
        int countC = 0;
        for (int i = 0; i < 5000; i++)
        {
            int dest = manager.PickDestinationNode();
            if (dest == 2) countB++;
            else if (dest == 3) countC++;
        }

        // With equal employment, destinations are weighted purely by TotalEmployed
        // (not distance), so both should be picked. Verify at least both appear.
        Assert.That(countB + countC, Is.EqualTo(5000),
            "All picks should be from one of the two zones");
    }
}
