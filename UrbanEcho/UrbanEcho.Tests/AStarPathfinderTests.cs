using Mapsui;
using Mapsui.Layers;
using UrbanEcho.Graph;

namespace UrbanEcho.Tests;

/// <summary>
/// Unit tests for AStarPathfinder: path correctness, cost preferences,
/// closed-edge avoidance, truck-restriction enforcement, and turn penalties.
/// All tests use lightweight in-memory RoadGraph instances — no UI or
/// SimManager dependency.
/// </summary>
public class AStarPathfinderTests
{
    [SetUp]
    public void Setup()
    {
        Helpers.Helper.TestMode = true;
    }

    // ── Graph-builder helpers ─────────────────────────────────────────────────

    private static RoadNode N(int id, double x, double y) => new RoadNode(id, x, y);

    private static RoadEdge Edge(
        int from, int to,
        double length, double speedLimitMs,
        RoadType roadType = RoadType.Arterial,
        bool truckAllowance = true,
        bool closed = false)
    {
        var metadata = new RoadMetadata
        {
            SpeedLimit = speedLimitMs,
            TruckAllowance = truckAllowance,
            RoadType = roadType
        };
        var feature = new PointFeature(new MPoint(0, 0));
        var edge = new RoadEdge(from, to, length, metadata, feature, isFromStartOfLineString: true);
        if (closed) edge.Close();
        return edge;
    }

    private static RoadGraph Graph(IEnumerable<RoadNode> nodes, IEnumerable<RoadEdge> edges)
    {
        var nodeDict = nodes.ToDictionary(n => n.Id);
        return new RoadGraph(nodeDict, edges.ToList());
    }

    // ── FindPath: basic correctness ───────────────────────────────────────────

