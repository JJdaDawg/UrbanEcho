using Box2dNet;
using Box2dNet.Interop;
using ExCSS;
using Mapsui.Layers;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using UrbanEcho.Events.UI;
using UrbanEcho.Graph;
using UrbanEcho.Helpers;
using UrbanEcho.Models;
using UrbanEcho.Physics;
using Point = NetTopologySuite.Geometries.Point;

namespace UrbanEcho.Sim
{
    public enum VehicleStates
    {
        Stopped = 0,
        Accelerating = 1,
        Decelerating = 2,
        AtTargetSpeed = 3,
        SlowDownForTurn = 4
    }

    public class Vehicle
    {
        public b2CastResultFcn? rayCastDelegate;

        private b2ShapeId intersectionShapeLastAt;

        public RoadIntersection? intersectionLastAt;

        private float whenToStopWaiting = 0;

        public Vector2 Pos;

        private Rectangle vehicleRectImage = new Rectangle(0, 0, 48, 24);

        private VehicleBody? body;

        private Vector2 initialStartPos = Vector2.Zero;
        private Vector2 startPos = Vector2.Zero;
        private Vector2 endPos = Vector2.Zero;

        private Vector2 nextPointOnPath = Vector2.Zero;
        private bool indexingForwardThroughLineString = true;

        private float distanceThresholdReachedTarget = 7.0f;

        private b2QueryFilter queryFilter = B2Api.b2DefaultQueryFilter();

        private Ray ray = new Ray(Vector2.Zero, Vector2.Zero);
        private float rayDistance = Helper.DoMapCorrection(10.0f);
        private b2Rot currentAngle;

        private bool waitingOnIntersection = false;
        private bool isWaiting = false;

        private float targetSpeed = 0;
        private float speedLimit = Helper.DoMapCorrection(50);
        private VehicleSettings settings;

        private float angleThresholdToDecelerate = Helper.Deg2Rad(45.0f);//How many degrees off target angle before decelerate
        private bool angleAboveThreshold = false;
        private bool vehicleInFront = false;
        private float metersFromCarInFront = 0;

        private float kmh = 0;

        private VehicleStates state = VehicleStates.Stopped;

        private PointFeature? feature;//The feature this vehicle is connected to

        private float angleToDest = 0;

        private RoadNode? nodeFrom;
        private RoadNode? nodeTo;

        private RoadEdge currentRoadEdge;

        public bool IsCreated = false;

        private int indexLineString;

        private int updateGroup = 0;

        private LineString? lineString;

        private List<int>? path;
        private int pathSegmentIndex = 0;
        private RoadGraph? graph;

        public int? NodeFromId => nodeFrom?.Id;
        public int? NodeToId => nodeTo?.Id;

        public bool PathSet = false;

        private float vehicleInFrontThresholdWaitTime = 30.0f;

        private float vehicleInFrontStartTime = 0;
        private float vehicleInFrontElaspedTime = 0;

        private int vehicleInFrontCount = 0;

        public Vehicle(PointFeature feature, RoadNode roadNodeFrom, RoadNode roadNodeTo, RoadEdge currentRoadEdge, string carType, int updateGroup)
        {
            settings = new VehicleSettings(carType);
            nodeFrom = roadNodeFrom;
            nodeTo = roadNodeTo;
            this.currentRoadEdge = currentRoadEdge;

            if (currentRoadEdge == null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Failed Adding Car with From Node{nodeFrom.X:F2},{nodeFrom.Y:F2} and To Node {nodeTo.X:F2}, {nodeTo.Y:F2} Road Edge passed was null"));
                return;
            }

            if (!settings.IsValid())
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Failed Adding Car that had {carType} as type"));
                return;
            }

            this.feature = feature;
            this.updateGroup = updateGroup;

