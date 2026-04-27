using Mapsui;
using Mapsui.Layers;
using UrbanEcho.Graph;

namespace UrbanEcho.Tests;

/// <summary>
/// Unit tests for RoadGraph: adjacency list construction, outgoing/incoming
/// edge lookups, and edge-case handling for isolated or missing nodes.
/// </summary>
public class RoadGraphTests
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

    // ── Nodes / Edges properties ─────────────────────────────────────────────

    [Test]
    public void Constructor_StoresAllNodes()
    {
        var graph = Graph([N(1, 0, 0), N(2, 100, 0), N(3, 200, 0)], []);

        Assert.That(graph.Nodes, Has.Count.EqualTo(3));
        Assert.That(graph.Nodes.ContainsKey(1), Is.True);
        Assert.That(graph.Nodes.ContainsKey(2), Is.True);
        Assert.That(graph.Nodes.ContainsKey(3), Is.True);
    }

    [Test]
    public void Constructor_StoresAllEdges()
    {
        var edges = new[] { Edge(1, 2), Edge(2, 3) };
        var graph = Graph([N(1, 0, 0), N(2, 100, 0), N(3, 200, 0)], edges);

        Assert.That(graph.Edges, Has.Count.EqualTo(2));
    }

    // ── GetOutgoingEdges ─────────────────────────────────────────────────────

    [Test]
    public void GetOutgoingEdges_ReturnsCorrectEdges()
    {
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0), N(2, 200, 0)],
            [Edge(0, 1), Edge(0, 2), Edge(1, 2)]);

        var outFrom0 = graph.GetOutgoingEdges(0);

        Assert.That(outFrom0, Has.Count.EqualTo(2));
        Assert.That(outFrom0.All(e => e.From == 0), Is.True);
    }

    [Test]
    public void GetOutgoingEdges_NodeWithNoOutgoing_ReturnsEmpty()
    {
        // Node 2 has no outgoing edges
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0), N(2, 200, 0)],
            [Edge(0, 1), Edge(1, 2)]);

        var outFrom2 = graph.GetOutgoingEdges(2);

        Assert.That(outFrom2, Is.Empty);
    }

    [Test]
    public void GetOutgoingEdges_NonExistentNode_ReturnsEmpty()
    {
        var graph = Graph([N(0, 0, 0)], []);

        var result = graph.GetOutgoingEdges(999);

        Assert.That(result, Is.Empty);
    }

    // ── GetIncomingEdges ─────────────────────────────────────────────────────

    [Test]
    public void GetIncomingEdges_ReturnsCorrectEdges()
    {
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0), N(2, 200, 0)],
            [Edge(0, 2), Edge(1, 2)]);

        var inTo2 = graph.GetIncomingEdges(2);

        Assert.That(inTo2, Has.Count.EqualTo(2));
        Assert.That(inTo2.All(e => e.To == 2), Is.True);
    }

    [Test]
    public void GetIncomingEdges_NodeWithNoIncoming_ReturnsEmpty()
    {
        // Node 0 has no incoming edges
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0)],
            [Edge(0, 1)]);

        var inTo0 = graph.GetIncomingEdges(0);

        Assert.That(inTo0, Is.Empty);
    }

    [Test]
    public void GetIncomingEdges_NonExistentNode_ReturnsEmpty()
    {
        var graph = Graph([N(0, 0, 0)], []);

        var result = graph.GetIncomingEdges(999);

        Assert.That(result, Is.Empty);
    }

    // ── Directed graph semantics ─────────────────────────────────────────────

    [Test]
    public void DirectedEdge_OnlyAppearsInOutgoingOfFromNode()
    {
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0)],
            [Edge(0, 1)]);

        Assert.That(graph.GetOutgoingEdges(0), Has.Count.EqualTo(1));
        Assert.That(graph.GetOutgoingEdges(1), Is.Empty, "Directed edge should not appear as outgoing from the To node");
    }

    [Test]
    public void DirectedEdge_OnlyAppearsInIncomingOfToNode()
    {
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0)],
            [Edge(0, 1)]);

        Assert.That(graph.GetIncomingEdges(1), Has.Count.EqualTo(1));
        Assert.That(graph.GetIncomingEdges(0), Is.Empty, "Directed edge should not appear as incoming to the From node");
    }

    // ── Empty graph ──────────────────────────────────────────────────────────

    [Test]
    public void EmptyGraph_NoNodesNoEdges()
    {
        var graph = Graph([], []);

        Assert.That(graph.Nodes, Is.Empty);
        Assert.That(graph.Edges, Is.Empty);
    }

    // ── Multiple edges between same pair ─────────────────────────────────────

    [Test]
    public void MultipleEdgesBetweenSameNodes_AllReturnedAsOutgoing()
    {
        // Two parallel edges from 0→1 (e.g. different lanes or road types)
        var graph = Graph(
            [N(0, 0, 0), N(1, 100, 0)],
            [Edge(0, 1, 100, 10), Edge(0, 1, 100, 15)]);

        Assert.That(graph.GetOutgoingEdges(0), Has.Count.EqualTo(2));
    }
}
