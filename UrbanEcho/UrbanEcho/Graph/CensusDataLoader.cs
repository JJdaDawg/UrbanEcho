using Avalonia.Controls.Shapes;
using DotSpatial.Projections;
using DotSpatial.Projections.Transforms;
using Mapsui;
using Mapsui.Nts;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Providers;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using System;
using System.Collections.Generic;
using System.Linq;
using UrbanEcho.Events.UI;
using UrbanEcho.Helpers;
using Polygon = NetTopologySuite.Geometries.Polygon;

namespace UrbanEcho.Graph
{
    /// <summary>
    /// Loads the Census 2021 Work Commuting shapefile, reprojects polygon
    /// coordinates from NAD83 UTM Zone 17N (EPSG:26917) to EPSG:3857
    /// (Web Mercator) using DotSpatial.Projections, then maps road
    /// graph nodes into census zones.
    /// </summary>
    public static class CensusDataLoader
    {
        private static ProjectionInfo? _src;//moved because it can give exception
        private static ProjectionInfo? _dst;

        /// <summary>
        /// Load census features from the shapefile, reproject to EPSG:3857,
        /// and build CensusZone objects with gate node assignments.
        /// </summary>
        public static List<CensusZone> Load(string shapefilePath, RoadGraph graph)
        {
            var zones = new List<CensusZone>();

            try
            {
                var source = new ShapeFile(shapefilePath);

                string EPSG26917 = @"+proj = utm + zone = 17 + ellps = GRS80 + towgs84 = 0, 0, 0, 0, 0, 0, 0 + units = m + no_defs + type = crs";
                string EPSG3857 = @"+proj = merc + a = 6378137 + b = 6378137 + lat_ts = 0 + lon_0 = 0 + x_0 = 0 + y_0 = 0 + k = 1 + units = m + nadgrids = @null + wktext + no_defs + type = crs";
                _src = ProjectionInfo.FromProj4String(EPSG26917);
                _dst = ProjectionInfo.FromProj4String(EPSG3857);

                List<IFeature> features = Helper.GetFeatures(source);

                EventQueueForUI.Instance.Add(new LogToConsole(
                    MainWindow.Instance.GetMainViewModel(),
                    $"[Census] Loaded {features.Count} features from shapefile"));

                double totalArea = 1;
                foreach (var feature in features)
                {
                    if (feature is not GeometryFeature gf)
                        continue;

                    string totalAreaFeature = feature["GEO_LEVEL"]?.ToString() ?? "";
                    if (totalAreaFeature == "Census metropolitan area")
                    {
                        if (gf.Geometry is not Polygon polygon)
                            continue;

                        totalArea = polygon.Area;
                    }
                }

                foreach (var feature in features)
                {
                    if (feature is not GeometryFeature gf)
                        continue;

                    // Skip the CMA summary row, only use dissemination areas
                    string geoLevel = feature["GEO_LEVEL"]?.ToString() ?? "";
                    if (!geoLevel.Contains("Dissemination", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (gf.Geometry is not Polygon polygon)
                        continue;
                    double theArea = polygon.Area;
                    double ratioOfArea = theArea / totalArea;
                    if (ratioOfArea > 1.0f)
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(
               MainWindow.Instance.GetMainViewModel(),
               $"[Census] Error with total area"));
                        ratioOfArea = 1.0f;
                    }

                    var zone = new CensusZone
                    {
                        GeoCode = feature["GEO_CODE"]?.ToString() ?? "",
                        GeoName = feature["GEO_NAME"]?.ToString() ?? "",
                        Boundary = ReprojectPolygon(polygon),
                        Population = TryParseInt(feature["POP_2021"]),
                        TotalEmployed = TryParseInt(feature["TOT_PLACE_"]),
                        CarTruckVanDrivers = TryParseInt(feature["COM_CAR__1"]),
                        CommuteWithinCSD = TryParseInt(feature["COM_WI_CSD"]),
                        CommuteDiffCSDSameCD = TryParseInt(feature["COM_DIFF_C"]),
                        CommuteLessThan15Min = TryParseInt(feature["COMM_LESS_"]),
                        Commute15To29Min = TryParseInt(feature["COMM_15_29"]),
                        Commute30To44Min = TryParseInt(feature["COMM_30_44"]),
                        RatioOfArea = ratioOfArea,
                    };

                    zones.Add(zone);
                }

                // Spatial join: assign road graph nodes to census zones
                AssignGateNodes(zones, graph);

                int before = zones.Count;
                zones.RemoveAll(z => z.GateNodeIds.Count == 0);

                EventQueueForUI.Instance.Add(new LogToConsole(
                    MainWindow.Instance.GetMainViewModel(),
                    $"[Census] {zones.Count}/{before} zones have road network nodes"));

                int totalDrivers = zones.Sum(z => z.CarTruckVanDrivers);
                EventQueueForUI.Instance.Add(new LogToConsole(
                    MainWindow.Instance.GetMainViewModel(),
                    $"[Census] Total car commuters across all zones: {totalDrivers}"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(
                    MainWindow.Instance.GetMainViewModel(),
                    $"[Census] Failed to load census data: {ex}"));
            }

            return zones;
        }

        /// <summary>
        /// For each road graph node, find which census zone polygon contains it.
        /// Uses prepared geometry for faster point-in-polygon tests.
        /// </summary>
        private static void AssignGateNodes(List<CensusZone> zones, RoadGraph graph)
        {
            var prepared = zones
                .Select(z => (zone: z, prep: PreparedGeometryFactory.Prepare(z.Boundary)))
                .ToList();

            int assigned = 0;

            foreach (var kvp in graph.Nodes)
            {
                var point = new Point(kvp.Value.X, kvp.Value.Y);

                foreach (var (zone, prep) in prepared)
                {
                    if (prep.Contains(point))
                    {
                        zone.GateNodeIds.Add(kvp.Key);
                        assigned++;
                        break; // A node belongs to exactly one DA
                    }
                }
            }

            EventQueueForUI.Instance.Add(new LogToConsole(
                MainWindow.Instance.GetMainViewModel(),
                $"[Census] Assigned {assigned}/{graph.Nodes.Count} road nodes to census zones"));
        }

        /// <summary>
        /// Reproject a polygon's coordinates from EPSG:26917 to EPSG:3857.
        /// </summary>
        private static Polygon ReprojectPolygon(Polygon polygon)
        {
            var factory = polygon.Factory;

            var shell = factory.CreateLinearRing(
                polygon.ExteriorRing.Coordinates.Select(ReprojectCoordinate).ToArray());

            var holes = new LinearRing[polygon.NumInteriorRings];
            for (int i = 0; i < polygon.NumInteriorRings; i++)
                holes[i] = factory.CreateLinearRing(
                    polygon.GetInteriorRingN(i).Coordinates.Select(ReprojectCoordinate).ToArray());

            return factory.CreatePolygon(shell, holes);
        }

        /// <summary>
        /// Reproject a single coordinate from EPSG:26917 to EPSG:3857 using DotSpatial.
        /// </summary>
        private static Coordinate ReprojectCoordinate(Coordinate c)
        {
            double[] xy = { c.X, c.Y };
            double[] z = { 0 };
            Reproject.ReprojectPoints(xy, z, _src, _dst, 0, 1);
            return new Coordinate(xy[0], xy[1]);
        }

        private static int TryParseInt(object? value)
        {
            if (value == null) return 0;
            return int.TryParse(value.ToString(), out int result) ? result : 0;
        }
    }
}