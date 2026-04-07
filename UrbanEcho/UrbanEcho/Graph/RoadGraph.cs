using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

public sealed class RoadGraph
{
    public IReadOnlyDictionary<int, RoadNode> Nodes { get; }
    public IReadOnlyList<RoadEdge> Edges { get; }

    private readonly Dictionary<int, List<RoadEdge>> _adjacency;

    private readonly Dictionary<int, List<RoadEdge>> _adjacencyForEdgeTo;

    public RoadGraph(
        Dictionary<int, RoadNode> nodes,
        List<RoadEdge> edges)
    {
        Nodes = nodes;
        Edges = edges;

        _adjacency = new Dictionary<int, List<RoadEdge>>();
        _adjacencyForEdgeTo = new Dictionary<int, List<RoadEdge>>();

        foreach (var edge in edges)
        {
            if (!_adjacency.TryGetValue(edge.From, out var list))
            {
                list = new List<RoadEdge>();
                _adjacency[edge.From] = list;
            }

            list.Add(edge);

            if (!_adjacencyForEdgeTo.TryGetValue(edge.To, out var listEdgeTo))
            {
                listEdgeTo = new List<RoadEdge>();
                _adjacencyForEdgeTo[edge.To] = listEdgeTo;
            }

            listEdgeTo.Add(edge);
        }
    }

    public IReadOnlyList<RoadEdge> GetOutgoingEdges(int nodeId)
    {
        return _adjacency.TryGetValue(nodeId, out var edges)
            ? edges
            : Array.Empty<RoadEdge>();
    }

    public IReadOnlyList<RoadEdge> GetIncomingEdges(int nodeId)
    {
        return _adjacencyForEdgeTo.TryGetValue(nodeId, out var edges)
            ? edges
            : Array.Empty<RoadEdge>();
    }

    public RoadNode GetNearestNode(Coordinate position)
    {
        RoadNode? closest = null;
        double best = double.MaxValue;

        foreach (var node in Nodes.Values)
        {
            var d = node.Position.Distance(position);
            if (d < best)
            {
                best = d;
                closest = node;
            }
        }

        return closest!;
    }

    public IReadOnlyList<RoadNode> GetNeighbors(int nodeId)
    {
        var neighbors = new List<RoadNode>();

        foreach (var edge in GetOutgoingEdges(nodeId))
        {
            if (Nodes.TryGetValue(edge.To, out var neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }
}