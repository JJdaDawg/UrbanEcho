using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

/// <summary>
/// Immutable directed graph of the road network, built from <see cref="RoadNode"/> vertices
/// and directed <see cref="RoadEdge"/> arcs.
/// <para>
/// Internally uses two adjacency-list dictionaries both outgoing and incoming edge
/// for fast traversal in either direction
/// Adjacency-list representation is from (see also azure wiki) https://dn721901.ca.archive.org/0/items/thomas-h.-cormen-charles-e.-leiserson-ronald-l.-rivest-clifford-stein-introducti/Thomas%20H.%20Cormen%2C%20Charles%20E.%20Leiserson%2C%20Ronald%20L.%20Rivest%2C%20Clifford%20Stein%20-%20Introduction%20to%20Algorithms-The%20MIT%20Press%20%282022%29.pdf
/// </para>
/// </summary>
public sealed class RoadGraph
{
    /// <summary>All nodes in the graph, keyed by node ID.</summary>
    public IReadOnlyDictionary<int, RoadNode> Nodes { get; }

    /// <summary>All directed edges in the graph.</summary>
    public IReadOnlyList<RoadEdge> Edges { get; }

    /// <summary>
    /// Forward adjacency list: maps each node ID to its outgoing edges.
    /// Used by A* and other traversals that follow the direction of travel.
    /// </summary>
    private readonly Dictionary<int, List<RoadEdge>> _adjacency;

    /// <summary>
    /// Reverse adjacency list maps each node ID to the edges whose destination is that node— needed for
    /// turn-penalty and rerouting logic that must inspect the edge arriving at a node.
    /// </summary>
    private readonly Dictionary<int, List<RoadEdge>> _adjacencyForEdgeTo;

    /// <summary>
    /// Constructs the graph and pre-builds both adjacency lists
    /// </summary>
    /// <param name="nodes">Road network nodes keyed by node ID.</param>
    /// <param name="edges">Directed road edges connecting those nodes.</param>
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

    /// <summary>
    /// Returns all edges that leave <paramref name="nodeId"/> (i.e. <c>edge.From == nodeId</c>).
    /// Returns an empty list when the node has no outgoing edges.
    /// </summary>
    public IReadOnlyList<RoadEdge> GetOutgoingEdges(int nodeId)
    {
        return _adjacency.TryGetValue(nodeId, out var edges)
            ? edges
            : Array.Empty<RoadEdge>();
    }

    /// <summary>
    /// Returns all edges that arrive at <paramref name="nodeId"/> (i.e. <c>edge.To == nodeId</c>).
    /// Returns an empty list when the node has no incoming edges.
    /// </summary>
    public IReadOnlyList<RoadEdge> GetIncomingEdges(int nodeId)
    {
        return _adjacencyForEdgeTo.TryGetValue(nodeId, out var edges)
            ? edges
            : Array.Empty<RoadEdge>();
    }

}