using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using UrbanEcho.Events.UI;

namespace UrbanEcho.Graph
{
    /// <summary>
    /// Uses AADT traffic volume data already present on road graph edges
    /// (loaded from the main shapefiles addt field) to build weighted spawn/destination
    /// lists and assign default volumes to unmatched edges. this should ideally also be used to
    /// match addt in a graph when addt is in a separate file but for now it just processes whatever is already on the graph edges in main.
    /// </summary>
    public static class TrafficVolumeLoader
    {
        /// <summary>
        /// Pre-built weighted list of node IDs for destination selection.
        /// Nodes adjacent to high-AADT edges appear more often.
        /// </summary>
        private static List<int>? _weightedDestinationNodes;

        /// <summary>
        /// True when at least one edge carried a real AADT value (> 0) from the source data.
        /// False for OSM-only graphs where every edge received the default fallback volume.
        /// </summary>
        public static bool HasRealAadt { get; private set; }

        /// <summary>
        /// Clears AADT state so stale values do not persist across project switches.
        /// </summary>
        public static void Reset()
        {
            HasRealAadt = false;
            _weightedDestinationNodes = null;
        }

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
            HasRealAadt = matched > 0;
            _weightedDestinationNodes = BuildWeightedDestinationNodes(graph);

            EventQueueForUI.Instance.Add(new AadtReadyEvent(HasRealAadt));

            LogStats(graph, matched, unmatched, minAADT, maxAADT, totalAADT);
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
        /// Picks a destination node uniformly at random, excluding <paramref name="excludeNodeId"/>.
        /// </summary>
        public static int PickRandomDestination(RoadGraph graph, int excludeNodeId)
        {
            var nodes = graph.Nodes.Keys.ToList();
            if (nodes.Count == 0) return excludeNodeId;
            int pick;
            int attempts = 0;
            do
            {
                pick = nodes[Random.Shared.Next(nodes.Count)];
                attempts++;
            } while (pick == excludeNodeId && nodes.Count > 1 && attempts < 50);
            return pick;
        }

        /// <summary>
        /// Builds a weighted list of node IDs where nodes adjacent to
        /// high-AADT edges appear proportionally more often.
        /// </summary>
        private static List<int> BuildWeightedDestinationNodes(RoadGraph graph)
        {
            // Aggregate max AADT touching each node.
            var nodeTraffic = new Dictionary<int, double>();
            foreach (var edge in graph.Edges)
            {
                double vol = edge.Metadata.TrafficVolume;
                if (!nodeTraffic.ContainsKey(edge.From) || vol > nodeTraffic[edge.From])
                    nodeTraffic[edge.From] = vol;
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
            var vm = MainWindow.Instance.GetMainViewModel();
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