            bool foundStartAndEnd = false;
            if (currentRoadEdge.Feature is GeometryFeature theRoad)
            {
                indexingForwardThroughLineString = currentRoadEdge.IsFromStartOfLineString;
                if (theRoad is GeometryFeature g)
                {
                    if (g.Geometry is LineString lineString)
                    {
                        this.lineString = lineString;
                        Point fromPoint = Helper.MakePrecisePoint(new Point(nodeFrom.X, nodeFrom.Y), lineString.PrecisionModel);
                        if (lineString.StartPoint.EqualsTopologically(fromPoint))
                        {
                            foundStartAndEnd = true;
                            //   EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Created Car with From Node{nodeFrom.X:F2},{nodeFrom.Y:F2} and To Node {nodeTo.X:F2}, {nodeTo.Y:F2} and road with Start {lineString.StartPoint:F2} and End {lineString.EndPoint:F2}"));
                        }

                        if (lineString.EndPoint.EqualsTopologically(fromPoint))
                        {
                            foundStartAndEnd = true;
                            // EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Created Car with From Node{nodeFrom.X:F2},{nodeFrom.Y:F2} and To Node {nodeTo.X:F2}, {nodeTo.Y:F2} and road with Start {lineString.StartPoint:F2} and End {lineString.EndPoint:F2}"));
                        }

                        if (foundStartAndEnd == false)
                        {
                            Point toPoint = Helper.MakePrecisePoint(new Point(nodeTo.X, nodeTo.Y), lineString.PrecisionModel);
                            if (lineString.StartPoint.EqualsTopologically(toPoint))
                            {
                                foundStartAndEnd = true;
                                //   EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Created Car with From Node{nodeFrom.X:F2},{nodeFrom.Y:F2} and To Node {nodeTo.X:F2}, {nodeTo.Y:F2} and road with Start {lineString.StartPoint:F2} and End {lineString.EndPoint:F2}"));
                            }

                            if (lineString.EndPoint.EqualsTopologically(toPoint))
                            {
                                foundStartAndEnd = true;
                                // EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Created Car with From Node{nodeFrom.X:F2},{nodeFrom.Y:F2} and To Node {nodeTo.X:F2}, {nodeTo.Y:F2} and road with Start {lineString.StartPoint:F2} and End {lineString.EndPoint:F2}"));
                            }

                            if (foundStartAndEnd == false)
                            {
                                //EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Failed Adding Car with From Node{nodeFrom.X:F2},{nodeFrom.Y:F2} and To Node {nodeTo.X:F2}, {nodeTo.Y:F2} and road with Start {lineString.StartPoint:F2} and End {lineString.EndPoint:F2}"));
                                IsCreated = false;
                            }
                        }
                        int startingIndex = 0;
                        if (indexingForwardThroughLineString)
                        {
                            indexLineString = 0;

                            if (indexLineString + 1 < lineString.Count)
                            {
                                indexLineString++;
                            }
                        }
                        else
                        {
                            indexLineString = lineString.Count - 1;
                            startingIndex = lineString.Count - 1;
                            if (indexLineString - 1 >= 0 && lineString.Count >= 1)
                            {
                                indexLineString--;
                            }
                        }

                        startPos = Helper.Convert2Box2dWorldPosition(lineString.Coordinates[startingIndex].X, lineString.Coordinates[startingIndex].Y);
                        endPos = Helper.Convert2Box2dWorldPosition(lineString.Coordinates[indexLineString].X, lineString.Coordinates[indexLineString].Y);

                        Vector2 directionNormalized = Vector2.Normalize(new Vector2(endPos.X - startPos.X, endPos.Y - startPos.Y));

                        angleToDest = MathF.Atan2(directionNormalized.Y, directionNormalized.X);
                        if (float.IsNaN(angleToDest))
                        {
                            angleToDest = 0;
                        }
                        //move start and end so car is using
                        //right hand lane
                        Vector2 laneOffset = new Vector2(MathF.Cos(angleToDest + Helper.Deg2Rad(-90.0f - 45.0f)) * Helper.DefaultLaneWidth, +MathF.Sin(angleToDest + Helper.Deg2Rad(-90.0f - 45.0f)) * Helper.DefaultLaneWidth);

                        startPos = new Vector2(startPos.X + laneOffset.X, startPos.Y + laneOffset.Y);
                        initialStartPos = startPos;
                        endPos = new Vector2(endPos.X + laneOffset.X, endPos.Y + laneOffset.Y);

                        FRect rect = new FRect(startPos.X - settings.GetLength() / 2, startPos.Y - settings.GetWidth() / 2, settings.GetLength(), settings.GetWidth());

                        rayCastDelegate = RayCastCallback;

                        queryFilter.categoryBits = 0xFFFF;
                        queryFilter.maskBits = 0xFFFF;

                        body = new VehicleBody(rect);

                        b2Rot rot = b2Rot.FromAngle(angleToDest);
                        B2Api.b2Body_SetTransform(body.BodyId, startPos, rot);

                        IsCreated = true;
                    }
                }
            }
        }

