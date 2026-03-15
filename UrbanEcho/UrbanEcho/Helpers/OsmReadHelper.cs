using BruTile;
using FluentAvalonia.UI.Media;
using Mapsui;
using Mapsui.Nts;
using Mapsui.Projections;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay.Snap;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
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

            try
            {
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
                                            List<Coordinate> coordinates = new List<Coordinate>();

                                            bool skip = false;

                                            for (int i = 0; i < way.Nodes.Length; i++)
                                            {
                                                if (nodes.TryGetValue(way.Nodes[i], out Node? node))
                                                {
                                                    if (node.Longitude is double lon && node.Latitude is double lat)
                                                    {
                                                        (double x, double y) point = SphericalMercator.FromLonLat(
                                                                lon,
                                                                lat
                                                            );
                                                        coordinates.Add(new Coordinate(point.x, point.y));
                                                    }
                                                    else
                                                    {
                                                        skip = true;
                                                        break; //Just quit if any nodes could not be located
                                                    }
                                                }
                                            }

                                            if (!skip && coordinates.Count > 1)
                                            {
                                                LineString lineString = new LineString(coordinates.ToArray());
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
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to get features from {path} - {ex.Message}"));
            }
            bool anySplittingRequired = true;

            while (anySplittingRequired && featuresList.Count < 1000)
            {
                (featuresList, anySplittingRequired) = SplitCrossingRoads(featuresList);
            }

            if (featuresList.Count > 1000)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Loaded the max number of roads for viewport (1000)"));
            }

            return featuresList;
        }

        public static (List<IFeature> newFeatures, bool splitHappened) SplitCrossingRoads(List<IFeature> featuresList)
        {
            List<IFeature> newFeaturesList = new List<IFeature>();
            int count = 0;
            bool hadToSplit = false;
            GeometryFeature? test = new GeometryFeature();
            foreach (IFeature feature in featuresList)
            {
                bool isNewFeature = false;

                if (hadToSplit == false)
                {
                    foreach (IFeature otherFeature in featuresList)
                    {
                        if (feature.Equals(otherFeature))
                        {
                            continue;
                        }
                        else
                        {
                            test = new GeometryFeature();
                            Coordinate[]? c1 = null;
                            Coordinate[]? c2 = null;

                            if (NeedsSplitting(feature, otherFeature, out c1, out c2))
                            {
                                hadToSplit = true;
                                isNewFeature = true;
                                LineString newLs1 = new LineString(c1);
                                LineString newLs2 = new LineString(c2);
                                GeometryFeature newGf1 = CopyFeatureAttributes(new GeometryFeature(newLs1), feature);
                                GeometryFeature newGf2 = CopyFeatureAttributes(new GeometryFeature(newLs2), feature);
                                newFeaturesList.Add(newGf1);
                                newFeaturesList.Add(newGf2);
                                break;
                                //EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Split value {count}"));
                            }
                        }
                    }
                }
                if (!isNewFeature)
                {
                    newFeaturesList.Add(feature);
                }
                else
                {
                    bool showme = true;
                    /*
                    if (test is GeometryCollection gc)
                    {
                        LineString lineString = new LineString(coordinates.ToArray());
                        GeometryFeature gf = OsmReadHelper.CreateRoadFeature(new GeometryFeature(lineString), way, objectIdToUse);
                    }*/
                }
            }

            count++;
            return (newFeaturesList, hadToSplit);
        }

        public static bool NeedsSplitting(IFeature feature1, IFeature feature2, out Coordinate[]? c1, out Coordinate[]? c2)
        {
            c1 = null;
            c2 = null;
            bool intersected = false;
            bool returnValue = false;
            Coordinate theIntersectPoint = new Coordinate(0, 0);
            try
            {
                if (feature1 is GeometryFeature gf1 && feature2 is GeometryFeature gf2)
                {
                    if (gf1.Geometry != null && gf2.Geometry != null)
                    {
                        if (gf1.Geometry is LineString ls1 && gf2.Geometry is LineString ls2)
                        {
                            bool foundIntersect = false;
                            for (int i1 = 0; i1 < ls1.Count - 1; i1++)
                            {
                                for (int i2 = 0; i2 < ls2.Count - 1; i2++)
                                {
                                    if (!ls1.Coordinates[0].Equals2D(ls2.Coordinates[i2], 5.0f) && !ls1.Coordinates[0].Equals2D(ls2.Coordinates[i2 + 1], 5.0f)
                                        && !ls1.Coordinates[ls1.Count - 1].Equals2D(ls2.Coordinates[i2], 5.0f) && !ls1.Coordinates[ls1.Count - 1].Equals2D(ls2.Coordinates[i2 + 1], 5.0f))
                                    {
                                        Coordinate p1 = new Coordinate(ls1.Coordinates[i1]);
                                        Coordinate p2 = new Coordinate(ls1.Coordinates[i1 + 1]);
                                        Coordinate q1 = new Coordinate(ls2.Coordinates[i2]);
                                        Coordinate q2 = new Coordinate(ls2.Coordinates[i2 + 1]);

                                        Coordinate p1Lengthened = new Coordinate(ls1.Coordinates[i1]);
                                        Coordinate p2Lengthened = new Coordinate(ls1.Coordinates[i1 + 1]);
                                        Coordinate q1Lengthened = new Coordinate(ls2.Coordinates[i2]);
                                        Coordinate q2Lengthened = new Coordinate(ls2.Coordinates[i2 + 1]);

                                        if (i1 == 0)
                                        {
                                            p1Lengthened = ExtendLine(p2.X, p2.Y, p1.X, p1.Y, 1.0f);
                                        }
                                        if (i1 + 1 == ls1.Count - 1)
                                        {
                                            p2Lengthened = ExtendLine(p1.X, p1.Y, p2.X, p2.Y, 1.0f);
                                        }

                                        if (i2 == 0)
                                        {
                                            q1Lengthened = ExtendLine(q2.X, q2.Y, q1.X, q1.Y, 1.0f);
                                        }
                                        if (i2 + 1 == ls2.Count - 1)
                                        {
                                            q2Lengthened = ExtendLine(q1.X, q1.Y, q2.X, q2.Y, 1.0f);
                                        }
                                        /*
                                        p1 = new Coordinate(0, 0);
                                        p2 = new Coordinate(10, 10);

                                        q1 = new Coordinate(0, 10);
                                        q2 = new Coordinate(10, 0);
                                        */
                                        intersected = false;
                                        RobustLineIntersector i = new NetTopologySuite.Algorithm.RobustLineIntersector();
                                        i.ComputeIntersection(p1Lengthened, p2Lengthened, q1Lengthened, q2Lengthened);

                                        if (i.HasIntersection)
                                        {
                                            intersected = true;
                                            theIntersectPoint = new Coordinate(i.GetIntersection(0));
                                        }
                                        /*
                                        {
                                            LineString showtestLs1 = new LineString(
                                                                                    new Coordinate[]
                                                                                    {
                                                                                        new Coordinate(p1Lengthened),
                                                                                        new Coordinate(p2Lengthened)
                                                                                    });
                                            LineString showtestLs2 = new LineString(new Coordinate[]
                                                                                                {
                                                                                                    new Coordinate(q1Lengthened),
                                                                                                    new Coordinate(q2Lengthened)
                                                                                                });

                                            LineString showoldLs1 = new LineString(
                                                                                    new Coordinate[]
                                                                                    {
                                                                                        new Coordinate(p1),
                                                                                        new Coordinate(p2)
                                                                                    });
                                            LineString showoldLs2 = new LineString(new Coordinate[]
                                                                                                {
                                                                                                    new Coordinate(q1),
                                                                                                    new Coordinate(q2)
                                                                                                });

                                            showtestLs1.Intersection(showtestLs2);

                                            if (showtestLs1.Intersects(showtestLs2))
                                            {
                                                Geometry p = showtestLs1.Intersection(showtestLs2);
                                            }
                                        }*/
                                    }
                                    if (intersected)
                                    {
                                        Coordinate newPoint = theIntersectPoint;

                                        c1 = new Coordinate[i1 + 2];
                                        for (int c1Index = 0; c1Index < c1.Count() - 1; c1Index++)
                                        {
                                            c1[c1Index] = new Coordinate(ls1.Coordinates[c1Index].X, ls1.Coordinates[c1Index].Y);
                                        }
                                        c1[c1.Count() - 1] = newPoint;

                                        c2 = new Coordinate[ls1.Count - c1.Count() + 2];
                                        c2[0] = newPoint;
                                        for (int c2Index = 1; c2Index < c2.Count(); c2Index++)
                                        {
                                            c2[c2Index] = new Coordinate(ls1.Coordinates[i1 + c2Index].X, ls1.Coordinates[i1 + c2Index].Y);
                                        }

                                        LineString testLs1 = new LineString(c1);
                                        LineString testLs2 = new LineString(c2);

                                        if (testLs1.Length > 5 && testLs2.Length > 5)
                                        {
                                            foundIntersect = true;
                                            returnValue = true;
                                        }
                                    }
                                    if (foundIntersect)
                                    {
                                        break;
                                    }
                                }
                                if (foundIntersect)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Exception splitting line string {ex.Message}"));
            }

            return returnValue;
        }

        public static Coordinate ExtendLine(double x1, double y1, double x2, double y2, double extendAmount)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);

            if (len == 0) return new Coordinate(x2, y2);
            double scaleX = (dx / len);
            double scaleY = (dy / len);
            double newX = x2 + scaleX * extendAmount;
            double newY = y2 + scaleY * extendAmount;

            return new Coordinate(newX, newY);
        }

        public static List<IFeature> GetIntersectionFeatures(string path)
        {
            //part of this code from here
            //https://github.com/OsmSharp/core/blob/develop/samples/Sample.Filter/Program.cs

            List<IFeature> featuresList = new List<IFeature>();
            Dictionary<long, Node> nodes = new Dictionary<long, Node>();

            try
            {
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
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to get features from {path} - {ex.Message}"));
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

        public static GeometryFeature CopyFeatureAttributes(GeometryFeature gf, IFeature oldFeature)
        {
            gf["STREET"] = oldFeature["STREET"];
            gf["SPEED_LIMI"] = oldFeature["SPEED_LIMI"];
            gf["LANES"] = oldFeature["LANES"];
            gf["FLOW_DIREC"] = oldFeature["FLOW_DIREC"];

            return gf;
        }

        public static GeometryFeature CreateRoadFeature(GeometryFeature gf, Way way)
        {
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

        //not used because we have to split the line segments to be further segmented (using a incrementing integer instead)
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