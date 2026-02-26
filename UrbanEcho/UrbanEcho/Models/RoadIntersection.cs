using Mapsui;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UrbanEcho.Physics;

namespace UrbanEcho.Sim
{
    public class RoadIntersection
    {
        public List<RoadEdge> EdgesInto;
        public List<RoadEdge> EdgesOut;

        public Vector2 Center;

        public IntersectionBody? Body;

        public string Name = "test123";

        public float WaitTime = 5.0f;

        public IFeature Feature;

        private bool isBodySet = false;

        public RoadIntersection(string name, float waitTime, IFeature feature, RoadGraph graph)
        {
            this.Name = name;
            Feature = feature;
            EdgesInto = new List<RoadEdge>();
            EdgesOut = new List<RoadEdge>();
            WaitTime = waitTime;
            bool isCenterSet = false;
            if (feature is GeometryFeature intersectGF)
            {
                if (intersectGF.Geometry is Point p)
                {
                    Center = Helpers.Helper.Convert2Box2dWorldPosition(p);
                    isCenterSet = true;
                }
            }

            if (graph is null)
            {
                return;
            }

            if (isCenterSet)
            {
                List<(Vector2 direction, float width)> connectionsForBody = setConnections(graph);

                Body = new IntersectionBody(this, connectionsForBody);
                isBodySet = true;
            }
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
                                        EdgesInto.Add(roadEdge);
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
    }
}