        public void SetPath(RoadGraph graph, List<int> path)
        {
            this.graph = graph;
            this.path = path;
            this.pathSegmentIndex = 0;
        }

        private bool AdvanceToNextRoad()
        {
            if (path == null || graph == null || body == null)
                return false;

            if (pathSegmentIndex >= path.Count - 1)
            {
                int currentNodeId = path[path.Count - 1];
                var nodes = graph.Nodes.Keys.ToList();
                if (nodes.Count < 2)
                    return false;

                var pathfinder = new AStarPathfinder(graph);
                int goalNode;
                do
                {
                    goalNode = nodes[Random.Shared.Next(nodes.Count)];
                } while (goalNode == currentNodeId);

                var newPath = pathfinder.FindPath(currentNodeId, goalNode).ToList();
                if (newPath.Count < 2)
                    return false;

                path = newPath;
                pathSegmentIndex = 0;
            }

            int fromId = path[pathSegmentIndex];
            int toId = path[pathSegmentIndex + 1];

            RoadEdge? nextEdge = null;
            foreach (var edge in graph.GetOutgoingEdges(fromId))
            {
                if (edge.To == toId)
                {
                    nextEdge = edge;
                    break;
                }
            }

            if (nextEdge == null)
                return false;

            if (graph.Nodes.TryGetValue(fromId, out var fromNode))
                nodeFrom = fromNode;
            if (graph.Nodes.TryGetValue(toId, out var toNode))
                nodeTo = toNode;

            if (nextEdge.Feature is GeometryFeature theRoad && theRoad.Geometry is LineString newLineString)
            {
                currentRoadEdge = nextEdge;
                lineString = newLineString;
                indexingForwardThroughLineString = nextEdge.IsFromStartOfLineString;

                int startingIndex;
                if (indexingForwardThroughLineString)
                {
                    startingIndex = 0;
                    indexLineString = Math.Min(1, lineString.Count - 1);
                }
                else
                {
                    startingIndex = lineString.Count - 1;
                    indexLineString = Math.Max(lineString.Count - 2, 0);
                }

                Vector2 startPosRoad = Helper.Convert2Box2dWorldPosition(lineString.Coordinates[startingIndex].X, lineString.Coordinates[startingIndex].Y);
                Vector2 endPosRoad = Helper.Convert2Box2dWorldPosition(lineString.Coordinates[indexLineString].X, lineString.Coordinates[indexLineString].Y);

                Vector2 roadDirectionNormalized = Vector2.Normalize(new Vector2(endPosRoad.X - startPosRoad.X, endPosRoad.Y - startPosRoad.Y));
                float angleForLaneOffset = MathF.Atan2(roadDirectionNormalized.Y, roadDirectionNormalized.X);
                if (float.IsNaN(angleForLaneOffset))
                    angleForLaneOffset = 0;

                Vector2 laneOffset = new Vector2(
                    MathF.Cos(angleForLaneOffset + Helper.Deg2Rad(-90.0f - 45.0f)) * Helper.DefaultLaneWidth,
                    MathF.Sin(angleForLaneOffset + Helper.Deg2Rad(-90.0f - 45.0f)) * Helper.DefaultLaneWidth);

                startPos = new Vector2(startPosRoad.X + laneOffset.X, startPosRoad.Y + laneOffset.Y);
                initialStartPos = startPosRoad;
                endPos = new Vector2(endPosRoad.X + laneOffset.X, endPosRoad.Y + laneOffset.Y);

                pathSegmentIndex++;
                return true;
            }

            return false;
        }

        public void SetIntersectionLastAt(b2ShapeId shapeId)
        {
            if (intersectionLastAt == null)
            {
                intersectionShapeLastAt = shapeId;
                IntPtr intPtr = B2Api.b2Shape_GetUserData(shapeId);
                intersectionLastAt = NativeHandle.GetObject<RoadIntersection>(intPtr);
                whenToStopWaiting = Sim.SimTime + intersectionLastAt.WaitTime;
                isWaiting = true;
            }
            else
            {
                //If it isn't same shape again get the shapes userdata
                if (intersectionShapeLastAt != shapeId)
                {
                    intersectionShapeLastAt = shapeId;
                    IntPtr intPtr = B2Api.b2Shape_GetUserData(shapeId);
                    intersectionLastAt = NativeHandle.GetObject<RoadIntersection>(intPtr);
                    whenToStopWaiting = Sim.SimTime + intersectionLastAt.WaitTime;
                    isWaiting = true;
                }
            }
        }

