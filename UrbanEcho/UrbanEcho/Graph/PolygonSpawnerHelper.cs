using Mapsui.Projections;
using NetTopologySuite.Geometries;
using OsmSharp;
using OsmSharp.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UrbanEcho.Graph
{
    /// <summary>
    /// Helpers for auto-placing spawners at polygon boundaries.
    /// Supports two strategies:
    ///   1. Edge-gate detection: find road nodes near a polygon's perimeter.
    ///   2. OSM residential detection: extract landuse=residential polygons from
    ///      an .osm file and return their boundaries for gate placement.
    /// </summary>
    public static class PolygonSpawnerHelper
    {
        /// <summary>
        /// Returns up to <paramref name="maxGates"/> road nodes whose distance to
        /// <paramref name="polygon"/>'s boundary is within <paramref name="toleranceMercator"/>
        /// EPSG:3857 units. Nodes are selected greedily so they are well spread out around
        /// the perimeter. Only nodes with at least one outgoing edge are considered.
        /// </summary>
        public static List<RoadNode> GetBoundaryNodes(
            Geometry polygon,
            RoadGraph graph,
            double toleranceMercator = 400.0,
            int maxGates = 6)
        {
            var boundary = polygon.Boundary;
            var candidates = new List<RoadNode>();

            foreach (var kvp in graph.Nodes)
            {
                if (graph.GetOutgoingEdges(kvp.Key).Count == 0)
                    continue;

                var pt = new Point(kvp.Value.X, kvp.Value.Y);
                if (boundary.Distance(pt) <= toleranceMercator)
                    candidates.Add(kvp.Value);
            }

            return GreedyFurthestFirst(candidates, maxGates);
        }

        /// <summary>
        /// Parses an OSM (.osm) file and returns polygons built from closed ways that
        /// carry residential land-use tags (landuse=residential or landuse=apartments).
        /// Returns a list of (display name, EPSG:3857 polygon).
        /// </summary>
        public static List<(string Name, Geometry Polygon)> GetOsmResidentialPolygons(string osmPath)
        {
            var results = new List<(string, Geometry)>();
            var nodePositions = new Dictionary<long, (double Lon, double Lat)>();
            var factory = new GeometryFactory();

            try
            {
                // First pass: collect node positions
                using (var fs = new FileInfo(osmPath).OpenRead())
                {
                    var source = new XmlOsmStreamSource(fs);
                    foreach (OsmGeo geo in source)
                    {
                        if (geo is OsmSharp.Node n &&
                            n.Id.HasValue && n.Longitude.HasValue && n.Latitude.HasValue)
                        {
                            nodePositions[n.Id.Value] = (n.Longitude.Value, n.Latitude.Value);
                        }
                    }
                }

                // Second pass: collect residential closed ways
                using (var fs = new FileInfo(osmPath).OpenRead())
                {
                    var source = new XmlOsmStreamSource(fs);
                    foreach (OsmGeo geo in source)
                    {
                        if (geo is OsmSharp.Way way &&
                            way.Tags != null &&
                            way.Nodes != null && way.Nodes.Length > 3 &&
                            IsResidentialLanduse(way.Tags) &&
                            way.Nodes.First() == way.Nodes.Last())   // closed ring
                        {
                            var coords = new List<Coordinate>();
                            bool skip = false;

                            foreach (long nid in way.Nodes)
                            {
                                if (!nodePositions.TryGetValue(nid, out var lonlat))
                                {
                                    skip = true;
                                    break;
                                }
                                var (mx, my) = SphericalMercator.FromLonLat(lonlat.Lon, lonlat.Lat);
                                coords.Add(new Coordinate(mx, my));
                            }

                            if (skip || coords.Count < 4)
                                continue;

                            try
                            {
                                var ring = factory.CreateLinearRing(coords.ToArray());
                                Geometry poly = factory.CreatePolygon(ring);
                                if (!poly.IsValid)
                                    poly = poly.Buffer(0);

                                string name = way.Tags.TryGetValue("name", out string? n2)
                                    ? n2
                                    : $"Residential_{way.Id}";

                                results.Add((name, poly));
                            }
                            catch
                            {
                                // Skip malformed rings
                            }
                        }
                    }
                }
            }
            catch
            {
                // Caller is responsible for user-facing error logging
            }

            return results;
        }

        private static bool IsResidentialLanduse(OsmSharp.Tags.TagsCollectionBase tags)
        {
            return tags.TryGetValue("landuse", out string? landuse) &&
                   (landuse == "residential" || landuse == "apartments");
        }

        /// <summary>
        /// Greedy furthest-first selection: picks up to <paramref name="count"/> nodes
        /// from <paramref name="pool"/> so that selected nodes are as spread out as possible.
        /// </summary>
        private static List<RoadNode> GreedyFurthestFirst(List<RoadNode> pool, int count)
        {
            if (pool.Count == 0)
                return new List<RoadNode>();
            if (pool.Count <= count)
                return new List<RoadNode>(pool);

            var selected = new List<RoadNode> { pool[0] };

            while (selected.Count < count)
            {
                RoadNode? best = null;
                double bestMinDist = -1;

                foreach (var node in pool)
                {
                    if (selected.Any(s => s.Id == node.Id))
                        continue;

                    double minDist = double.MaxValue;
                    foreach (var s in selected)
                    {
                        double dx = node.X - s.X;
                        double dy = node.Y - s.Y;
                        double d = dx * dx + dy * dy;
                        if (d < minDist) minDist = d;
                    }

                    if (minDist > bestMinDist)
                    {
                        bestMinDist = minDist;
                        best = node;
                    }
                }

                if (best == null) break;
                selected.Add(best);
            }

            return selected;
        }
    }
}
