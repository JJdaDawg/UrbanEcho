using Mapsui;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using UrbanEcho.Physics;
using UrbanEcho.Sim;

namespace UrbanEcho.Models
{
    public class RoadIntersection : IBodyParent
    {
        public List<EdgeTrafficRule> EdgesInto;
        public List<RoadEdge> EdgesOut;

        public Vector2 Center;

        public IntersectionBody Body;

        public string Name = "test123";

        //public float WaitTime = 5.0f;

        public IFeature Feature;

        private bool isBodySet = false;

        public TrafficRule FallBackTrafficRule;//Used if intersection has no connecting edges

        public SignalType TheSignalType = SignalType.Uncontrolled;

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

        public RoadIntersection(string name, float waitTime, IFeature feature, RoadGraph graph)
        {
            this.Name = name;
            Feature = feature;
            EdgesInto = new List<EdgeTrafficRule>();
            EdgesOut = new List<RoadEdge>();
            //WaitTime = waitTime;
            bool isCenterSet = false;

            if (feature is GeometryFeature intersectGF)
            {
                if (intersectGF.Geometry is Point p)
                {
                    Center = Helpers.Helper.Convert2Box2dWorldPosition(p);
                    isCenterSet = true;
                }
            }

            if (isCenterSet && graph is not null)
            {
                List<(Vector2 direction, float width)> connectionsForBody = setConnections(graph);

                Body = new IntersectionBody(this, connectionsForBody);
                isBodySet = true;
            }
            FallBackTrafficRule = TrafficRule.SetDefaultTrafficRule();
            SetStaticTrafficRules();
        }

        private void SetStaticTrafficRules()
        {
            TheSignalType = SetSignalType();
            if (TheSignalType == SignalType.AllWayStop || TheSignalType == SignalType.TwoWayStop)
            {
                FallBackTrafficRule = TrafficRule.SetStopSignTrafficRule();
            }
            else if (TheSignalType == SignalType.FullSignal)
            {
                FallBackTrafficRule = TrafficRule.SetDefaultTrafficRule();
            }

            foreach (EdgeTrafficRule edgeTrafficRule in EdgesInto)
            {
                if (TheSignalType == SignalType.AllWayStop || TheSignalType == SignalType.TwoWayStop)
                {
                    edgeTrafficRule.TrafficRule = TrafficRule.SetStopSignTrafficRule();
                }
                else
                {
                    edgeTrafficRule.TrafficRule = TrafficRule.SetDefaultTrafficRule();
                }
            }
            if (TheSignalType == SignalType.TwoWayStop)
            {
                if (EdgesInto.Count == 0)
                {
                    FallBackTrafficRule.SetNeverBlock();
                }

                if (EdgesInto.Count > 1)
                {
                    if (EdgesInto.Count <= 3)
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

        private List<(Vector2 pos, float width)> setConnections(RoadGraph graph)
        {
            List<(Vector2 pos, float width)> connectingPoints = new List<(Vector2 pos, float width)>();

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
                                float threshold = 15.0f;
                                Vector2 roadFeaturePos = Helpers.Helper.Convert2Box2dWorldPosition(lineString.Coordinates[startIndex].X, lineString.Coordinates[startIndex].Y);
                                bool connectionAdded = false;
                                if (Vector2.Distance(Center, roadFeaturePos) < threshold)
                                {
                                    int nextIndexForSegment = (directionToUse) ? startIndex + 1 : startIndex - 1;
                                    Vector2 end = Helpers.Helper.Convert2Box2dWorldPosition(lineString.Coordinates[nextIndexForSegment].X, lineString.Coordinates[nextIndexForSegment].Y);
                                    //TODO: update key value and minWidth to not be hardcoded here
                                    float minPavementWidth = 8.0f;
                                    float width = Helpers.Helper.DoMapCorrection(Helpers.Helper.TryGetFeatureKVPToFloat(roadFeature, "PAVEMENT_W", minPavementWidth));

                                    if (width < minPavementWidth)
                                    {
                                        width = minPavementWidth;
                                    }

                                    width *= 1.25f;//Add bit of width just incase intersection is not centered
                                    if (checkingOutGoing)
                                    {
                                        EdgesOut.Add(roadEdge);
                                        if (!(connectingPoints.Any(point => point.pos == end)))
                                        {
                                            connectingPoints.Add((end, width));
                                        }
                                    }
                                    else
                                    {
                                        TrafficRule trafficRule = TrafficRule.SetDefaultTrafficRule();

                                        EdgesInto.Add(new EdgeTrafficRule(roadEdge, trafficRule));
                                        if (!(connectingPoints.Any(point => point.pos == end)))
                                        {
                                            connectingPoints.Add((end, width));
                                        }
                                    }
                                    connectionAdded = true;
                                }
                                return connectionAdded;
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
            else
            {
                if (EdgesInto.Count != 0)
                {
                    for (int i = 0; i < EdgesInto.Count; i++)
                    {
                        EdgesInto[i].TrafficRule.SetBlock(false);
                    }
                }
                FallBackTrafficRule.SetBlock(false);
            }
        }

        private void UpdateStopSignRule()
        {
            if (EdgesInto.Count != 0)
            {
                //allow one incoming to unblock at a time every 2 seconds
                int every2Seconds = (int)(Sim.Sim.GetSimTime() * 0.5f);
                int edgeToUnblock = every2Seconds % (EdgesInto.Count);
                if (edgeToUnblock > 0)
                {
                    bool value = false;
                }
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
                int every2Seconds = (int)(Sim.Sim.GetSimTime() * 0.5f);
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
    }
}