        public void SetVehicleInFrontCount(float howFar)
        {
            metersFromCarInFront = rayDistance * howFar;

            if (!vehicleInFront)
            {
                vehicleInFrontStartTime = Sim.SimTime;
            }
            vehicleInFrontCount++;
        }

        public void ResetVehicleInFrontCount()
        {
            vehicleInFrontCount = 0;
        }

        public void Update()
        {
            Pos = B2Api.b2Body_GetPosition(body.BodyId);

            float distanceToTarget = Vector2.Distance(Pos, endPos);

            if (distanceToTarget <= distanceThresholdReachedTarget)
            {
                currentAngle = B2Api.b2Body_GetRotation(body.BodyId);
                float currentFloatAngle = currentAngle.GetAngle();

                UpdateEndPos(currentFloatAngle);
            }
            try
            {
                feature.Point.X = (double)Pos.X + World.Offset.X;
                feature.Point.Y = (double)Pos.Y + World.Offset.Y;
            }
            catch
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Feature that is assigned to the vehicle is null"));
            }
            if (Sim.GroupToUpdate == updateGroup)
            {
                currentAngle = B2Api.b2Body_GetRotation(body.BodyId);
                float currentFloatAngle = currentAngle.GetAngle();

                SetAngle(currentFloatAngle);
                try
                {
                    feature["Angle"] = Helper.Rad2Deg(currentFloatAngle);
                }
                catch (Exception ex)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Vehicle missing angle feature + {ex.ToString()}"));
                }

                if (isWaiting)
                {
                    if (Sim.SimTime < whenToStopWaiting)
                    {
                        waitingOnIntersection = true;
                    }
                    else
                    {
                        waitingOnIntersection = false;
                        isWaiting = false;
                    }
                }
                else
                {
                    waitingOnIntersection = false;
                }

                if (waitingOnIntersection == true)
                {
                    targetSpeed = 0;
                }
                else
                {
                    targetSpeed = speedLimit;
                }

                float updateToSpeed = kmh;

                if (state == VehicleStates.Accelerating)
                {
                    updateToSpeed = Math.Clamp(updateToSpeed + settings.GetAcceleration(), 0, speedLimit);
                }
                if (state == VehicleStates.Decelerating)
                {
                    updateToSpeed = Math.Clamp(updateToSpeed - settings.GetDeceleration(), 0, speedLimit);
                }

                if (state == VehicleStates.SlowDownForTurn)
                {
                    updateToSpeed = Math.Clamp(updateToSpeed - settings.GetDeceleration() * settings.GetSlowDownfactor(), 0, speedLimit);
                }

                if (updateToSpeed > 0)
                {
                    float speedToUseMs = Helper.Kmh2Ms(updateToSpeed);
                    Vector2 velocityToSetMs = new Vector2(currentAngle.c * speedToUseMs, currentAngle.s * speedToUseMs);

                    B2Api.b2Body_SetLinearVelocity(body.BodyId, velocityToSetMs);
                }
                else
                {
                    B2Api.b2Body_SetLinearVelocity(body.BodyId, Vector2.Zero);
                }

                kmh = Helper.MS2Kmh(Vector2.Dot(B2Api.b2Body_GetLinearVelocity(body.BodyId), new Vector2(currentAngle.c, currentAngle.s)));
                if (float.IsNaN(kmh))
                {
                    kmh = 0;
                }
                if (vehicleInFront == false)
                {
                    if (kmh <= 0 && targetSpeed == 0)
                    {
                        state = VehicleStates.Stopped;
                    }
                    else
                    {
                        if (kmh >= targetSpeed && targetSpeed != 0)
                        {
                            state = VehicleStates.AtTargetSpeed;
                        }
                        else
                        {
                            if (kmh < targetSpeed)
                            {
                                state = VehicleStates.Accelerating;
                            }
                            else
                            {
                                state = VehicleStates.Decelerating;
                            }
                        }

                        if (angleAboveThreshold && (!(state == VehicleStates.Decelerating || state == VehicleStates.Stopped)))
                        {
                            state = VehicleStates.SlowDownForTurn;
                        }
                    }
                }
                else
                {
                    if (kmh <= 0 && targetSpeed == 0)
                    {
                        state = VehicleStates.Stopped;
                    }
                    else
                    {
                        state = VehicleStates.Decelerating;
                    }
                }

