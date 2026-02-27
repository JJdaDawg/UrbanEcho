using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using UrbanEcho.Events.UI;

namespace UrbanEcho.Graph
{
    /// <summary>
    /// Uses AADT traffic volume data already present on road graph edges
    /// (loaded from the main shapefile) to build weighted spawn/destination
    /// lists and assign default volumes to unmatched edges.
    /// </summary>
    public static class TrafficVolumeLoader
    {
        /// <summary>
        /// Pre-built weighted list of node IDs for destination selection.
        /// Nodes adjacent to high-AADT edges appear more often.
        /// </summary>
        private static List<int>? _weightedDestinationNodes;

        /// <summary>
        /// Processes the graph edges that already carry AADT from the main shapefile.
        /// Edges with no AADT (0) get a small default volume so they remain routable,
        /// then builds the weighted destination node list.
        /// </summary>
        public static void AssignToGraph(RoadGraph graph)
        {
            int matched = 0;
            int unmatched = 0;
            double minAADT = double.MaxValue;
            double maxAADT = 0;
            double totalAADT = 0;

            foreach (var edge in graph.Edges)
            {
                double aadt = edge.Metadata.TrafficVolume;

                // some roads may have 0 or missing AADT, so we assign a small default volume to keep them in play for now
                if (aadt > 0)
                {
                    matched++;
                    totalAADT += aadt;
                    if (aadt < minAADT) minAADT = aadt;
                    if (aadt > maxAADT) maxAADT = aadt;
                }
                else
                {
                    // Default small volume for edges without AADT so they're still routable
                    edge.Metadata.TrafficVolume = 100;
                    unmatched++;
                }
            }

            // Build the weighted destination node list now that volumes are assigned
            _weightedDestinationNodes = BuildWeightedDestinationNodes(graph);

            LogStats(graph, matched, unmatched, minAADT, maxAADT, totalAADT);
        }

        /// <summary>
        /// Returns a list of edge indices from the graph, weighted by AADT for spawn selection.
        /// Edges with higher traffic volume appear more frequently.
        /// </summary>
        public static List<int> BuildWeightedEdgeSpawnList(RoadGraph graph)
        {
            var weightedIndices = new List<int>();

            // Find max AADT to normalize weights
            double maxAADT = 1;
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                double vol = graph.Edges[i].Metadata.TrafficVolume;
                if (vol > maxAADT)
                    maxAADT = vol;
            }

            for (int i = 0; i < graph.Edges.Count; i++)
            {
                // Normalize to 1-10 weight buckets so high-traffic edges get more vehicles
                double normalized = graph.Edges[i].Metadata.TrafficVolume / maxAADT;
                int weight = Math.Max(1, (int)(normalized * 10));
                for (int w = 0; w < weight; w++)
                {
                    weightedIndices.Add(i);
                }
            }

            return weightedIndices;
        }

        /// <summary>
        /// Picks a destination node weighted by traffic volume.
        /// Nodes near high-AADT edges are chosen more frequently,
        /// simulating that vehicles are more likely to drive toward busy areas.
        /// </summary>
        public static int PickWeightedDestination(RoadGraph graph, int excludeNodeId)
        {
            if (_weightedDestinationNodes == null || _weightedDestinationNodes.Count == 0)
            {
                // Fallback: uniform random
                var nodes = graph.Nodes.Keys.ToList();
                int pick;
                do
                {
                    pick = nodes[Random.Shared.Next(nodes.Count)];
                } while (pick == excludeNodeId && nodes.Count > 1);
                return pick;
            }

            // Pick from weighted list, retry if same as excluded node
            int attempts = 0;
            int result;
            do
            {
                result = _weightedDestinationNodes[Random.Shared.Next(_weightedDestinationNodes.Count)];
                attempts++;
            } while (result == excludeNodeId && attempts < 50);

            return result;
        }

        /// <summary>
        /// Builds a weighted list of node IDs where nodes adjacent to
        /// high-AADT edges appear proportionally more often.
        /// </summary>
        private static List<int> BuildWeightedDestinationNodes(RoadGraph graph)
        {
            // Aggregate max AADT touching each node
            var nodeTraffic = new Dictionary<int, double>();
            foreach (var edge in graph.Edges)
            {
                double vol = edge.Metadata.TrafficVolume;
                if (!nodeTraffic.ContainsKey(edge.From) || vol > nodeTraffic[edge.From])
                    nodeTraffic[edge.From] = vol;
                if (!nodeTraffic.ContainsKey(edge.To) || vol > nodeTraffic[edge.To])
                    nodeTraffic[edge.To] = vol;
            }

            double maxVol = nodeTraffic.Values.DefaultIfEmpty(1).Max();
            if (maxVol <= 0) maxVol = 1;

            var weighted = new List<int>();
            foreach (var kvp in nodeTraffic)
            {
                // 1-10 weight buckets
                double normalized = kvp.Value / maxVol;
                int weight = Math.Max(1, (int)(normalized * 10));
                for (int w = 0; w < weight; w++)
                {
                    weighted.Add(kvp.Key);
                }
            }

            return weighted;
        }

        private static void LogStats(RoadGraph graph, int matched, int unmatched,
            double minAADT, double maxAADT, double totalAADT)
        {
            var vm = UrbanEcho.Sim.Sim.GetMainViewModel();
            if (vm == null) return;

            EventQueueForUI.Instance.Add(new LogToConsole(vm,
                $"[TrafficVolume] Edges matched: {matched}/{matched + unmatched} " +
                $"({(matched * 100.0 / Math.Max(1, matched + unmatched)):F1}%)"));

            if (matched > 0)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(vm,
                    $"[TrafficVolume] AADT range: {minAADT:F0} - {maxAADT:F0}, " +
                    $"avg: {totalAADT / matched:F0}"));
            }

            if (_weightedDestinationNodes != null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(vm,
                    $"[TrafficVolume] Destination pool: {_weightedDestinationNodes.Count} " +
                    $"weighted entries from {graph.Nodes.Count} nodes"));
            }
        }
    }
}
