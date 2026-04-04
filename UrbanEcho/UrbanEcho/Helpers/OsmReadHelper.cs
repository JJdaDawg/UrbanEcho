using Avalonia.Animation;
using BruTile;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
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
        private static List<string> AllowableRoadTypes = PopulateAllowableRoadTypes();
        private static List<string> AllowableIntersectionTypes = PopulateAllowableIntersectionTypes();

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

            TestIfFeatureListHasDuplicateNodes(featuresList, false);
            List<IFeature> checkedList = new List<IFeature>();
            while (anySplittingRequired && featuresList.Count < 1000)//This limit of 1000 can be removed once we are sure is working
            {
                (featuresList, anySplittingRequired) = SplitCrossingRoads(featuresList, checkedList);
            }

            if (featuresList.Count > 1000)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Loaded the max number of roads for viewport (1000)"));
            }
            TestIfFeatureListHasDuplicateNodes(featuresList, true);

            SetToAndFromNames(featuresList);
            SetObjectIds(featuresList);
            return featuresList;
        }

        public static void SetObjectIds(List<IFeature> featuresList)
        {
            int idCounter = 1;
            foreach (IFeature feature in featuresList)
            {
                feature["OBJECTID"] = idCounter.ToString();
                idCounter++;
            }
        }

        public static void SetToAndFromNames(List<IFeature> featuresList)
        {
            foreach (IFeature feature in featuresList)
            {
                if (feature is GeometryFeature gf)
                {
                    if (gf.Geometry != null)
                    {
                        if (gf.Geometry is LineString ls)
                        {
                            foreach (IFeature otherFeature in featuresList)
                            {
                                if (feature != otherFeature)
                                {
                                    if (otherFeature is GeometryFeature otherGf)
                                    {
                                        if (otherGf.Geometry != null)
                                        {
                                            if (otherGf.Geometry is LineString otherLs)
                                            {
                                                if (!feature.Fields.Contains("TO_STREET") || Helpers.Helper.TryGetFeatureKVPToString(feature, "STREET", "") == Helpers.Helper.TryGetFeatureKVPToString(otherFeature, "STREET", ""))
                                                {
                                                    if (Helpers.Helper.TryGetFeatureKVPToString(otherFeature, "STREET", "") != "Unnamed")
                                                    {
                                                        if (ls.Coordinates.Last<Coordinate>().Equals2D(otherLs.Coordinates.Last<Coordinate>(), 0.1f))
                                                        {
                                                            feature["TO_STREET"] = Helpers.Helper.TryGetFeatureKVPToString(otherFeature, "STREET", "");
                                                        }
                                                        else
                                                        {
                                                            if (ls.Coordinates.Last<Coordinate>().Equals2D(otherLs.Coordinates.First<Coordinate>(), 0.1f))
                                                            {
                                                                feature["TO_STREET"] = Helpers.Helper.TryGetFeatureKVPToString(otherFeature, "STREET", "");
                                                            }
                                                        }
                                                    }
                                                }

                                                if (!feature.Fields.Contains("FROM_STREE") || Helpers.Helper.TryGetFeatureKVPToString(feature, "STREET", "") == Helpers.Helper.TryGetFeatureKVPToString(otherFeature, "STREET", ""))
                                                {
                                                    if (Helpers.Helper.TryGetFeatureKVPToString(otherFeature, "STREET", "") != "Unnamed")
                                                    {
                                                        if (ls.Coordinates.First<Coordinate>().Equals2D(otherLs.Coordinates.Last<Coordinate>(), 0.1f))
                                                        {
                                                            feature["FROM_STREE"] = Helpers.Helper.TryGetFeatureKVPToString(otherFeature, "STREET", "");
                                                        }
                                                        else
                                                        {
                                                            if (ls.Coordinates.First<Coordinate>().Equals2D(otherLs.Coordinates.First<Coordinate>(), 0.1f))
                                                            {
                                                                feature["FROM_STREE"] = Helpers.Helper.TryGetFeatureKVPToString(otherFeature, "STREET", "");
                                                            }
                                                        }
                                                    }
                                                }

                                                if (feature.Fields.Contains("TO_STREET") && feature.Fields.Contains("FROM_STREE"))
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (!feature.Fields.Contains("TO_STREET"))
                {
                    feature["TO_STREET"] = "";
                }

                if (!feature.Fields.Contains("FROM_STREE"))
                {
                    feature["FROM_STREE"] = "";
                }
            }
        }

        public static void TestIfFeatureListHasDuplicateNodes(List<IFeature> featuresList, bool afterSplitting)
        {
            foreach (IFeature feature in featuresList)
            {
                if (feature is GeometryFeature gf)
                {
                    if (gf.Geometry != null)
                    {
                        if (gf.Geometry is LineString ls)
                        {
                            for (int i = 0; i < ls.Coordinates.Count(); i++)
                            {
                                for (int j = 0; j < ls.Coordinates.Count(); j++)
                                {
                                    if (i != j)
                                    {
                                        if (ls.Coordinates[i].Equals2D(ls.Coordinates[j], 0.1f))
                                        {
                                            string when = afterSplitting ? "after splitting" : "before splitting";
                                            EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Duplicate Node on linestring in featurelist {when}"));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static bool TestIfLineStringHasIntersectPoint(LineString ls, Coordinate c, out int indexForDuplicateNode)
        {
            indexForDuplicateNode = 0;
            for (int i = 0; i < ls.Coordinates.Count(); i++)
            {
                if (ls.Coordinates[i].Equals2D(c, 0.1f))
                {
                    indexForDuplicateNode = i;
                    return true;
                }
            }
            return false;
        }

        public static bool TestIfLineStringHasDuplicateNodes(LineString ls, bool first)
        {
            for (int i = 0; i < ls.Coordinates.Count(); i++)
            {
                for (int j = 0; j < ls.Coordinates.Count(); j++)
                {
                    if (i != j)
                    {
                        if (ls.Coordinates[i].Equals2D(ls.Coordinates[j], 0.1f))
                        {
                            string when = first ? "first part of split" : "second part of split";
                            EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Duplicate Node on linestring {when}"));
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static (List<IFeature> newFeatures, bool splitHappened) SplitCrossingRoads(List<IFeature> featuresList, List<IFeature> checkedList)
        {
            List<IFeature> newFeaturesList = new List<IFeature>();
            int count = 0;
            bool hadToSplit = false;

            foreach (IFeature feature in featuresList)
            {
                bool isNewFeature = false;
                bool wasCheckedThisTime = false;
                if (!checkedList.Contains(feature))
                {
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
                                Coordinate[]? c1 = null;
                                Coordinate[]? c2 = null;
                                if (IsSplittingAllowed(feature, otherFeature))
                                {
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
                                    }
                                }
                            }
                        }
                        wasCheckedThisTime = true;
                    }
                    if (!isNewFeature)
                    {
                        newFeaturesList.Add(feature);
                        if (wasCheckedThisTime)
                        {
                            checkedList.Add(feature);
                        }
                    }
                }
                else
                {
                    newFeaturesList.Add(feature);
                }
            }

            count++;
            return (newFeaturesList, hadToSplit);
        }

        public static bool IsSplittingAllowed(IFeature feature1, IFeature feature2)
        {
            bool allow = true;//allow by default
            if (feature1.Fields.Contains("highway") && feature2.Fields.Contains("highway"))
            {
                string? hw1 = feature1["highway"]?.ToString();
                string? hw2 = feature2["highway"]?.ToString();

                if (hw1 != null && hw2 != null)
                {
                    if (hw1 == "motorway" && (hw2 != "motorway" || hw2 != "motorway_link"))
                    {
                        allow = false;
                    }
                }
            }

            return allow;
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

                                        intersected = false;
                                        RobustLineIntersector i = new NetTopologySuite.Algorithm.RobustLineIntersector();
                                        i.ComputeIntersection(p1Lengthened, p2Lengthened, q1Lengthened, q2Lengthened);

                                        if (i.HasIntersection)
                                        {
                                            intersected = true;
                                            theIntersectPoint = new Coordinate(i.GetIntersection(0));
                                        }
                                    }
                                    if (intersected)
                                    {
                                        Coordinate newPoint = theIntersectPoint;
                                        bool pointDuplicated = TestIfLineStringHasIntersectPoint(ls1, theIntersectPoint.CoordinateValue, out int indexForDuplicate);
                                        if (!pointDuplicated)
                                        {
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
                                        }
                                        else
                                        {
                                            c1 = new Coordinate[indexForDuplicate + 1];

                                            for (int c1Index = 0; c1Index < c1.Count(); c1Index++)
                                            {
                                                c1[c1Index] = new Coordinate(ls1.Coordinates[c1Index].X, ls1.Coordinates[c1Index].Y);
                                            }

                                            c2 = new Coordinate[ls1.Count - indexForDuplicate];

                                            for (int c2Index = 0; c2Index < c2.Count(); c2Index++)
                                            {
                                                c2[c2Index] = new Coordinate(ls1.Coordinates[indexForDuplicate + c2Index].X, ls1.Coordinates[indexForDuplicate + c2Index].Y);
                                            }
                                        }

                                        LineString testLs1 = new LineString(c1);
                                        LineString testLs2 = new LineString(c2);

                                        if (testLs1.Length > 5 && testLs2.Length > 5)
                                        {
                                            foundIntersect = true;
                                            returnValue = true;

                                            if (TestIfLineStringHasDuplicateNodes(testLs1, true))
                                            {
                                                bool breakhere = true;
                                            }
                                            if (TestIfLineStringHasDuplicateNodes(testLs2, false))
                                            {
                                                bool breakhere = true;
                                            }
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

            featuresList = JoinStopIntersectionsCloseBy(featuresList);

            featuresList = RemoveIntersectionsInSamePosition(featuresList);

            SetObjectIds(featuresList);

            return featuresList;
        }

        public static List<IFeature> RemoveIntersectionsInSamePosition(List<IFeature> featuresList)
        {
            List<IFeature> newFeatures = new List<IFeature>();

            List<IFeature> toRemoveFeatures = new List<IFeature>();

            double tolerance = 5.0f;

            foreach (IFeature feature in featuresList)
            {
                List<IFeature> thisPassRemoveFeatures = new List<IFeature>();
                bool joinTheFeature = false;

                if (!toRemoveFeatures.Contains(feature))
                {
                    foreach (IFeature otherFeature in featuresList)
                    {
                        if (feature != otherFeature && !toRemoveFeatures.Contains(otherFeature))
                        {
                            if (feature is GeometryFeature gf1 && gf1.Geometry is Point p1 && otherFeature is GeometryFeature gf2 && gf2.Geometry is Point p2)
                            {
                                double distance = p1.Distance(p2);

                                if (p1.Coordinate.Equals2D(p2.Coordinate, tolerance))
                                {
                                    joinTheFeature = true;

                                    if (!thisPassRemoveFeatures.Contains(feature))
                                    {
                                        thisPassRemoveFeatures.Add(feature);
                                    }

                                    if (!thisPassRemoveFeatures.Contains(otherFeature))
                                    {
                                        thisPassRemoveFeatures.Add(otherFeature);
                                    }
                                }
                            }
                        }
                    }
                }

                if (joinTheFeature)
                {
                    foreach (IFeature addToRemoveFeature in thisPassRemoveFeatures)
                    {
                        if (!toRemoveFeatures.Contains(addToRemoveFeature))
                            toRemoveFeatures.Add(addToRemoveFeature);
                    }

                    newFeatures.Add(feature);
                }
                else
                {
                    if (!toRemoveFeatures.Contains(feature))
                    {
                        newFeatures.Add(feature);
                    }
                }
            }

            return newFeatures;
        }

        public static List<IFeature> JoinStopIntersectionsCloseBy(List<IFeature> featuresList)
        {
            List<IFeature> newFeatures = new List<IFeature>();

            List<IFeature> toRemoveFeatures = new List<IFeature>();

            double tolerance = 40.0f;

            foreach (IFeature feature in featuresList)
            {
                List<IFeature> thisPassRemoveFeatures = new List<IFeature>();
                bool joinTheFeature = false;

                if (Helper.TryGetFeatureKVPToString(feature, "Intersec_1", "") == "All Way Stop")
                {
                    if (!toRemoveFeatures.Contains(feature))
                    {
                        foreach (IFeature otherFeature in featuresList)
                        {
                            if (Helper.TryGetFeatureKVPToString(otherFeature, "Intersec_1", "") == "All Way Stop")
                            {
                                if (feature != otherFeature && !toRemoveFeatures.Contains(otherFeature))
                                {
                                    if (feature is GeometryFeature gf1 && gf1.Geometry is Point p1 && otherFeature is GeometryFeature gf2 && gf2.Geometry is Point p2)
                                    {
                                        double distance = p1.Distance(p2);

                                        if (p1.Coordinate.Equals2D(p2.Coordinate, tolerance))
                                        {
                                            joinTheFeature = true;

                                            if (!thisPassRemoveFeatures.Contains(feature))
                                            {
                                                thisPassRemoveFeatures.Add(feature);
                                            }

                                            if (!thisPassRemoveFeatures.Contains(otherFeature))
                                            {
                                                thisPassRemoveFeatures.Add(otherFeature);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (joinTheFeature)
                {
                    double averagePosX = 0;
                    double averagePosY = 0;

                    foreach (IFeature addToRemoveFeature in thisPassRemoveFeatures)
                    {
                        if (addToRemoveFeature is GeometryFeature gf1 && gf1.Geometry is Point p1)
                        {
                            averagePosX = averagePosX + p1.X / (double)(thisPassRemoveFeatures.Count);
                            averagePosY = averagePosY + p1.Y / (double)(thisPassRemoveFeatures.Count);

                            if (!toRemoveFeatures.Contains(addToRemoveFeature))
                                toRemoveFeatures.Add(addToRemoveFeature);
                        }
                    }

                    if (feature is GeometryFeature gfOld)
                    {
                        Point newPoint = new Point(averagePosX, averagePosY);
                        GeometryFeature gf = new GeometryFeature(newPoint);

                        gf["Intersecti"] = gfOld["Intersecti"];
                        gf["Intersec_1"] = gfOld["Intersec_1"];
                        gf["highway"] = gfOld["highway"];

                        newFeatures.Add(gf);
                    }
                }
                else
                {
                    if (!toRemoveFeatures.Contains(feature))
                    {
                        newFeatures.Add(feature);
                    }
                }
            }

            return newFeatures;
        }

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
            list.Add("tertiary_link");
            list.Add("secondary_link");
            list.Add("primary_link");

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
            gf["highway"] = oldFeature["highway"];
            gf["CARTO_CLAS"] = oldFeature["CARTO_CLAS"];
            gf["TRUCK_ACCE"] = oldFeature["TRUCK_ACCE"];
            return gf;
        }

        public static GeometryFeature CreateRoadFeature(GeometryFeature gf, Way way)
        {
            gf["STREET"] = GetName(way);
            gf["SPEED_LIMI"] = GetSpeedLimit(way);
            gf["LANES"] = GetLane(way);
            gf["FLOW_DIREC"] = GetDirection(way);
            gf["highway"] = GetHighway(way);
            gf["CARTO_CLAS"] = GetRoadType(way);
            string truckString = "NO ACCESS";
            if (gf["CARTO_CLAS"]?.ToString() is string cartoClass)
            {
                if (cartoClass != "Local Street")
                {
                    truckString = "24HR";
                }
            }
            gf["TRUCK_ACCE"] = truckString;
            return gf;
        }

        public static GeometryFeature CreateIntersectionFeature(GeometryFeature gf, Node node)
        {
            gf["Intersecti"] = GetName(node);
            gf["Intersec_1"] = GetSignalType(node);
            gf["highway"] = GetHighway(node);
            return gf;
        }

        public static string GetRoadType(Way way)
        {
            string returnValue = "Local Street";

            if (way.Tags.TryGetValue("highway", out string value))
            {
                if (value == "residential" || value == "unclassified")
                {
                    returnValue = "Local Street";
                }
                else if (value == "Roundabout")
                {
                    returnValue = "Roundabout";
                }
                else if (value.Contains("link"))
                {
                    returnValue = "Ramp";
                }
                else if (value == "secondary" || value == "tertiary")
                {
                    returnValue = "Collector";
                }
                else if (value == "primary")
                {
                    returnValue = "Arterial";
                }
                else if (value == "motorway" || value == "trunk")
                {
                    returnValue = "Expressway / Highway";
                }
            }

            return returnValue;
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

        public static string GetHighway(Node n)
        {
            string returnValue = "stop";

            if (n.Tags.TryGetValue("highway", out string value))
            {
                returnValue = value;
            }

            return returnValue;
        }

        public static string GetHighway(Way way)
        {
            string returnValue = "unclassified";

            if (way.Tags.TryGetValue("highway", out string value))
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