                ray = new Ray(Pos, new Vector2(currentAngle.c * rayDistance, currentAngle.s * rayDistance));
                ResetVehicleInFrontCount();//Has to be before the raycast else will always be false
                B2Api.b2World_CastRay(World.WorldId, ray.Start, ray.Translation, queryFilter, rayCastDelegate, 1);

                if (vehicleInFrontCount > 0)
                {
                    vehicleInFront = true;
                    vehicleInFrontElaspedTime = Sim.SimTime - vehicleInFrontStartTime;

                    if (vehicleInFrontElaspedTime > vehicleInFrontThresholdWaitTime)
                    {
                        ResetVehicleToNewPos();
                    }
                }
                else
                {
                    vehicleInFront = false;
                    vehicleInFrontElaspedTime = 0;
                }
            }
        }

        public void ResetVehicleToNewPos()
        {
            int goalNode;
            int startNode;
            do
            {
                goalNode = Sim.nodes[Random.Shared.Next(Sim.nodes.Count)];
                startNode = Sim.nodes[Random.Shared.Next(Sim.nodes.Count)];
            } while (goalNode == startNode && Sim.nodes.Count > 1);

            var path = Sim.pathfinder.FindPath(startNode, goalNode).ToList();

            if (path.Count > 1)
            {
                SetPath(graph, path);
                PathSet = true;

                Vector2 startingPos = Helpers.Helper.Convert2Box2dWorldPosition(Sim.RoadGraph.Nodes[path[0]].X, Sim.RoadGraph.Nodes[path[0]].Y);

                b2Rot rot = b2Rot.FromAngle(0);
                B2Api.b2Body_SetTransform(body.BodyId, startingPos, rot);
                Pos = B2Api.b2Body_GetPosition(body.BodyId);
                B2Api.b2Body_SetAngularVelocity(body.BodyId, 0.0f);
                AdvanceToNextRoad();
            }
            else
            {
                //Else try and reset it to a new position again
                ResetVehicleToNewPos();
            }
        }

        private void UpdateEndPos(float currentFloatAngle)
        {
            bool startingOver = false;

            int currentLineStringIndex = indexLineString;

            if (indexingForwardThroughLineString)
            {
                if (indexLineString + 1 < lineString.Count)
                {
                    indexLineString++;
                }
                else
                {
                    if (AdvanceToNextRoad())
                    {
                        return;
                    }

                    b2Rot rot = b2Rot.FromAngle(angleToDest);
                    B2Api.b2Body_SetTransform(body.BodyId, initialStartPos, rot);
                    Pos = B2Api.b2Body_GetPosition(body.BodyId);
                    currentLineStringIndex = 0;
                    if (lineString.Count >= 2)
                    {
                        indexLineString = 1;
                    }
                    startPos = initialStartPos;
                    B2Api.b2Body_SetAngularVelocity(body.BodyId, 0.0f);
                    startingOver = true;
                }
            }
            else
            {
                if (indexLineString - 1 >= 0 && lineString.Count >= 1)
                {
                    indexLineString--;
                }
                else
                {
                    if (AdvanceToNextRoad())
                    {
                        return;
                    }

                    b2Rot rot = b2Rot.FromAngle(angleToDest);
                    B2Api.b2Body_SetTransform(body.BodyId, initialStartPos, rot);
                    Pos = B2Api.b2Body_GetPosition(body.BodyId);
                    currentLineStringIndex = lineString.Count - 1;
                    if (lineString.Count >= 2)
                    {
                        indexLineString = lineString.Count - 2;
                        startPos = initialStartPos;
                    }
                    B2Api.b2Body_SetAngularVelocity(body.BodyId, 0.0f);
                    startingOver = true;
                }
            }

            Vector2 startPosRoad = Helper.Convert2Box2dWorldPosition(lineString.Coordinates[currentLineStringIndex].X, lineString.Coordinates[currentLineStringIndex].Y);
            Vector2 endPosRoad = Helper.Convert2Box2dWorldPosition(lineString.Coordinates[indexLineString].X, lineString.Coordinates[indexLineString].Y);

            Vector2 roadDirectionNormalized = Vector2.Normalize(new Vector2(endPosRoad.X - startPosRoad.X, endPosRoad.Y - startPosRoad.Y));
            float angleForLaneOffset = MathF.Atan2(roadDirectionNormalized.Y, roadDirectionNormalized.X);
            if (float.IsNaN(angleForLaneOffset))
                angleForLaneOffset = 0;

            Vector2 laneOffset = new Vector2(
                MathF.Cos(angleForLaneOffset + Helper.Deg2Rad(-90.0f - 45.0f)) * Helper.DefaultLaneWidth,
                MathF.Sin(angleForLaneOffset + Helper.Deg2Rad(-90.0f - 45.0f)) * Helper.DefaultLaneWidth);

            startPos = new Vector2(startPosRoad.X + laneOffset.X, startPosRoad.Y + laneOffset.Y);
            initialStartPos = startPosRoad;
            endPos = new Vector2(endPosRoad.X + laneOffset.X, endPosRoad.Y + laneOffset.Y);
        }

        private void SetAngle(float currentAngle)
        {
            float targetAngle = 0;

            Vector2 roadDirection = Vector2.Normalize(new Vector2(endPos.X - startPos.X, endPos.Y - startPos.Y));
            Vector2 closestPointToLine = findNearestPointOnLine(startPos, endPos, Pos);

            Vector2 targetPointToAimTowards = new Vector2(closestPointToLine.X + roadDirection.X * settings.GetLookAheadValueForSteerTowardsLane(), closestPointToLine.Y + roadDirection.Y * settings.GetLookAheadValueForSteerTowardsLane());
            if (Vector2.Distance(endPos, startPos) <= Vector2.Distance(targetPointToAimTowards, startPos))
            {
                targetPointToAimTowards = endPos;
            }

            Vector2 directionNormalized = Vector2.Normalize(new Vector2(targetPointToAimTowards.X - Pos.X, targetPointToAimTowards.Y - Pos.Y));

            targetAngle = MathF.Atan2(directionNormalized.Y, directionNormalized.X);

            //https://stackoverflow.com/questions/51905268/how-to-find-closest-point-on-line
            //https://stackoverflow.com/questions/22668659/calculate-on-which-side-of-a-line-a-point-is
            //https://stackoverflow.com/questions/1560492/how-to-tell-whether-a-point-is-to-the-right-or-left-side-of-a-line/1560510#1560510

            Vector2 findNearestPointOnLine(Vector2 origin, Vector2 end, Vector2 point)
            {
                //Get heading
                Vector2 heading = (end - origin);
                float magnitudeMax = heading.Length();
                heading = Vector2.Normalize(heading);

                //Do projection from the point but clamp it
                Vector2 lhs = point - origin;
                float dotP = Vector2.Dot(lhs, heading);
                dotP = Math.Clamp(dotP, 0f, magnitudeMax);
                return origin + heading * dotP;
            }

            float angle = targetAngle - currentAngle;

            if (float.IsNaN(angle))
            {
                angle = 0;
            }
            else
            {
                //https://phaser.io/tutorials/box2d-tutorials/rotate-to-angle#:~:text=This%20topic%20covers%20rotating%20a%20body%20to,directly%20setting%20the%20angle%20or%20using%20torque/
                angle = (float)(((angle + Math.PI) % (2 * Math.PI) + 2 * Math.PI) % (2 * Math.PI) - Math.PI);

                B2Api.b2Body_SetAngularVelocity(body.BodyId, angle * settings.GetTurnSpeed());

                if (Math.Abs(angle) >= angleThresholdToDecelerate)
                {
                    angleAboveThreshold = true;
                }
                else
                {
                    angleAboveThreshold = false;
                }
            }
        }

        private float RayCastCallback(b2ShapeId shapeId, Vector2 point, Vector2 normal, float fraction, nint context)
        {
            b2Filter filter = B2Api.b2Shape_GetFilter(shapeId);
            if (filter.categoryBits == (ulong)ShapeCategories.Intersection && fraction != 0)
            {
                SetIntersectionLastAt(shapeId);
            }
            if (filter.categoryBits == (ulong)ShapeCategories.Vehicle && fraction != 0)
            {
                SetVehicleInFrontCount(fraction);
            }

            return -1.0f;//-1.0f means keep doing raycasting if hit any other object
                         //see above declaration public b2CastResultFcn? rayCastDelegate; for details
        }
    }
}