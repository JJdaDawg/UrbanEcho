using Mapsui;
using Mapsui.Layers;
using UrbanEcho.Graph;

namespace UrbanEcho.Tests;

/// <summary>
/// Unit tests for TrafficVolumeLoader: AADT fallback assignment, weighted and
/// random destination picking, and HasRealAadt flag behavior.
/// Note: AssignToGraph is not directly testable here because it calls
/// MainWindow.Instance for logging. These tests cover the public static
/// pick methods and reset behavior.
/// </summary>
public class TrafficVolumeLoaderTests
{
    [SetUp]
    public void Setup()
    {
        Helpers.Helper.TestMode = true;
        TrafficVolumeLoader.Reset();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RoadNode N(int id, double x, double y) => new(id, x, y);

    private static RoadEdge MakeEdge(int from, int to, double length, double speedLimitMs, double trafficVolume = 0)
    {
        var metadata = new RoadMetadata
        {
            SpeedLimit = speedLimitMs,
            TrafficVolume = trafficVolume
        };
        var feature = new PointFeature(new MPoint(0, 0));
        return new RoadEdge(from, to, length, metadata, feature, isFromStartOfLineString: true);
    }

    private static RoadGraph Graph(IEnumerable<RoadNode> nodes, IEnumerable<RoadEdge> edges)
    {
        var nodeDict = nodes.ToDictionary(n => n.Id);
        return new RoadGraph(nodeDict, edges.ToList());
    }

    // ── Reset ────────────────────────────────────────────────────────────────

    [Test]
    public void Reset_ClearsHasRealAadt()
    {
        // After reset, HasRealAadt should be false
        TrafficVolumeLoader.Reset();
        Assert.That(TrafficVolumeLoader.HasRealAadt, Is.False);
    }

    // ── PickRandomDestination ────────────────────────────────────────────────

    [Test]
    public void PickRandomDestination_ReturnsNodeFromGraph()
    {
        var graph = Graph(
            [N(1, 0, 0), N(2, 100, 0), N(3, 200, 0)],
            [MakeEdge(1, 2, 100, 10), MakeEdge(2, 3, 100, 10)]);

        var validNodes = new HashSet<int> { 1, 2, 3 };
        for (int i = 0; i < 100; i++)
        {
            int pick = TrafficVolumeLoader.PickRandomDestination(graph, excludeNodeId: -1);
            Assert.That(validNodes, Does.Contain(pick));
        }
    }

    [Test]
    public void PickRandomDestination_AvoidsExcludedNode()
    {
        var graph = Graph(
            [N(1, 0, 0), N(2, 100, 0)],
            [MakeEdge(1, 2, 100, 10)]);

        // With two nodes and excluding node 1, should always return node 2
        for (int i = 0; i < 50; i++)
        {
            int pick = TrafficVolumeLoader.PickRandomDestination(graph, excludeNodeId: 1);
            Assert.That(pick, Is.EqualTo(2));
        }
    }

    [Test]
    public void PickRandomDestination_SingleNode_ReturnsThatNode()
    {
        var graph = Graph([N(5, 0, 0)], []);

        // Only one node — must return it even if it matches the exclude
        int pick = TrafficVolumeLoader.PickRandomDestination(graph, excludeNodeId: 5);
        Assert.That(pick, Is.EqualTo(5));
    }

    [Test]
    public void PickRandomDestination_EmptyGraph_ReturnsExcludeNode()
    {
        var graph = Graph([], []);
        int pick = TrafficVolumeLoader.PickRandomDestination(graph, excludeNodeId: 99);
        Assert.That(pick, Is.EqualTo(99));
    }

    // ── PickWeightedDestination (fallback when no AADT assigned) ─────────────

    [Test]
    public void PickWeightedDestination_NoAadtLoaded_FallsBackToUniformRandom()
    {
        // Without calling AssignToGraph, _weightedDestinationNodes is null → fallback
        var graph = Graph(
            [N(1, 0, 0), N(2, 100, 0), N(3, 200, 0)],
            [MakeEdge(1, 2, 100, 10), MakeEdge(2, 3, 100, 10)]);

        var validNodes = new HashSet<int> { 1, 2, 3 };
        for (int i = 0; i < 100; i++)
        {
            int pick = TrafficVolumeLoader.PickWeightedDestination(graph, excludeNodeId: -1);
            Assert.That(validNodes, Does.Contain(pick));
        }
    }

    [Test]
    public void PickWeightedDestination_FallbackAvoidsExcludedNode()
    {
        var graph = Graph(
            [N(1, 0, 0), N(2, 100, 0)],
            [MakeEdge(1, 2, 100, 10)]);

        for (int i = 0; i < 50; i++)
        {
            int pick = TrafficVolumeLoader.PickWeightedDestination(graph, excludeNodeId: 1);
            Assert.That(pick, Is.EqualTo(2));
        }
    }

    // ── Edge metadata default volume ─────────────────────────────────────────

    [Test]
    public void EdgeWithZeroTrafficVolume_DefaultsToZeroBeforeAssignment()
    {
        var edge = MakeEdge(1, 2, 100, 10, trafficVolume: 0);
        Assert.That(edge.Metadata.TrafficVolume, Is.EqualTo(0));
    }

    [Test]
    public void EdgeWithRealTrafficVolume_RetainsValue()
    {
        var edge = MakeEdge(1, 2, 100, 10, trafficVolume: 5000);
        Assert.That(edge.Metadata.TrafficVolume, Is.EqualTo(5000));
    }
}
