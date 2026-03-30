using CommunityToolkit.Mvvm.Messaging;
using DocumentFormat.OpenXml.Drawing.Charts;
using Mapsui;
using Mapsui.Nts;
using Mapsui.Projections;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using UrbanEcho.Events.UI;
using UrbanEcho.Messages;
using UrbanEcho.Physics;
using UrbanEcho.Reporting;
using UrbanEcho.Sim;
using UrbanEcho.ViewModels;

namespace UrbanEcho.Models
{
    public class RoadIntersection : IBodyParent, IDisposable
    {
        public List<EdgeTrafficRule> EdgesInto;
        public List<RoadEdge> EdgesOut;

        public Vector2 Center;

        public IntersectionBody? Body;

        public string Name = "test123";

        public IFeature Feature;

        private bool isBodySet = false;
        private bool isCenterSet = false;
        public TrafficRule FallBackTrafficRule;//Used if intersection has no connecting edges

        public SignalType TheSignalType = SignalType.Uncontrolled;

        private List<PairedTrafficRule> pairedRoads;

        private float aadtSumEdgesInto = 0;
        private float ratioForSignal = 0;//Ratio for how long first paired roads allow traffic

        //Seconds on is trafficLightCycleTime * ratio
        private float trafficLightCycleTime = 60.0f;//Time for a traffic light cycle

        private float offsetTime = 0;//Add a random offset so everything doesn't seem like its running on same time
        private bool wasFirstPartOfCycle = true;
        private float timeWhenStartedCurrentPartOfCycle = 0.0f;
        private float amountOfTimeForAllRed = 2.0f;
        private bool didSetCurrentCycleValues = true;
        private bool didSetBlockedForAllRed = false;
        private bool firstCycleInitialized = false;

        private RecordedStats stats = new RecordedStats();

        public enum SignalType
        {
            [Description("All Way Stop")]
            AllWayStop,

            [Description("Flasher")]
            Flasher,

            [Description("Full Signal")]
            FullSignal,

            [Description("Pedestrian Signal")]
            PedestrianSignal,

            Ramp,
            Rotary,

            [Description("Stop with LRT Signal")]
            StopLRTSignal,

            [Description("Two Way Stop")]
            TwoWayStop,

            Uncontrolled
        };

        private RoadIntersection(string name, IFeature feature, RoadGraph graph)
        {
            this.Name = name;
            Feature = feature;
            EdgesInto = new List<EdgeTrafficRule>();
            EdgesOut = new List<RoadEdge>();
            pairedRoads = new List<PairedTrafficRule>();
            TheSignalType = SetSignalType();
            //Only allow few types to be created
            if (TheSignalType == SignalType.AllWayStop || TheSignalType == SignalType.TwoWayStop || TheSignalType == SignalType.FullSignal)
            {
                if (feature is GeometryFeature intersectGF)
                {
                    if (intersectGF.Geometry is Point p)
                    {
                        Center = Helpers.Helper.Convert2Box2dWorldPosition(p);
                        isCenterSet = true;

                        (double lon, double lat) = SphericalMercator.ToLonLat(p.X, p.Y);
                        stats.SetPosition(lat, lon);
                    }
                }
            }
        }

        public static RoadIntersection? Create(string name, IFeature feature, RoadGraph graph)
        {
            RoadIntersection? returnValue = new RoadIntersection(name, feature, graph);

            if (returnValue.isCenterSet == false)//If center not set just return null
            {
                returnValue = null;
            }
            else
            {
                returnValue.offsetTime = 5.0f + (float)Random.Shared.NextDouble() * 10.0f;

                List<(Vector2 direction, float width)> connectionsForBody = returnValue.setConnections(graph, 15.0f);
                if (connectionsForBody.Count == 0)
                {
                    //Try again with higher threshold for connecting roads (don't use higher at first or some roads get linked when they shouldn't)
                    connectionsForBody = returnValue.setConnections(graph, 20.0f);
                }

                if (returnValue.isCenterSet && graph is not null)
                {
                    returnValue.Body = new IntersectionBody(returnValue, connectionsForBody);
                    returnValue.isBodySet = true;
                    returnValue.FallBackTrafficRule = TrafficRule.SetDefaultTrafficRule();

                    string currentName = Helpers.Helper.TryGetFeatureKVPToString(returnValue.Feature, "Intersecti", "");

                    if (currentName == "Unnamed" || currentName == "")
                    {
                        SetName(returnValue);
                    }

                    if (returnValue.Feature.Fields.Contains("highway"))
                    {
                        string intersectionType = Helpers.Helper.TryGetFeatureKVPToString(returnValue.Feature, "Intersec_1", "");
                        if (intersectionType == "All Way Stop")
                        {
                            SetStopTypeOSM(returnValue);
                        }
                        returnValue.TheSignalType = returnValue.SetSignalType(); //Update signal type in case it was changed
                    }

                    returnValue.SetStaticTrafficRules();
                }
                else
                {
                    returnValue = null;
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to add a intersection"));
                }
            }

            if (returnValue != null)
            {
                //Register Stats update event
                foreach (EdgeTrafficRule edgeTrafficRule in returnValue.EdgesInto)
                {
                    edgeTrafficRule.RoadEdge.UpdateIntersectionStats += returnValue.UpdateStats;
                }
            }
            return returnValue;
        }