    [Test]
    public void FindPath_DirectConnection_ReturnsTwoNodePath()
    {
        // 0 ──► 1
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0)],
            [Edge(0, 1, 100, 10)]);

        var path = new AStarPathfinder(graph).FindPath(0, 1);

        Assert.That(path, Is.EqualTo(new[] { 0, 1 }));
    }

    [Test]
    public void FindPath_ThreeNodeLine_ReturnsFullPath()
    {
        // 0 ──► 1 ──► 2
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0), N(2, 200, 0)],
            [Edge(0, 1, 100, 10), Edge(1, 2, 100, 10)]);

        var path = new AStarPathfinder(graph).FindPath(0, 2);

        Assert.That(path, Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void FindPath_StartEqualsGoal_ReturnsSingleNodePath()
    {
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0)],
            [Edge(0, 1, 100, 10)]);

        var path = new AStarPathfinder(graph).FindPath(0, 0);

        Assert.That(path, Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public void FindPath_DisconnectedNodes_ReturnsEmpty()
    {
        // Two nodes, no edges between them
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0)],
            []);

        var path = new AStarPathfinder(graph).FindPath(0, 1);

        Assert.That(path, Is.Empty);
    }

    // ── FindPath: closed-edge avoidance ───────────────────────────────────────

    [Test]
    public void FindPath_DirectEdgeClosed_UsesDetour()
    {
        // 0 ──[closed]──► 1
        // 0 ──► 2 ──────► 1
        var graph = Graph(
            [N(0, 0, 0), N(1, 200, 0), N(2, 100, 100)],
            [
                Edge(0, 1, 200, 10, closed: true),  // blocked
                Edge(0, 2, 100, 10),
                Edge(2, 1, 100, 10)
            ]);

        var path = new AStarPathfinder(graph).FindPath(0, 1);

        Assert.That(path, Is.EqualTo(new[] { 0, 2, 1 }));
    }

    [Test]
    public void FindPath_AllPathsClosed_ReturnsEmpty()
    {
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0)],
            [Edge(0, 1, 100, 10, closed: true)]);

        var path = new AStarPathfinder(graph).FindPath(0, 1);

        Assert.That(path, Is.Empty);
    }

    // ── FindPath: road-type cost preference ──────────────────────────────────

    [Test]
    public void FindPath_PrefersFasterRoadType_ChoosesArterialOverLocalStreet()
    {
        // Two paths from 0 to 3 with equal edge length and speed limit.
        // Path A (0→1→3): LocalStreet multiplier = 3.0  → cost = 2 * (100/10 * 3.0) = 60
        // Path B (0→2→3): Arterial multiplier  = 1.0  → cost = 2 * (100/10 * 1.0) = 20
        var graph = Graph(
            [N(0, 0, 0), N(1, 50, 50), N(2, 50, -50), N(3, 100, 0)],
            [
                Edge(0, 1, 100, 10, RoadType.LocalStreet),
                Edge(1, 3, 100, 10, RoadType.LocalStreet),
                Edge(0, 2, 100, 10, RoadType.Arterial),
                Edge(2, 3, 100, 10, RoadType.Arterial)
            ]);

        var path = new AStarPathfinder(graph).FindPath(0, 3);

        Assert.That(path, Is.EqualTo(new[] { 0, 2, 3 }));
    }

    // ── FindPath: truck-restriction enforcement ───────────────────────────────

    [Test]
    public void FindPath_TruckOnNoTruckEdge_BlockedAndUsesDetour()
    {
        // 0 ──[no-truck]──► 1
        // 0 ──► 2 ─────────► 1  (truck-allowed detour)
        var graph = Graph(
            [N(0, 0, 0), N(1, 200, 0), N(2, 100, 100)],
            [
                Edge(0, 1, 200, 10, truckAllowance: false),
                Edge(0, 2, 100, 10, truckAllowance: true),
                Edge(2, 1, 100, 10, truckAllowance: true)
            ]);

        var path = new AStarPathfinder(graph, isTruck: true).FindPath(0, 1);

        Assert.That(path, Is.EqualTo(new[] { 0, 2, 1 }));
    }

    [Test]
    public void FindPath_CarOnNoTruckEdge_TakesDirectRoute()
    {
        // Non-truck vehicles are not restricted — they take the direct edge.
        var graph = Graph(
            [N(0, 0, 0), N(1, 200, 0), N(2, 100, 100)],
            [
                Edge(0, 1, 200, 10, truckAllowance: false),
                Edge(0, 2, 100, 10, truckAllowance: true),
                Edge(2, 1, 100, 10, truckAllowance: true)
            ]);

        var path = new AStarPathfinder(graph, isTruck: false).FindPath(0, 1);

        Assert.That(path, Is.EqualTo(new[] { 0, 1 }));
    }

    // ── FindPath: turn-penalty preference ────────────────────────────────────

    [Test]
    public void FindPath_StraightRouteVsTurningRoute_PrefersStraight()
    {
        // Layout (all edges identical length=100, speed=10, Arterial):
        //
        //   3 (50, 50)          (off to the side — forces a turn)
        //  / \
        // 0   2 (200, 0)
        //  \ /
        //   1 (100, 0)          (perfectly in line with 0 and 2)
        //
        // Path A: 0 → 1 → 2  — straight (angle ≈ 0°), no turn penalty  → cost ≈ 20
        // Path B: 0 → 3 → 2  — sharp bend at 3 (≈63°) → +8 s penalty   → cost ≈ 28
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0), N(2, 200, 0), N(3, 50, 50)],
            [
                Edge(0, 1, 100, 10),
                Edge(1, 2, 100, 10),
                Edge(0, 3, 100, 10),
                Edge(3, 2, 100, 10)
            ]);

        var path = new AStarPathfinder(graph).FindPath(0, 2);

        Assert.That(path, Is.EqualTo(new[] { 0, 1, 2 }));
    }

    // ── FindPathEdges: consistency with FindPath ──────────────────────────────

    [Test]
    public void FindPathEdges_ThreeNodeLine_ReturnsTwoEdges()
    {
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0), N(2, 200, 0)],
            [Edge(0, 1, 100, 10), Edge(1, 2, 100, 10)]);

        var edges = new AStarPathfinder(graph).FindPathEdges(0, 2);

        Assert.That(edges.Count, Is.EqualTo(2));
        Assert.That(edges[0].From, Is.EqualTo(0));
        Assert.That(edges[0].To, Is.EqualTo(1));
        Assert.That(edges[1].From, Is.EqualTo(1));
        Assert.That(edges[1].To, Is.EqualTo(2));
    }

    [Test]
    public void FindPathEdges_DisconnectedNodes_ReturnsEmpty()
    {
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0)],
            []);

        var edges = new AStarPathfinder(graph).FindPathEdges(0, 1);

        Assert.That(edges, Is.Empty);
    }

    [Test]
    public void FindPathEdges_EdgeCount_IsOneFewerThanNodeCount()
    {
        // For any simple path the edge list must be exactly (nodes - 1).
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0), N(2, 200, 0), N(3, 300, 0)],
            [Edge(0, 1, 100, 10), Edge(1, 2, 100, 10), Edge(2, 3, 100, 10)]);

        var nodePath = new AStarPathfinder(graph).FindPath(0, 3);
        var edgePath = new AStarPathfinder(graph).FindPathEdges(0, 3);

        Assert.That(edgePath.Count, Is.EqualTo(nodePath.Count - 1));
    }

    // ── TravelTimeSeconds: edge cost math ─────────────────────────────────────

    [TestCase(100.0, 10.0, 10.0)]   // 100 m at 10 m/s → 10 s
    [TestCase(500.0, 25.0, 20.0)]   // 500 m at 25 m/s → 20 s
    [TestCase(36.0, 18.0, 2.0)]   // 36 m at 18 m/s → 2 s
    public void TravelTimeSeconds_ComputedFromLengthAndSpeedLimit(
        double length, double speedMs, double expectedSeconds)
    {
        var edge = Edge(0, 1, length, speedMs);
        Assert.That(edge.TravelTimeSeconds, Is.EqualTo(expectedSeconds).Within(0.001));
    }

    // ── RoutingCostMultiplier: road-type weighting ────────────────────────────

    [TestCase(RoadType.Freeway, 0.9)]
    [TestCase(RoadType.Arterial, 1.0)]
    [TestCase(RoadType.Collector, 1.2)]
    [TestCase(RoadType.LocalStreet, 3.0)]
    [TestCase(RoadType.AlleywayLane, 5.0)]
    [TestCase(RoadType.Private, 6.0)]
    public void RoutingCostMultiplier_ReturnsExpectedWeight(RoadType type, double expected)
    {
        Assert.That(type.RoutingCostMultiplier(), Is.EqualTo(expected).Within(0.001));
    }
}