using System.Collections.Generic;

public sealed class RoadGraph
{
    public IReadOnlyDictionary<int, RoadNode> Nodes { get; }
    public IReadOnlyList<RoadEdge> Edges { get; }

    public RoadGraph(
        Dictionary<int, RoadNode> nodes,
        List<RoadEdge> edges)
    {
        Nodes = nodes;
        Edges = edges;
    }
}