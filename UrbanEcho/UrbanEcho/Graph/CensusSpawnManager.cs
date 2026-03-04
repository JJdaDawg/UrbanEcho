using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UrbanEcho.Graph
{
    /// <summary>
    /// Builds a gravity-model Origin-Destination table from census dissemination
    /// areas and provides weighted spawn/destination node selection for vehicles.
    /// </summary>
    public sealed class CensusSpawnManager
    {
        private readonly List<CensusZone> _zones;
        private readonly Random _rng = new();

        // Flat weighted list of node IDs — each node appears proportional to its zone's CarTruckVanDrivers
        private readonly List<int> _weightedSpawnNodes = new();

        // Per-origin zone: list of (destinationZoneIndex, gravityWeight)
        private readonly List<(int zoneIndex, double weight)>[] _odRows;

        public IReadOnlyList<CensusZone> Zones => _zones;
        public bool IsLoaded => _zones.Count > 0 && _weightedSpawnNodes.Count > 0;

        public CensusSpawnManager(List<CensusZone> zones, RoadGraph graph)
        {
            _zones = zones;
            _odRows = new List<(int, double)>[zones.Count];
            BuildWeightedSpawnNodes();
            BuildODTable();
        }

        /// <summary>
        /// Build a flat list of node IDs weighted by CarTruckVanDrivers per zone.
        /// A zone with 200 drivers contributes proportionally more entries than one with 10.
        /// </summary>
        private void BuildWeightedSpawnNodes()
        {
            double maxValue = 0;
            foreach (var zone in _zones)
            {
                double zoneAreaToUse = zone.RatioOfArea;
                if (zoneAreaToUse < 0.0001f)
                {
                    zoneAreaToUse = 0.0001f;
                }
                double theValue = (zone.CarTruckVanDrivers) * 1.0f / zoneAreaToUse;

                if (theValue > maxValue)
                {
                    maxValue = theValue;
                }
            }

            foreach (var zone in _zones)
            {
                double zoneAreaToUse = zone.RatioOfArea;
                if (zoneAreaToUse < 0.0001f)
                {
                    zoneAreaToUse = 0.0001f;
                }
                double intensity = (((zone.CarTruckVanDrivers) * 1.0f / zoneAreaToUse) / maxValue) * 10.0f;
                // Scale down so the list stays manageable — 1 entry per 10 drivers, min 1
                int entries = Math.Max(1, (int)(intensity));

                foreach (int nodeId in zone.GateNodeIds)
                {
                    for (int i = 0; i < entries; i++)
                        _weightedSpawnNodes.Add(nodeId);
                }
            }
        }

        /// <summary>
        /// Build the gravity model O-D weight table.
        /// Trip weight(i→j) = TotalEmployed(j) / distance(centroid_i, centroid_j).
        /// </summary>
        private void BuildODTable()
        {
            Point[] centroids = _zones.Select(z => z.Boundary.Centroid).ToArray();

            for (int i = 0; i < _zones.Count; i++)
            {
                var row = new List<(int, double)>();
                Point origin = centroids[i];

                for (int j = 0; j < _zones.Count; j++)
                {
                    if (_zones[j].GateNodeIds.Count == 0)
                        continue;

                    double dx = centroids[j].X - origin.X;
                    double dy = centroids[j].Y - origin.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);

                    // Avoid divide-by-zero for same-zone intra trips
                    if (dist < 1.0) dist = 1.0;

                    double attraction = Math.Max(1, _zones[j].TotalEmployed);
                    double weight = attraction / dist;

                    row.Add((j, weight));
                }

                _odRows[i] = row;
            }
        }

        /// <summary>
        /// Pick a single spawn node ID weighted by zone car commuter counts.
        /// </summary>
        public int PickWeightedSpawnNode()
        {
            if (_weightedSpawnNodes.Count == 0)
                return 0;

            return _weightedSpawnNodes[_rng.Next(_weightedSpawnNodes.Count)];
        }

        /// <summary>
        /// Pick a spawn + destination node pair using the gravity model O-D table.
        /// </summary>
        public (int spawnNode, int destNode) PickSpawnAndDestination()
        {
            int originIdx = PickWeightedZoneIndex();
            int spawnNode = PickNodeFromZone(_zones[originIdx]);

            int destIdx = PickDestinationZone(originIdx);
            int destNode = PickNodeFromZone(_zones[destIdx]);

            return (spawnNode, destNode);
        }

        /// <summary>Pick a zone index weighted by VehicleTripsGenerated (CarTruckVanDrivers).</summary>
        private int PickWeightedZoneIndex()
        {
            double total = _zones.Sum(z => (double)z.VehicleTripsGenerated);
            double pick = _rng.NextDouble() * total;
            double cumulative = 0.0;

            for (int i = 0; i < _zones.Count; i++)
            {
                cumulative += _zones[i].VehicleTripsGenerated;
                if (pick <= cumulative)
                    return i;
            }

            return _zones.Count - 1;
        }

        /// <summary>Pick a destination zone from the gravity-weighted O-D row for the given origin.</summary>
        private int PickDestinationZone(int originZoneIdx)
        {
            var row = _odRows[originZoneIdx];

            if (row == null || row.Count == 0)
                return _rng.Next(_zones.Count);

            double total = row.Sum(r => r.weight);
            double pick = _rng.NextDouble() * total;
            double cumulative = 0.0;

            foreach (var (zoneIndex, weight) in row)
            {
                cumulative += weight;
                if (pick <= cumulative)
                    return zoneIndex;
            }

            return row[^1].zoneIndex;
        }

        /// <summary>Pick a random gate node from the given zone.</summary>
        private int PickNodeFromZone(CensusZone zone)
        {
            if (zone.GateNodeIds.Count == 0)
                return 0;

            return zone.GateNodeIds[_rng.Next(zone.GateNodeIds.Count)];
        }
    }
}