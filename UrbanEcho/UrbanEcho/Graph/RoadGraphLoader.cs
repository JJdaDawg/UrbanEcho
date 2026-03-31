using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Precision;

using Avalonia.Media;

using System;
using System.Collections.Generic;
using System.Linq;
using Mapsui.Nts;
using UrbanEcho.Graph;

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
                int endIndex = 1;
                double length = 0;

                //while (startIndex < coords.Length - 1)
                //{
                /*
                if (!IsGraphNode(coords[startIndex], startIndex == 0, usage))
                {
                    startIndex++;
                    continue;
                }*/

                //double length = 0;
                //int endIndex = startIndex + 1;

                while (endIndex < coords.Length)
                {
                    length += coords[endIndex - 1]
                        .Distance(coords[endIndex]);
                    /*
                    if (IsGraphNode(
                        coords[endIndex],
                        endIndex == coords.Length - 1,
                        usage))
                        break;
                    */
                    if (endIndex == coords.Length - 1)
                        break;
                    endIndex++;
                }

                //if (endIndex < coords.Length)
                //{
                int from = GetNodeId(coords[startIndex]);
                int to = GetNodeId(coords[endIndex]);

                if (from != to && length > 0)
                {
                    if (!(metadata.OneWay))
                    {
                        edges.Add(new RoadEdge(from, to, length, metadata, g, true));
                        edges.Add(new RoadEdge(to, from, length, metadata, g, false));
                    }
                    else
                    {
                        if (metadata.FromToFlowDirection)
                        {
                            edges.Add(new RoadEdge(from, to, length, metadata, g, true));
                        }
                        else
                        {
                            edges.Add(new RoadEdge(to, from, length, metadata, g, false));
                        }
                    }
                }
                //}

                //startIndex = endIndex;
                //}
            }
            else
            {
                continue;
            }
        }

        var graph = new RoadGraph(nodeObjects, edges);

        return graph;//RemoveOrphanComponents(graph);
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

        bool fromTo = false;
        bool oneWay = false;

        if (f.Fields.Contains("FLOW_DIREC"))
        {
            if (f["FLOW_DIREC"]?.ToString() == "FromTo")
            {
                fromTo = true;
                oneWay = true;
            }
            if (f["FLOW_DIREC"]?.ToString() == "ToFrom")
            {
                fromTo = false;
                oneWay = true;
            }
        }

        bool truckAllowed = true;

        if (f.Fields.Contains("TRUCK_ACCE"))
        {
            if (f["TRUCK_ACCE"]?.ToString() == "NO ACCESS")
            {
                truckAllowed = false;
            }
        }

        return new RoadMetadata
        {
            RoadName = f.Fields.Contains("STREET")
                ? f["STREET"]?.ToString() ?? ""
                : "",

            SpeedLimit = speedKmh / 3.6,
            OneWay = oneWay,
            FromToFlowDirection = fromTo,
            TrafficVolume = f.Fields.Contains("AADT")
                && f["AADT"] != null
                && double.TryParse(f["AADT"].ToString(), out var aadtVal)
                ? aadtVal
                : AssignAADTValue(f),
            RoadType = f.Fields.Contains("CARTO_CLAS")
                ? RoadTypeExtensions.ParseCartoClass(f["CARTO_CLAS"]?.ToString())
                : RoadType.Unknown,
            TruckAllowance = truckAllowed
        };
    }

    private static double AssignAADTValue(Mapsui.IFeature f)
    {
        double aadtValue = 100;

        RoadType type = RoadType.Unknown;

        if (f.Fields.Contains("CARTO_CLAS"))
        {
            type = RoadTypeExtensions.ParseCartoClass(f["CARTO_CLAS"]?.ToString());

            if (type == RoadType.Freeway)
            {
                aadtValue = 600;
            }
            if (type == RoadType.Arterial)
            {
                aadtValue = 500;
            }
            if (type == RoadType.Expressway)
            {
                aadtValue = 600;
            }
            if (type == RoadType.Collector)
            {
                aadtValue = 300;
            }
            if (type == RoadType.LocalStreet)
            {
                aadtValue = 100;
            }
            if (type == RoadType.Ramp)
            {
                aadtValue = 300;
            }
            if (type == RoadType.Roundabout)
            {
                aadtValue = 300;
            }
            if (type == RoadType.AlleywayLane)
            {
                aadtValue = 100;
            }
            if (type == RoadType.Private)
            {
                aadtValue = 100;
            }
        }

        return aadtValue;
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