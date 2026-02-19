using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Precision;

using Avalonia.Media;

using System;
using System.Collections.Generic;
using System.Linq;
using Mapsui.Nts;

namespace UrbanTrafficSim.Core.IO;

public static class RoadGraphLoader
{
    public static RoadGraph? LoadFromFeatures(IEnumerable<Mapsui.IFeature> features)
    {
        var featureList = features.ToList();
        if (!featureList.Any())
            return new RoadGraph(new(), new());
        GeometryFeature? firstGeometryFeature;
        if (featureList.First() is GeometryFeature firstFeature)
        {
            firstGeometryFeature = firstFeature;
        }
        else
        {
            return null;
        }

        var factory = firstGeometryFeature.Geometry.Factory;

        var precisionModel = factory.PrecisionModel;

        NormalizeFeatures(featureList, precisionModel);

        var usage = CountCoordinateUsage(featureList);

        var nodes = new Dictionary<string, int>();
        var nodeObjects = new Dictionary<int, RoadNode>();
        var edges = new List<RoadEdge>();
        int nextNodeId = 0;

        int GetNodeId(Coordinate c)
        {
            string key = $"{c.X}_{c.Y}";
            if (!nodes.TryGetValue(key, out var id))
            {
                id = nextNodeId++;
                nodes[key] = id;
                nodeObjects[id] = new RoadNode(id, c.X, c.Y);
            }
            return id;
        }

        foreach (var feature in featureList)
        {
            if (feature is GeometryFeature g)
            {
                if (g.Geometry is not LineString line)
                    continue;

                var metadata = ExtractMetadata(feature);

                var coords = line.Coordinates;
                int startIndex = 0;

                while (startIndex < coords.Length - 1)
                {
                    if (!IsGraphNode(coords[startIndex], startIndex == 0, usage))
                    {
                        startIndex++;
                        continue;
                    }

                    double length = 0;
                    int endIndex = startIndex + 1;

                    while (endIndex < coords.Length)
                    {
                        length += coords[endIndex - 1]
                            .Distance(coords[endIndex]);

                        if (IsGraphNode(
                            coords[endIndex],
                            endIndex == coords.Length - 1,
                            usage))
                            break;

                        endIndex++;
                    }

                    if (endIndex < coords.Length)
                    {
                        int from = GetNodeId(coords[startIndex]);
                        int to = GetNodeId(coords[endIndex]);

                        if (from != to && length > 0)
                        {
                            edges.Add(new RoadEdge(from, to, length, metadata, g));

                            if (!metadata.OneWay)
                                edges.Add(new RoadEdge(to, from, length, metadata, g));
                        }
                    }

                    startIndex = endIndex;
                }
            }
            else
            {
                continue;
            }
        }

        var graph = new RoadGraph(nodeObjects, edges);

        return RemoveOrphanComponents(graph);
    }

    private static void NormalizeFeatures(
        IEnumerable<Mapsui.IFeature> features,
        PrecisionModel precision)
    {
        foreach (var f in features)
        {
            if (f is GeometryFeature g)
            {
                if (g.Geometry is not LineString line)
                    continue;

                foreach (var c in line.Coordinates)
                    precision.MakePrecise(c);
            }
            else
            {
                continue;
            }
        }
    }

    private static Dictionary<string, int> CountCoordinateUsage(
        IEnumerable<Mapsui.IFeature> features)
    {
        var usage = new Dictionary<string, int>();

        foreach (var f in features)
        {
            if (f is GeometryFeature g)
            {
                if (g.Geometry is not LineString line)
                    continue;

                foreach (var c in line.Coordinates)
                {
                    string key = $"{c.X}_{c.Y}";
                    usage[key] = usage.TryGetValue(key, out var n) ? n + 1 : 1;
                }
            }
            else
            {
                continue;
            }
        }

        return usage;
    }

    private static bool IsGraphNode(
        Coordinate c,
        bool isEndpoint,
        Dictionary<string, int> usage)
    {
        string key = $"{c.X}_{c.Y}";
        return isEndpoint || usage[key] > 1;
    }

    private static RoadMetadata ExtractMetadata(Mapsui.IFeature f)
    {
        double speedKmh = 50; // fallback

        if (f.Fields.Contains("SPEED_LIMI"))
        {
            var raw = f["SPEED_LIMI"];
            if (raw != null && double.TryParse(raw.ToString(), out var parsed))
                speedKmh = parsed;
        }

        return new RoadMetadata
        {
            RoadType = f.Fields.Contains("CATEGORY")
                ? f["CATEGORY"]?.ToString() ?? ""
                : "",

            SpeedLimit = speedKmh / 3.6,
            OneWay = f.Fields.Contains("FLOW_DIREC") &&
                     f["FLOW_DIREC"]?.ToString() == "OneWay"
        };
    }

    private static RoadGraph RemoveOrphanComponents(RoadGraph graph)
    {
        var adjacency = graph.Edges
            .GroupBy(e => e.From)
            .ToDictionary(g => g.Key,
                          g => g.Select(e => e.To).ToList());

        var visited = new HashSet<int>();
        var components = new List<List<int>>();

        foreach (var node in graph.Nodes.Keys)
        {
            if (visited.Contains(node))
                continue;

            var stack = new Stack<int>();
            var component = new List<int>();

            stack.Push(node);

            while (stack.Count > 0)
            {
                var n = stack.Pop();
                if (!visited.Add(n))
                    continue;

                component.Add(n);

                if (adjacency.TryGetValue(n, out var neighbors))
                    foreach (var m in neighbors)
                        stack.Push(m);
            }

            components.Add(component);
        }

        var largest = components
            .OrderByDescending(c => c.Count)
            .First();

        var keep = new HashSet<int>(largest);

        var newNodes = graph.Nodes
            .Where(kvp => keep.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var newEdges = graph.Edges
            .Where(e => keep.Contains(e.From)
                     && keep.Contains(e.To))
            .ToList();

        return new RoadGraph(newNodes, newEdges);
    }
}