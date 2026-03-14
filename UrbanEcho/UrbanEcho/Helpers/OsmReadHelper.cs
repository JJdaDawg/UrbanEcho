using Mapsui;
using Mapsui.Nts;
using Mapsui.Projections;
using NetTopologySuite.Geometries;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Models;
using Node = OsmSharp.Node;

namespace UrbanEcho.Helpers
{
    public static class OsmReadHelper
    {
        public static List<IFeature> GetRoadFeatures(string path)
        {
            //part of this code from here
            //https://github.com/OsmSharp/core/blob/develop/samples/Sample.Filter/Program.cs

            List<IFeature> featuresList = new List<IFeature>();
            Dictionary<long, Node> nodes = new Dictionary<long, Node>();
            using (var fileStream = new FileInfo(path).OpenRead())
            {
                XmlOsmStreamSource source = new XmlOsmStreamSource(fileStream);

                var progress = source.ShowProgress();
                foreach (OsmGeo geo in source)
                {
                    if (geo is Node n)
                    {
                        if (n.Id != null)
                        {
                            nodes[n.Id.Value] = n;
                        }
                    }
                }

                foreach (OsmGeo geo in source)
                {
                    if (geo is OsmSharp.Way way)
                    {
                        if (way.Tags != null)
                        {
                            if (way.Tags.TryGetValue("highway", out string highwayType))
                            {
                                if (OsmReadHelper.CheckRoadTypeAllowed(highwayType))
                                {
                                    if (way.Nodes.Count() > 1)
                                    {
                                        Coordinate[] coordinates = new Coordinate[way.Nodes.Count()];
                                        int coordCount = 0;
                                        bool skip = false;

                                        foreach (long index in way.Nodes)
                                        {
                                            if (nodes.TryGetValue(index, out Node? node))
                                            {
                                                if (node.Longitude is double lon && node.Latitude is double lat)
                                                {
                                                    (double x, double y) point = SphericalMercator.FromLonLat(
                                                            lon,
                                                            lat
                                                        );
                                                    coordinates[coordCount++] = new Coordinate(point.x, point.y);
                                                }
                                                else
                                                {
                                                    skip = true;
                                                    break;
                                                }
                                            }
                                        }

                                        if (!skip)
                                        {
                                            LineString lineString = new LineString(coordinates);
                                            GeometryFeature gf = OsmReadHelper.CreateRoadFeature(new GeometryFeature(lineString), way);

                                            featuresList.Add(gf);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return featuresList;
        }

        public static List<IFeature> GetIntersectionFeatures(string path)
        {
            //part of this code from here
            //https://github.com/OsmSharp/core/blob/develop/samples/Sample.Filter/Program.cs

            List<IFeature> featuresList = new List<IFeature>();
            Dictionary<long, Node> nodes = new Dictionary<long, Node>();
            using (var fileStream = new FileInfo(path).OpenRead())
            {
                XmlOsmStreamSource source = new XmlOsmStreamSource(fileStream);

                var progress = source.ShowProgress();
                foreach (OsmGeo geo in source)
                {
                    if (geo is Node n)
                    {
                        if (n.Id != null)
                        {
                            nodes[n.Id.Value] = n;
                        }
                    }
                }

                foreach (OsmGeo geo in source)
                {
                    if (geo is OsmSharp.Node node)
                    {
                        if (node.Tags != null)
                        {
                            if (node.Tags.TryGetValue("highway", out string highwayType))
                            {
                                if (OsmReadHelper.CheckIntersectionTypeAllowed(highwayType))
                                {
                                    if (node.Longitude is double lon && node.Latitude is double lat)
                                    {
                                        (double x, double y) point = SphericalMercator.FromLonLat(
                                                lon,
                                                lat
                                            );
                                        Point newPoint = new Point(point.x, point.y);
                                        GeometryFeature gf = OsmReadHelper.CreateIntersectionFeature(new GeometryFeature(newPoint), node);

                                        featuresList.Add(gf);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return featuresList;
        }

        private static List<string> AllowableRoadTypes = PopulateAllowableRoadTypes();
        private static List<string> AllowableIntersectionTypes = PopulateAllowableIntersectionTypes();

        public static bool CheckRoadTypeAllowed(string stringToCheck)
        {
            bool returnValue = false;
            if (AllowableRoadTypes.Count == 0)
            {
                PopulateAllowableRoadTypes();
            }

            if (AllowableRoadTypes.Contains(stringToCheck))
            {
                returnValue = true;
            }

            return returnValue;
        }

        public static bool CheckIntersectionTypeAllowed(string stringToCheck)
        {
            bool returnValue = false;
            if (AllowableIntersectionTypes.Count == 0)
            {
                PopulateAllowableIntersectionTypes();
            }

            if (AllowableIntersectionTypes.Contains(stringToCheck))
            {
                returnValue = true;
            }

            return returnValue;
        }

        private static List<string> PopulateAllowableRoadTypes()
        {
            List<string> list = new List<string>();
            list.Add("residential");
            list.Add("service");
            list.Add("unclassified");
            list.Add("tertiary");
            list.Add("secondary");
            list.Add("primary");
            list.Add("trunk");
            list.Add("motorway");
            list.Add("motorway_link");
            list.Add("trunk_link");

            return list;
        }

        private static List<string> PopulateAllowableIntersectionTypes()
        {
            List<string> list = new List<string>();
            list.Add("stop");
            list.Add("traffic_signals");

            return list;
        }

        public static GeometryFeature CreateRoadFeature(GeometryFeature gf, Way way)
        {
            gf["OBJECTID"] = GetOsmId(way);
            gf["STREET"] = GetName(way);
            gf["SPEED_LIMI"] = GetSpeedLimit(way);
            gf["LANES"] = GetLane(way);
            gf["FLOW_DIREC"] = GetDirection(way);

            return gf;
        }

        public static GeometryFeature CreateIntersectionFeature(GeometryFeature gf, Node node)
        {
            gf["Intersecti"] = GetName(node);
            gf["Intersec_1"] = GetSignalType(node);

            return gf;
        }

        public static string GetSignalType(Node n)
        {
            string returnValue = "All Way Stop";
            if (n.Tags.TryGetValue("highway", out string value))
            {
                if (value == "traffic_signals")
                {
                    returnValue = "Full Signal";
                }
            }

            return returnValue;
        }

        public static string GetName(Node n)
        {
            string returnValue = "Unnamed";
            if (n.Tags.TryGetValue("name", out string value))
            {
                returnValue = value;
            }

            return returnValue;
        }

        public static string GetOsmId(Way way)
        {
            string returnValue = "0";

            if (way.Tags.TryGetValue("osm_id", out string value))
            {
                returnValue = value;
            }

            return returnValue;
        }

        public static string GetName(Way way)
        {
            string returnValue = "Unnamed";

            if (way.Tags.TryGetValue("name", out string value))
            {
                returnValue = value;
            }

            return returnValue;
        }

        public static string GetDirection(Way way)
        {
            string returnValue = "TwoWay";

            if (way.Tags.TryGetValue("oneway", out string value))
            {
                if (value == "yes")
                {
                    returnValue = "FromTo";
                }
            }

            return returnValue;
        }

        public static int GetSpeedLimit(Way way)
        {
            int returnValue = 50;

            if (way.Tags.TryGetValue("maxspeed", out string value))
            {
                if (int.TryParse(value, out int intValue))
                {
                    returnValue = intValue;
                }
            }

            return returnValue;
        }

        public static int GetLane(Way way)
        {
            int returnValue = 2;

            if (way.Tags.TryGetValue("lanes", out string value))
            {
                if (int.TryParse(value, out int intValue))
                {
                    returnValue = intValue;
                }
            }

            return returnValue;
        }
    }
}