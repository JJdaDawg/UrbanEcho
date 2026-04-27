using Mapsui;
using Mapsui.Layers;
using UrbanEcho.Graph;

namespace UrbanEcho.Tests;

/// <summary>
/// Unit tests for PathStepBuilder: turn direction computation (straight, left,
/// right) and step list construction from A* edge output.
/// Uses point features (no LineString geometry) so turn directions are computed
/// from raw graph node positions.
/// </summary>
public class PathStepBuilderTests
{
    [SetUp]
    public void Setup()
    {
        Helpers.Helper.TestMode = true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RoadNode N(int id, double x, double y) => new(id, x, y);

    private static RoadEdge Edge(int from, int to, double length = 100, double speedLimitMs = 10)
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

    // ── Build: basic structure ───────────────────────────────────────────────

    [Test]
    public void Build_SingleEdge_ReturnsSingleStepWithStraight()
    {
        var edges = new List<RoadEdge> { Edge(0, 1) };
        var graph = Graph([N(0, 0, 0), N(1, 100, 0)], edges);

        var steps = PathStepBuilder.Build(edges, graph);

        Assert.That(steps, Has.Count.EqualTo(1));
        Assert.That(steps[0].Turn, Is.EqualTo(TurnDirection.Straight));
        Assert.That(steps[0].NextEdge, Is.Null);
    }

    [Test]
    public void Build_TwoEdges_ReturnsTwoSteps()
    {
        var e1 = Edge(0, 1);
        var e2 = Edge(1, 2);
        var edges = new List<RoadEdge> { e1, e2 };
        var graph = Graph([N(0, 0, 0), N(1, 100, 0), N(2, 200, 0)], edges);

        var steps = PathStepBuilder.Build(edges, graph);

        Assert.That(steps, Has.Count.EqualTo(2));
        Assert.That(steps[0].NextEdge, Is.Not.Null);
        Assert.That(steps[1].NextEdge, Is.Null);
    }

    [Test]
    public void Build_LastStep_AlwaysStraight()
    {
        // Even if edge geometry suggests a turn, the last step has no next edge → straight
        var e1 = Edge(0, 1);
        var e2 = Edge(1, 2);
        var edges = new List<RoadEdge> { e1, e2 };
        var graph = Graph([N(0, 0, 0), N(1, 100, 0), N(2, 200, 0)], edges);

        var steps = PathStepBuilder.Build(edges, graph);

        Assert.That(steps[^1].Turn, Is.EqualTo(TurnDirection.Straight));
    }

    // ── Turn direction: straight ─────────────────────────────────────────────

    [Test]
    public void Build_CollinearEdges_TurnIsStraight()
    {
        // 0 → 1 → 2 in a straight line along X axis
        var e1 = Edge(0, 1);
        var e2 = Edge(1, 2);
        var edges = new List<RoadEdge> { e1, e2 };
        var graph = Graph([N(0, 0, 0), N(1, 100, 0), N(2, 200, 0)], edges);

        var steps = PathStepBuilder.Build(edges, graph);

        Assert.That(steps[0].Turn, Is.EqualTo(TurnDirection.Straight));
    }

    // ── Turn direction: left ─────────────────────────────────────────────────

    [Test]
    public void Build_LeftTurn_TurnIsLeft()
    {
        // 0(0,0) → 1(100,0) → 2(100,100): 90° left turn
        var e1 = Edge(0, 1);
        var e2 = Edge(1, 2);
        var edges = new List<RoadEdge> { e1, e2 };
        var graph = Graph([N(0, 0, 0), N(1, 100, 0), N(2, 100, 100)], edges);

        var steps = PathStepBuilder.Build(edges, graph);

        Assert.That(steps[0].Turn, Is.EqualTo(TurnDirection.Left));
    }

    // ── Turn direction: right ────────────────────────────────────────────────

    [Test]
    public void Build_RightTurn_TurnIsRight()
    {
        // 0(0,0) → 1(100,0) → 2(100,-100): 90° right turn
        var e1 = Edge(0, 1);
        var e2 = Edge(1, 2);
        var edges = new List<RoadEdge> { e1, e2 };
        var graph = Graph([N(0, 0, 0), N(1, 100, 0), N(2, 100, -100)], edges);

        var steps = PathStepBuilder.Build(edges, graph);

        Assert.That(steps[0].Turn, Is.EqualTo(TurnDirection.Right));
    }

    // ── Edge references ──────────────────────────────────────────────────────

    [Test]
    public void Build_EachStep_ReferencesCorrectEdge()
    {
        var e1 = Edge(0, 1);
        var e2 = Edge(1, 2);
        var e3 = Edge(2, 3);
        var edges = new List<RoadEdge> { e1, e2, e3 };
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0), N(2, 200, 0), N(3, 300, 0)],
            edges);

        var steps = PathStepBuilder.Build(edges, graph);

        Assert.That(steps[0].Edge, Is.SameAs(e1));
        Assert.That(steps[1].Edge, Is.SameAs(e2));
        Assert.That(steps[2].Edge, Is.SameAs(e3));
    }

    [Test]
    public void Build_NextEdge_ChainedCorrectly()
    {
        var e1 = Edge(0, 1);
        var e2 = Edge(1, 2);
        var e3 = Edge(2, 3);
        var edges = new List<RoadEdge> { e1, e2, e3 };
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0), N(2, 200, 0), N(3, 300, 0)],
            edges);

        var steps = PathStepBuilder.Build(edges, graph);

        Assert.That(steps[0].NextEdge, Is.SameAs(e2));
        Assert.That(steps[1].NextEdge, Is.SameAs(e3));
        Assert.That(steps[2].NextEdge, Is.Null);
    }

    // ── Empty input ──────────────────────────────────────────────────────────

    [Test]
    public void Build_EmptyEdgeList_ReturnsEmptyStepList()
    {
        var graph = Graph([N(0, 0, 0)], []);

        var steps = PathStepBuilder.Build(Array.Empty<RoadEdge>(), graph);

        Assert.That(steps, Is.Empty);
    }
}
