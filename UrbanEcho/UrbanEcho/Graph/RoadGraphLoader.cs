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

/// <summary>
/// Builds a <see cref="RoadGraph"/> from Mapsui/NTS line-string features
/// loaded from a municipal road-network shapefile.
/// </summary>
/// <remarks>
/// Shapefile attribute columns and their meanings:
/// <list type="table">
///   <item><term>STREET</term>     <description>Street name</description></item>
///   <item><term>SPEED_LIMI</term> <description>Posted speed limit (km/h)</description></item>
///   <item><term>FLOW_DIREC</term> <description>One-way flow direction: "FromTo", "ToFrom", or blank (two-way)</description></item>
///   <item><term>TRUCK_ACCE</term> <description>Truck access flag; "NO ACCESS" prohibits trucks</description></item>
///   <item><term>AADT</term>       <description>Annual Average Daily Traffic volume</description></item>
///   <item><term>CARTO_CLAS</term> <description>Cartographic road class (Freeway, Arterial, Collector, etc.)</description></item>
/// </list>
///
/// data from kitchener geohub in future more versatile loader would be nice to support multiple formats and schemas, but this is hardcoded to the specific columns in the shapefile we have on hand.
/// </remarks>
public static class RoadGraphLoader
{
    /// <summary>
    /// Converts an enumerable of Mapsui line-string features into a directed
    /// <see cref="RoadGraph"/> with nodes snapped to a 0.01-unit precision grid.
    /// Two-way edges are added in both directions; one-way edges respect
    /// <c>FLOW_DIREC</c>. Returns an empty graph when the feature list is empty,
    /// or <see langword="null"/> when the first feature is not a
    /// <see cref="GeometryFeature"/>.
    /// </summary>
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

        //Use a precision model of 0.01 accuracy anything within that value will be marked as same point.
        PrecisionModel precisionModel = new PrecisionModel(100);// factory.PrecisionModel;

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

                while (endIndex < coords.Length)
                {
                    length += coords[endIndex - 1]
                        .Distance(coords[endIndex]);

                    if (endIndex == coords.Length - 1)
                        break;
                    endIndex++;
                }

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
            }
            else
            {
                continue;
            }
        }

        var graph = new RoadGraph(nodeObjects, edges);

        return graph;
    }

    /// <summary>
    /// Snaps every coordinate in each line-string feature in-place to the
    /// supplied <paramref name="precision"/> model, so nearby endpoints
    /// collapse to the same key and form shared graph nodes.
    /// </summary>
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

    /// <summary>
    /// Counts how many line-string features share each coordinate (by string key).
    /// Coordinates referenced by more than one feature are road intersections
    /// and must become graph nodes even when they are interior polyline points.
    /// </summary>
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

    /// <summary>
    /// Returns <see langword="true"/> when coordinate <paramref name="c"/> must
    /// become a graph node — either because it is a line endpoint or because
    /// it is shared by more than one feature (i.e. an intersection).
    /// </summary>
    private static bool IsGraphNode(
        Coordinate c,
        bool isEndpoint,
        Dictionary<string, int> usage)
    {
        string key = $"{c.X}_{c.Y}";
        return isEndpoint || usage[key] > 1;
    }

    /// <summary>
    /// Reads road attribute columns from a shapefile feature and returns a
    /// populated <see cref="RoadMetadata"/> record.
    /// Falls back to 50 km/h when <c>SPEED_LIMI</c> is absent or unparseable,
    /// and delegates to <see cref="AssignAADTValue"/> when <c>AADT</c> is missing.
    /// </summary>
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

    /// <summary>
    /// Returns a heuristic Annual Average Daily Traffic (AADT) estimate when
    /// the shapefile does not supply a measured value.
    /// Estimates are based on typical regional traffic volumes by cartographic
    /// road class (CARTO_CLAS): freeways/expressways ~600, arterials ~500,
    /// collectors/ramps/roundabouts ~300, local streets/private/alleys ~100.
    /// </summary>
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
}