        //Set stop type if osm data was used
        private static void SetStopTypeOSM(RoadIntersection roadIntersection)
        {
            bool doneSettingType = false;
            foreach (EdgeTrafficRule edgeTrafficRule in roadIntersection.EdgesInto)
            {
                string roadType1 = Helpers.Helper.TryGetFeatureKVPToString(edgeTrafficRule.RoadEdge.Feature, "highway", "");

                foreach (EdgeTrafficRule otherEdgeTrafficRule in roadIntersection.EdgesInto)
                {
                    if (roadType1 == "")
                    {
                        break;
                    }
                    string roadType2 = Helpers.Helper.TryGetFeatureKVPToString(otherEdgeTrafficRule.RoadEdge.Feature, "highway", "");
                    if (roadType2 == "")
                    {
                        continue;
                    }

                    if (roadType1 != roadType2)//If two different types of highway set as a two way stop
                    {
                        roadIntersection.Feature["Intersec_1"] = "Two Way Stop";
                    }
                }

                if (doneSettingType)
                {
                    break;
                }
            }
        }

        private static void SetName(RoadIntersection roadIntersection)
        {
            bool doneSettingName = false;
            foreach (EdgeTrafficRule edgeTrafficRule in roadIntersection.EdgesInto)
            {
                string roadName1 = Helpers.Helper.TryGetFeatureKVPToString(edgeTrafficRule.RoadEdge.Feature, "STREET", "");

                foreach (EdgeTrafficRule otherEdgeTrafficRule in roadIntersection.EdgesInto)
                {
                    if (roadName1 == "")
                    {
                        break;
                    }
                    string roadName2 = Helpers.Helper.TryGetFeatureKVPToString(otherEdgeTrafficRule.RoadEdge.Feature, "STREET", "");
                    if (roadName2 == "")
                    {
                        continue;
                    }

                    if (roadName1 != roadName2)
                    {
                        if (roadName1.Length > 4 && roadName2.Length > 4)
                        {
                            //Do just a short name compare
                            if (roadName1.Substring(0, 4) == roadName2.Substring(0, 4))
                            {
                                continue;
                            }
                            else
                            {
                                roadIntersection.Feature["Intersecti"] = $"{roadName1} @ {roadName2}";
                                roadIntersection.Name = $"{roadName1} @ {roadName2}";
                                doneSettingName = true;
                                break;
                            }
                        }
                    }
                }

                if (doneSettingName)
                {
                    break;
                }
            }
        }

        private void SetStaticTrafficRules()
        {
            if (TheSignalType == SignalType.AllWayStop || TheSignalType == SignalType.TwoWayStop)
            {
                SetStaticTrafficRulesStopSign();
            }
            else if (TheSignalType == SignalType.FullSignal)
            {
                SetStaticTrafficRulesFullSignal();
            }
            else
            {
                FallBackTrafficRule = TrafficRule.SetDefaultTrafficRule();

                foreach (EdgeTrafficRule edgeTrafficRule in EdgesInto)
                {
                    edgeTrafficRule.TrafficRule = TrafficRule.SetDefaultTrafficRule();
                }
            }
        }

