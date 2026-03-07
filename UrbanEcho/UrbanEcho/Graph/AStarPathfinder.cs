using System;
using System.Collections.Generic;

namespace UrbanEcho.Graph
{
    public sealed class AStarPathfinder
    {
        private readonly RoadGraph _graph;
        private readonly IReadOnlyDictionary<int, double>? _nodePenalties;

        /// <summary>Upper-bound speed (m/s) keeps the heuristic admissible when cost = travel time.</summary>
        private const double MaxSpeedMs = 130.0 / 3.6;

        /// <summary>Flat penalty (seconds) added for every turn beyond the straight threshold.</summary>
        private const double TurnPenaltySeconds = 8.0;

        /// <summary>Angle (radians) below which a direction change is considered straight.</summary>
        private const double StraightThreshold = Math.PI / 9.0; // 20°

        public AStarPathfinder(RoadGraph graph, IReadOnlyDictionary<int, double>? nodePenalties = null)
        {
            _graph = graph;
            _nodePenalties = nodePenalties;
        }

        /// <summary>
        /// Compute the weighted routing cost for an edge, including the road-type multiplier.
        /// </summary>
        private static double EdgeRoutingCost(RoadEdge edge)
        {
            return edge.TravelTimeSeconds * edge.Metadata.RoadType.RoutingCostMultiplier();
        }

        /// <summary>
        /// Compute the turn penalty between two consecutive edges.
        /// Returns 0 when going straight, <see cref="TurnPenaltySeconds"/> for any significant turn.
        /// </summary>
        private double ComputeTurnPenalty(RoadEdge incoming, RoadEdge outgoing)
        {
            if (!_graph.Nodes.TryGetValue(incoming.From, out var a) ||
                !_graph.Nodes.TryGetValue(incoming.To, out var b) ||
                !_graph.Nodes.TryGetValue(outgoing.To, out var c))
                return 0;

            double abX = b.X - a.X, abY = b.Y - a.Y;
            double bcX = c.X - b.X, bcY = c.Y - b.Y;

            double cross = abX * bcY - abY * bcX;
            double dot   = abX * bcX + abY * bcY;

            double angle = Math.Atan2(Math.Abs(cross), dot);

            if (angle < StraightThreshold)
                return 0;

            return TurnPenaltySeconds;
        }

        public IReadOnlyList<int> FindPath(int node_start, int node_goal)
        {
            var openSet = new PriorityQueue<int, double>();
            var closedSet = new HashSet<int>();

            var g = new Dictionary<int, double>();
            var h = new Dictionary<int, double>();
            var f = new Dictionary<int, double>();
            var parent = new Dictionary<int, int>();
            var parentEdge = new Dictionary<int, RoadEdge>();

            foreach (var node in _graph.Nodes.Keys)
                g[node] = double.PositiveInfinity;

            g[node_start] = 0;
            h[node_start] = Heuristic(node_start, node_goal);
            f[node_start] = h[node_start];

            openSet.Enqueue(node_start, f[node_start]);

            while (openSet.Count > 0)
            {
                int node_current = openSet.Dequeue();

                if (closedSet.Contains(node_current))
                    continue;

                if (node_current == node_goal)
                    return ReconstructPath(parent, node_current);

                closedSet.Add(node_current);

                foreach (var edge in _graph.GetOutgoingEdges(node_current))
                {
                    int node_successor = edge.To;

                    double turnPenalty = parentEdge.TryGetValue(node_current, out var prevEdge)
                        ? ComputeTurnPenalty(prevEdge, edge)
                        : 0;

                    double successor_current_cost =
                        g[node_current] + EdgeRoutingCost(edge)
                        + turnPenalty
                        + (_nodePenalties?.GetValueOrDefault(node_successor, 0.0) ?? 0.0);

                    if (successor_current_cost >= g.GetValueOrDefault(node_successor, double.PositiveInfinity)
                        && closedSet.Contains(node_successor))
                        continue;

                    if (!g.ContainsKey(node_successor) ||
                        successor_current_cost < g[node_successor])
                    {
                        parent[node_successor] = node_current;
                        parentEdge[node_successor] = edge;
                        g[node_successor] = successor_current_cost;

                        h[node_successor] = Heuristic(node_successor, node_goal);
                        f[node_successor] = g[node_successor] + h[node_successor];

                        openSet.Enqueue(node_successor, f[node_successor]);
                        closedSet.Remove(node_successor);
                    }
                }
            }

            return Array.Empty<int>();
        }

        private double Heuristic(int a, int b)
        {
            var pa = _graph.Nodes[a];
            var pb = _graph.Nodes[b];

            double dx = pa.X - pb.X;
            double dy = pa.Y - pb.Y;

            // Cost unit is seconds — divide by max speed so it never overestimates.
            return Math.Sqrt(dx * dx + dy * dy) / MaxSpeedMs;
        }

        public IReadOnlyList<RoadEdge> FindPathEdges(int node_start, int node_goal)
        {
            var openSet = new PriorityQueue<int, double>();
            var closedSet = new HashSet<int>();

            var g = new Dictionary<int, double>();
            var h = new Dictionary<int, double>();
            var f = new Dictionary<int, double>();
            var parentEdge = new Dictionary<int, RoadEdge>();

            foreach (var node in _graph.Nodes.Keys)
                g[node] = double.PositiveInfinity;

            g[node_start] = 0;
            h[node_start] = Heuristic(node_start, node_goal);
            f[node_start] = h[node_start];

            openSet.Enqueue(node_start, f[node_start]);

            while (openSet.Count > 0)
            {
                int node_current = openSet.Dequeue();

                if (closedSet.Contains(node_current))
                    continue;

                if (node_current == node_goal)
                    return ReconstructEdgePath(parentEdge, node_current);

                closedSet.Add(node_current);

                foreach (var edge in _graph.GetOutgoingEdges(node_current))
                {
                    int node_successor = edge.To;

                    double turnPenalty = parentEdge.TryGetValue(node_current, out var prevEdge)
                        ? ComputeTurnPenalty(prevEdge, edge)
                        : 0;

                    double successor_current_cost =
                        g[node_current] + EdgeRoutingCost(edge)
                        + turnPenalty
                        + (_nodePenalties?.GetValueOrDefault(node_successor, 0.0) ?? 0.0);

                    if (successor_current_cost >= g.GetValueOrDefault(node_successor, double.PositiveInfinity)
                        && closedSet.Contains(node_successor))
                        continue;

                    if (!g.ContainsKey(node_successor) ||
                        successor_current_cost < g[node_successor])
                    {
                        parentEdge[node_successor] = edge;
                        g[node_successor] = successor_current_cost;

                        h[node_successor] = Heuristic(node_successor, node_goal);
                        f[node_successor] = g[node_successor] + h[node_successor];

                        openSet.Enqueue(node_successor, f[node_successor]);
                        closedSet.Remove(node_successor);
                    }
                }
            }

            return Array.Empty<RoadEdge>();
        }

        private static List<RoadEdge> ReconstructEdgePath(
            Dictionary<int, RoadEdge> parentEdge,
            int current)
        {
            var edges = new List<RoadEdge>();

            while (parentEdge.TryGetValue(current, out var edge))
            {
                edges.Add(edge);
                current = edge.From;
            }

            edges.Reverse();
            return edges;
        }

        private static List<int> ReconstructPath(
            Dictionary<int, int> parent,
            int current)
        {
            var path = new List<int> { current };

            while (parent.TryGetValue(current, out var prev))
            {
                current = prev;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }
    }
}