        private void SetStaticTrafficRulesFullSignal()
        {
            FallBackTrafficRule = TrafficRule.SetDefaultTrafficRule();

            foreach (EdgeTrafficRule edgeTrafficRule in EdgesInto)
            {
                edgeTrafficRule.TrafficRule = TrafficRule.SetDefaultTrafficRule();
            }
            //If there are only two edges in set then set the two as a pair
            if (EdgesInto.Count <= 2)
            {
                PairedTrafficRule pairedTrafficRule = new PairedTrafficRule(EdgesInto);
                pairedRoads.Add(pairedTrafficRule);
            }
            if (EdgesInto.Count > 2)
            {
                List<EdgeTrafficRule> firstPairedEdges = new List<EdgeTrafficRule>();

                foreach (EdgeTrafficRule edgeTrafficRule1 in EdgesInto)
                {
                    if (firstPairedEdges.Count > 0)
                    {
                        break;//Found first paired edges so stop
                    }
                    string? roadName1 = edgeTrafficRule1.RoadEdge.Feature["STREET"]?.ToString();

                    foreach (EdgeTrafficRule edgeTrafficRule2 in EdgesInto)
                    {
                        if (edgeTrafficRule1 == edgeTrafficRule2)
                        {
                            continue;
                        }

                        string? roadName2 = edgeTrafficRule2.RoadEdge.Feature["STREET"]?.ToString();

                        if (roadName1 == roadName2)
                        {
                            if (!firstPairedEdges.Contains(edgeTrafficRule1))
                            {
                                firstPairedEdges.Add(edgeTrafficRule1);
                            }
                            if (!firstPairedEdges.Contains(edgeTrafficRule2))
                            {
                                firstPairedEdges.Add(edgeTrafficRule2);
                            }
                        }
                        else //Try another compare that is not exactly same name
                        {
                            if (roadName1 != null && roadName2 != null)
                            {
                                if (roadName1.Length > 4 && roadName2.Length > 4)
                                {
                                    //Do just a short name compare
                                    if (roadName1.Substring(0, 4) == roadName2.Substring(0, 4))
                                    {
                                        if (!firstPairedEdges.Contains(edgeTrafficRule1))
                                        {
                                            firstPairedEdges.Add(edgeTrafficRule1);
                                        }
                                        if (!firstPairedEdges.Contains(edgeTrafficRule2))
                                        {
                                            firstPairedEdges.Add(edgeTrafficRule2);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (!Feature.Fields.Contains("highway")) //Only use this method if it was not a osm file
                {
                    //no pairs found try matching by using closest aadt values
                    if (firstPairedEdges.Count == 0)
                    {
                        foreach (EdgeTrafficRule edgeTrafficRule1 in EdgesInto)
                        {
                            float aadtValue1 = Helpers.Helper.TryGetFeatureKVPToFloat(edgeTrafficRule1.RoadEdge.Feature, "AADT", 0);
                            bool oneMatch = false;
                            float closestMatch = 0;
                            EdgeTrafficRule closestRule = EdgesInto[1];//This cannot be null and should get overwritten with correct value
                            float difference = 0;
                            foreach (EdgeTrafficRule edgeTrafficRule2 in EdgesInto)
                            {
                                if (edgeTrafficRule1 == edgeTrafficRule2)
                                {
                                    continue;
                                }
                                float aadtValue2 = Helpers.Helper.TryGetFeatureKVPToFloat(edgeTrafficRule2.RoadEdge.Feature, "AADT", 0);
                                if (oneMatch == false)
                                {
                                    closestRule = edgeTrafficRule2;
                                    oneMatch = true;
                                    difference = Math.Abs(aadtValue1 - aadtValue2);
                                    closestMatch = difference;
                                }
                                else
                                {
                                    difference = Math.Abs(aadtValue1 - aadtValue2);
                                    if (difference < closestMatch)
                                    {
                                        closestRule = edgeTrafficRule2;
                                        closestMatch = difference;
                                    }
                                }
                            }

                            if (!firstPairedEdges.Contains(edgeTrafficRule1))
                            {
                                firstPairedEdges.Add(edgeTrafficRule1);
                            }
                            if (!firstPairedEdges.Contains(closestRule))
                            {
                                firstPairedEdges.Add(closestRule);
                            }

                            if (firstPairedEdges.Count != 0)
                            {
                                break;
                            }
                        }
                    }
                }
                else
                {
                    //used for osm instead of aadt get road priority values
                    if (firstPairedEdges.Count == 0)
                    {
                        foreach (EdgeTrafficRule edgeTrafficRule1 in EdgesInto)
                        {
                            string highwayClass1 = Helpers.Helper.TryGetFeatureKVPToString(edgeTrafficRule1.RoadEdge.Feature, "CARTO_CLAS", "");
                            int priority1 = Helpers.Helper.GetPriority(highwayClass1);
                            bool oneMatch = false;
                            float closestMatch = 0;
                            EdgeTrafficRule closestRule = EdgesInto[1];//This cannot be null and should get overwritten with correct value
                            float difference = 0;
                            foreach (EdgeTrafficRule edgeTrafficRule2 in EdgesInto)
                            {
                                if (edgeTrafficRule1 == edgeTrafficRule2)
                                {
                                    continue;
                                }
                                string highwayClass2 = Helpers.Helper.TryGetFeatureKVPToString(edgeTrafficRule2.RoadEdge.Feature, "CARTO_CLAS", "");
                                int priority2 = Helpers.Helper.GetPriority(highwayClass2);

                                if (oneMatch == false)
                                {
                                    closestRule = edgeTrafficRule2;
                                    oneMatch = true;
                                    difference = Math.Abs(priority1 - priority2);
                                    closestMatch = difference;
                                }
                                else
                                {
                                    difference = Math.Abs(priority1 - priority2);
                                    if (difference < closestMatch)
                                    {
                                        closestRule = edgeTrafficRule2;
                                        closestMatch = difference;
                                    }
                                }
                            }

                            if (!firstPairedEdges.Contains(edgeTrafficRule1))
                            {
                                firstPairedEdges.Add(edgeTrafficRule1);
                            }
                            if (!firstPairedEdges.Contains(closestRule))
                            {
                                firstPairedEdges.Add(closestRule);
                            }

                            if (firstPairedEdges.Count != 0)
                            {
                                break;
                            }
                        }
                    }
                }

                if (firstPairedEdges.Count == 0)
                {
                    PairedTrafficRule pairedTrafficRule = new PairedTrafficRule(EdgesInto);
                    pairedRoads.Add(pairedTrafficRule);
                }
                else
                {
                    PairedTrafficRule pairedTrafficRule1 = new PairedTrafficRule(firstPairedEdges);
                    pairedRoads.Add(pairedTrafficRule1);

                    List<EdgeTrafficRule> secondPairedEdges = new List<EdgeTrafficRule>();

                    foreach (EdgeTrafficRule edgeTrafficRule in EdgesInto)
                    {
                        if (!firstPairedEdges.Contains(edgeTrafficRule))
                        {
                            secondPairedEdges.Add(edgeTrafficRule);
                        }
                    }
                    if (secondPairedEdges.Count != 0)
                    {
                        PairedTrafficRule pairedTrafficRule2 = new PairedTrafficRule(secondPairedEdges);
                        pairedRoads.Add(pairedTrafficRule2);
                    }
                }
            }

            if (pairedRoads.Count > 1)
            {
                float computeRatioForSignal = 1.0f;
                if (aadtSumEdgesInto != 0)
                {
                    computeRatioForSignal = pairedRoads[0].CombinedAADT / aadtSumEdgesInto;
                }
                else
                {
                    if (pairedRoads[0] != null && pairedRoads[1] != null)
                    {
                        if (pairedRoads[0].TrafficRules.Count > 0 && pairedRoads[1].TrafficRules.Count > 0)
                        {
                            int priorityDifference = Helpers.Helper.GetPriority(pairedRoads[0].TrafficRules[0].RoadEdge.Metadata.RoadType) -
                                Helpers.Helper.GetPriority(pairedRoads[1].TrafficRules[0].RoadEdge.Metadata.RoadType);
                            computeRatioForSignal = 0.5f + priorityDifference * 0.25f;//give signal ratio based on road type priority
                        }
                    }
                }

                ratioForSignal = Math.Clamp(computeRatioForSignal, 0.2f, 0.8f);
            }
        }

        private void SetStaticTrafficRulesStopSign()
        {
            FallBackTrafficRule = TrafficRule.SetStopSignTrafficRule();
            foreach (EdgeTrafficRule edgeTrafficRule in EdgesInto)
            {
                edgeTrafficRule.TrafficRule = TrafficRule.SetStopSignTrafficRule();
            }

            if (TheSignalType == SignalType.TwoWayStop)
            {
                if (EdgesInto.Count == 0)
                {
                    FallBackTrafficRule.SetNeverBlock();
                }

                if (EdgesInto.Count > 1)
                {
                    if (!Feature.Fields.Contains("highway")) //Only use this method if it was not a osm file)
                    {
                        if (EdgesInto.Count <= 3)
                        {
                            //check if two names are the same
                            bool twoNamesSame = false;

                            if (EdgesInto.Count == 3)
                            {
                                string? roadName1 = EdgesInto[0].RoadEdge.Feature["STREET"]?.ToString();
                                string? roadName2 = EdgesInto[1].RoadEdge.Feature["STREET"]?.ToString();
                                string? roadName3 = EdgesInto[2].RoadEdge.Feature["STREET"]?.ToString();

                                if (roadName1 == roadName2)
                                {
                                    twoNamesSame = true;

                                    EdgesInto[0].TrafficRule.SetNeverBlock();
                                    EdgesInto[1].TrafficRule.SetNeverBlock();
                                }
                                else
                                {
                                    if (roadName1 == roadName3)
                                    {
                                        twoNamesSame = true;

                                        EdgesInto[0].TrafficRule.SetNeverBlock();
                                        EdgesInto[2].TrafficRule.SetNeverBlock();
                                    }
                                    else
                                    {
                                        if (roadName2 == roadName3)
                                        {
                                            twoNamesSame = true;

                                            EdgesInto[1].TrafficRule.SetNeverBlock();
                                            EdgesInto[2].TrafficRule.SetNeverBlock();
                                        }
                                    }
                                }
                            }

                            if (!twoNamesSame)
                            {
                                int indexForLowestAADT = 0;
                                float lowestAADTValueFound = float.PositiveInfinity;

                                //find lowest AADT value of the two or three and set the other edges to never block
                                for (int i = 0; i < EdgesInto.Count; i++)
                                {
                                    float aadtValue = Helpers.Helper.TryGetFeatureKVPToFloat(EdgesInto[i].RoadEdge.Feature, "AADT", 0);

                                    if (aadtValue < lowestAADTValueFound)
                                    {
                                        indexForLowestAADT = i;
                                        lowestAADTValueFound = aadtValue;
                                    }
                                }

                                for (int i = 0; i < EdgesInto.Count; i++)
                                {
                                    if (i != indexForLowestAADT)
                                    {
                                        EdgesInto[i].TrafficRule.SetNeverBlock();
                                    }
                                }
                            }
                        }
                        if (EdgesInto.Count >= 4)
                        {
                            //find two lowest AADT values and set the other edges to never block
                            List<(float aadt, EdgeTrafficRule edgeRule)> aadtValues = new List<(float aadt, EdgeTrafficRule rule)>();

                            //get a list of the AADT values
                            for (int i = 0; i < EdgesInto.Count; i++)
                            {
                                aadtValues.Add((Helpers.Helper.TryGetFeatureKVPToFloat(EdgesInto[i].RoadEdge.Feature, "AADT", 0), EdgesInto[i]));
                            }
                            //sort from lowest to highest AADT value
                            aadtValues.Sort((edge1, edge2) => edge1.aadt.CompareTo(edge2.aadt));
                            //Set any that are not the two lowest values to never block
                            for (int i = 2; i < aadtValues.Count; i++)
                            {
                                aadtValues[i].edgeRule.TrafficRule.SetNeverBlock();
                            }
                        }
                    }
                    else //Only use this method if it was a osm file)
                    {
                        if (EdgesInto.Count <= 3)
                        {
                            //check if two names are the same
                            bool twoNamesSame = false;

                            if (EdgesInto.Count == 3)
                            {
                                string? roadName1 = EdgesInto[0].RoadEdge.Feature["STREET"]?.ToString();
                                string? roadName2 = EdgesInto[1].RoadEdge.Feature["STREET"]?.ToString();
                                string? roadName3 = EdgesInto[2].RoadEdge.Feature["STREET"]?.ToString();

                                if (roadName1 == roadName2)
                                {
                                    twoNamesSame = true;

                                    EdgesInto[0].TrafficRule.SetNeverBlock();
                                    EdgesInto[1].TrafficRule.SetNeverBlock();
                                }
                                else
                                {
                                    if (roadName1 == roadName3)
                                    {
                                        twoNamesSame = true;

                                        EdgesInto[0].TrafficRule.SetNeverBlock();
                                        EdgesInto[2].TrafficRule.SetNeverBlock();
                                    }
                                    else
                                    {
                                        if (roadName2 == roadName3)
                                        {
                                            twoNamesSame = true;

                                            EdgesInto[1].TrafficRule.SetNeverBlock();
                                            EdgesInto[2].TrafficRule.SetNeverBlock();
                                        }
                                    }
                                }
                            }

                            if (!twoNamesSame)
                            {
                                int indexForLowestPriority = 0;
                                int lowestPriorityValueFound = int.MaxValue;

                                //find lowest AADT value of the two or three and set the other edges to never block
                                for (int i = 0; i < EdgesInto.Count; i++)
                                {
                                    int priorityValue = Helpers.Helper.GetPriority(EdgesInto[i].RoadEdge.Metadata.RoadType);

                                    if (priorityValue < lowestPriorityValueFound)
                                    {
                                        indexForLowestPriority = i;
                                        lowestPriorityValueFound = priorityValue;
                                    }
                                }

                                for (int i = 0; i < EdgesInto.Count; i++)
                                {
                                    if (i != indexForLowestPriority)
                                    {
                                        EdgesInto[i].TrafficRule.SetNeverBlock();
                                    }
                                }
                            }
                        }
                        if (EdgesInto.Count >= 4)
                        {
                            //find two lowest priority values and set the other edges to never block
                            List<(int priority, EdgeTrafficRule edgeRule)> priorityValues = new List<(int priority, EdgeTrafficRule rule)>();

                            //get a list of the AADT values
                            for (int i = 0; i < EdgesInto.Count; i++)
                            {
                                priorityValues.Add((Helpers.Helper.GetPriority(EdgesInto[i].RoadEdge.Metadata.RoadType), EdgesInto[i]));
                            }
                            //sort from lowest to highest AADT value
                            priorityValues.Sort((edge1, edge2) => Helpers.Helper.GetPriority(edge1.edgeRule.RoadEdge.Metadata.RoadType).CompareTo(Helpers.Helper.GetPriority(edge2.edgeRule.RoadEdge.Metadata.RoadType)));
                            //Set any that are not the two lowest values to never block
                            for (int i = 2; i < priorityValues.Count; i++)
                            {
                                priorityValues[i].edgeRule.TrafficRule.SetNeverBlock();
                            }
                        }
                    }
                }
            }
        }

        public SignalType SetSignalType()
        {
            SignalType type = SignalType.Uncontrolled;

            switch (Feature["Intersec_1"]?.ToString())
            {
                case "Two Way Stop":
                    type = SignalType.TwoWayStop;
                    break;

                case "All Way Stop":
                    type = SignalType.AllWayStop;
                    break;

                case "Flasher":
                    type = SignalType.Flasher;
                    break;

                case "Full Signal":
                    type = SignalType.FullSignal;
                    break;

                case "Intersection Pedestrian Signal":
                    type = SignalType.PedestrianSignal;
                    break;

                case "Pedestrian Crossover":
                    type = SignalType.PedestrianSignal;
                    break;

                case "Stop with LRT Signals":
                    type = SignalType.StopLRTSignal;
                    break;

                default:

                    break;
            }

            return type;
        }

        public bool IsBodySet()
        {
            return isBodySet;
        }

        private List<(Vector2 pos, float width)> setConnections(RoadGraph graph, float thresholdToUse)
        {
            List<(Vector2 pos, float width)> connectingPoints = new List<(Vector2 pos, float width)>();

            if (graph is null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Graph was null when trying to add intersection"));
            }
            else
            {
                for (int edgeIndex = 0; edgeIndex < graph.Edges.Count; edgeIndex++)
                {
                    IFeature roadFeature = graph.Edges[edgeIndex].Feature;
                    RoadEdge roadEdge = graph.Edges[edgeIndex];

                    if (roadFeature is GeometryFeature gf)
                        if (gf.Geometry is LineString lineString)
                        {
                            if (lineString.Length < 10.0f)
                            {
                                continue;//Do not include very short roads that may start and end within the intersection
                            }
                            if (lineString.Count > 1)
                            {
                                //Add incoming connections
                                bool forwardDirection = roadEdge.IsFromStartOfLineString;

                                bool connectionHasBeenAdded = CheckIfAddConnection(true);

                                //If that edge was not a outgoing edge check if it is a incoming
                                //edge by flipping direction and checking the from end
                                if (!(connectionHasBeenAdded))
                                {
                                    connectionHasBeenAdded = CheckIfAddConnection(false);
                                }

                                bool CheckIfAddConnection(bool checkingOutGoing)
                                {
                                    bool directionToUse = (checkingOutGoing) ? forwardDirection : !forwardDirection;
                                    int startIndex = 0;
                                    if (!directionToUse)
                                    {
                                        startIndex = lineString.Count - 1;
                                    }
                                    float threshold = thresholdToUse;
                                    Vector2 roadFeaturePos = Helpers.Helper.Convert2Box2dWorldPosition(lineString.Coordinates[startIndex].X, lineString.Coordinates[startIndex].Y);
                                    bool connectionAdded = false;
                                    if (Vector2.Distance(Center, roadFeaturePos) < threshold)
                                    {
                                        int nextIndexForSegment = (directionToUse) ? startIndex + 1 : startIndex - 1;
                                        Vector2 end = Helpers.Helper.Convert2Box2dWorldPosition(lineString.Coordinates[nextIndexForSegment].X, lineString.Coordinates[nextIndexForSegment].Y);

                                        double pavementWidth = 0;

                                        int lanes = Helpers.Helper.TryGetFeatureKVPToInt(gf, "LANES", 2);
                                        pavementWidth = lanes * Helpers.Helper.DefaultLaneWidth * Helpers.Helper.ExtraPavementFactor;

                                        if (checkingOutGoing)
                                        {
                                            EdgesOut.Add(roadEdge);
                                            if (!(connectingPoints.Any(point => point.pos == end)))
                                            {
                                                connectingPoints.Add((end, (float)pavementWidth));
                                            }
                                        }
                                        else
                                        {
                                            TrafficRule trafficRule = TrafficRule.SetDefaultTrafficRule();

                                            EdgesInto.Add(new EdgeTrafficRule(roadEdge, trafficRule));
                                            aadtSumEdgesInto += Helpers.Helper.TryGetFeatureKVPToFloat(roadEdge.Feature, "AADT", 0);

                                            if (!(connectingPoints.Any(point => point.pos == end)))
                                            {
                                                connectingPoints.Add((end, (float)pavementWidth));
                                            }
                                        }
                                        connectionAdded = true;
                                    }
                                    return connectionAdded;
                                }
                            }
                        }
                }
            }
            return connectingPoints;
        }

        public void UpdateTrafficRules()
        {
            if (TheSignalType == SignalType.AllWayStop || TheSignalType == SignalType.TwoWayStop)
            {
                UpdateStopSignRule();
            }
            if (TheSignalType == SignalType.FullSignal)
            {
                UpdateFullSignalRule();
            }
        }

        private void UpdateFullSignalRule()
        {
            if (pairedRoads.Count < 2)
            {
                //Just keep signals unblocked all the time and do nothing
            }
            else
            {
                //gives a 0 to 1 value for where in traffic light cycle it is in
                float cyclePostion = ((offsetTime + SimManager.Instance.GetSimTime()) % trafficLightCycleTime) / trafficLightCycleTime;
                bool isFirstPartOfCycle = (cyclePostion <= ratioForSignal);
                if (isFirstPartOfCycle != wasFirstPartOfCycle || !firstCycleInitialized)
                {
                    firstCycleInitialized = true;
                    didSetBlockedForAllRed = false;
                    didSetCurrentCycleValues = false;
                    wasFirstPartOfCycle = isFirstPartOfCycle;
                    timeWhenStartedCurrentPartOfCycle = SimManager.Instance.GetSimTime();
                }

                if (timeWhenStartedCurrentPartOfCycle + amountOfTimeForAllRed > SimManager.Instance.GetSimTime())
                {
                    if (!didSetBlockedForAllRed)
                    {
                        foreach (EdgeTrafficRule edgeTrafficRule in EdgesInto)//Set all as blocked if during all red part of cycle
                        {
                            edgeTrafficRule.TrafficRule.SetBlock(true);
                        }
                        didSetBlockedForAllRed = true;
                    }
                }
                else
                {
                    if (!didSetCurrentCycleValues)
                    {
                        //Choose what pair of roads unblock traffic this only needs to be done once after the all red part of cycle
                        foreach (EdgeTrafficRule edgeTrafficRule in EdgesInto)
                        {
                            if (pairedRoads[0].TrafficRules.Contains(edgeTrafficRule))
                            {
                                if (cyclePostion <= ratioForSignal)
                                {
                                    edgeTrafficRule.TrafficRule.SetBlock(false);
                                }
                                else
                                {
                                    edgeTrafficRule.TrafficRule.SetBlock(true);
                                }
                            }
                            else //if edge isn't in first road pair list apply opposite blocking rules
                            {
                                if (cyclePostion <= ratioForSignal)
                                {
                                    edgeTrafficRule.TrafficRule.SetBlock(true);
                                }
                                else
                                {
                                    edgeTrafficRule.TrafficRule.SetBlock(false);
                                }
                            }
                        }
                        didSetCurrentCycleValues = true;
                    }
                }
            }
        }

        private void UpdateStopSignRule()
        {
            if (EdgesInto.Count != 0)
            {
                //allow one incoming to unblock at a time every 3.3 seconds
                int every2Seconds = (int)((offsetTime + SimManager.Instance.GetSimTime()) * 0.3f);
                int edgeToUnblock = every2Seconds % (EdgesInto.Count);

                for (int i = 0; i < EdgesInto.Count; i++)
                {
                    if (EdgesInto[i].TrafficRule.IsNeverBlockingTraffic() == false)
                    {
                        if (edgeToUnblock != i)
                        {
                            EdgesInto[i].TrafficRule.SetBlock(true);
                        }
                        else
                        {
                            EdgesInto[i].TrafficRule.SetBlock(false);
                        }
                    }
                    else
                    {
                        EdgesInto[i].TrafficRule.SetBlock(false);
                    }
                }
            }

            if (FallBackTrafficRule.IsNeverBlockingTraffic() == false)
            {
                int every2Seconds = (int)((offsetTime + SimManager.Instance.GetSimTime()) * 0.5f);
                int fallBackBlocking = (every2Seconds) % 2;
                if (fallBackBlocking == 0)
                {
                    FallBackTrafficRule.SetBlock(true);
                }
                else
                {
                    FallBackTrafficRule.SetBlock(false);
                }
            }
            else
            {
                FallBackTrafficRule.SetBlock(false);
            }
        }

        public IReadOnlyList<IFeature> GetConnectedRoadFeatures()
        {
            var features = new List<IFeature>();
            var addedGeometries = new HashSet<Geometry>();

            foreach (EdgeTrafficRule etr in EdgesInto)
            {
                if (etr.RoadEdge.Feature is not GeometryFeature gf || gf.Geometry is null) continue;

                bool hasRightOfWay = TheSignalType switch
                {
                    SignalType.FullSignal => !etr.TrafficRule.IsBlockingTraffic(),
                    SignalType.TwoWayStop => etr.TrafficRule.IsNeverBlockingTraffic(),
                    _ => false
                };

                var clone = new GeometryFeature(gf.Geometry);
                clone["RightOfWay"] = hasRightOfWay ? 1 : 0;
                features.Add(clone);
                addedGeometries.Add(gf.Geometry);
            }

            foreach (RoadEdge edge in EdgesOut)
            {
                if (edge.Feature is not GeometryFeature gf || gf.Geometry is null) continue;
                if (addedGeometries.Contains(gf.Geometry)) continue;

                var clone = new GeometryFeature(gf.Geometry);
                clone["RightOfWay"] = 0;
                features.Add(clone);
            }

            return features;
        }

        public void ChangeSignalType(SignalType newSignalType)
        {
            TheSignalType = newSignalType;

            Feature["Intersec_1"] = newSignalType switch
            {
                SignalType.TwoWayStop => "Two Way Stop",
                SignalType.AllWayStop => "All Way Stop",
                SignalType.FullSignal => "Full Signal",
                SignalType.Flasher => "Flasher",
                _ => ""
            };

            EventQueueForUI.Instance.Add(new RefreshMapEvent(MainWindow.Instance.GetMainViewModel().Map.MyMap));
            //pairedRoads.Clear();
            //ratioForSignal = 0;
            //firstCycleInitialized = false;

            RecalculateStaticTrafficRules();
        }

        private void RecalculateStaticTrafficRules()
        {
        }

        public void ApplyStopSignAssignment(List<(EdgeTrafficRule edge, bool hasStopSign)> assignments)
        {
            foreach (var (edge, hasStopSign) in assignments)
            {
                edge.TrafficRule = hasStopSign ? TrafficRule.SetStopSignTrafficRule() : TrafficRule.SetStopSignTrafficRule();
                if (!hasStopSign) { edge.TrafficRule.SetNeverBlock(); }
            }
        }

        public RecordedStats GetStats()
        {
            return this.stats;
        }

        private void UpdateStats(Stats incomingStats)
        {
            stats.RecordVehicle(incomingStats);
        }

        public void ResetStats()
        {
            stats.Reset();
        }

        public void Dispose()
        {
            foreach (EdgeTrafficRule edgeTrafficRule in this.EdgesInto)
            {
                edgeTrafficRule.RoadEdge.UpdateIntersectionStats -= this.UpdateStats;
            }
        }
    }
}