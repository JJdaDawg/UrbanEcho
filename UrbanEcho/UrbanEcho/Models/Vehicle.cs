using Box2dNet;
using Box2dNet.Interop;
using ExCSS;
using Mapsui.Layers;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UrbanEcho.Events.UI;
using UrbanEcho.Graph;
using UrbanEcho.Helpers;
using UrbanEcho.Models;
using UrbanEcho.Models.UI;
using UrbanEcho.Physics;

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

    public class Vehicle : IBodyParent
    {
        public VehicleUI VehicleUI
        {
            get; private set;
        } = new VehicleUI();

        public bool IsForceStopped = false;

        //public b2CastResultFcn? rayCastDelegate;
        public b2OverlapResultFcn overlapDelegateVehicle;

        public b2OverlapResultFcn overlapDelegateIntersection;

        private b2ShapeId intersectionShapeLastAt;

        public RoadIntersection? intersectionLastAt;

        private float whenToStopWaiting = 0;
        private float minimumStopWaiting = 3.0f;
        private TrafficRule currentTrafficRule;

        public Vector2 Pos;

        //private Rectangle vehicleRectImage = new Rectangle(0, 0, 48, 24);

        public VehicleBody? Body;

        private Vector2 startPos = Vector2.Zero;

        private Vector2 endPos = Vector2.Zero;

        private float distanceThresholdReachedTarget = 10.0f;

        private b2QueryFilter queryFilter = B2Api.b2DefaultQueryFilter();

        private Ray ray = new Ray(Vector2.Zero, Vector2.Zero);
        private float rayDistance;
        private b2Rot currentAngle;

        private float targetSpeed = 0;

        private VehicleSettings settings;

        private float angleThresholdToDecelerate = Helper.Deg2Rad(45.0f);//How many degrees off target angle before decelerate
        private bool angleAboveThreshold = false;

        private PointFeature? feature;//The feature this vehicle is connected to

        private RoadEdge currentRoadEdge;

        public bool IsCreated = false;

        private int prevIndexLineString;
        private int indexLineString;

        private int updateGroup = 0;

        private List<int>? path;
        private List<PathStep>? pathSteps;

        private int pathSegmentIndex = 0;
        private RoadGraph graph;

        private float vehicleInFrontThresholdWaitTime = 120.0f;//Set long so we know it isn't just a light

        private float vehicleInFrontStartTime = 0;
        private float vehicleInFrontElaspedTime = 0;

        private int vehicleInFrontCount = 0;

        private bool insideAnotherVehicle = false;

        private int intersectionInFrontCount = 0;
        private bool hasClearedIntersection = true;
        private bool intersectionOccupied = false;

        private int lanePicked = 1;
        private float calculatedOffsetForLane = 0.5f;

        private float lastTimeCheckedOverlap = 0;
        private float overlapTestFrequency = 5.0f;//Check for overlap every five seconds
        private float stoppedThresholdWaitTime = 120.0f;//If vehicle hasn't moved for this long respawn it
        private float stoppedStartTime = 0;
        private float stoppedElaspedTime = 0;
        private bool startedStoppedTimer = false;

        private TurnDirection oldTurn = TurnDirection.Straight;
        private bool didFirstUpdate = false;//used to hide vehicles while loading up paths

        private float kmh = 0;

        public float Kmh
        {
            get
            {
                return kmh;
            }
            set
            {
                VehicleUI.Kmh = value;
                kmh = value;
            }
        }

        private float speedLimit = 50;

        public float SpeedLimit
        {
            get
            {
                return speedLimit;
            }
            set
            {
                VehicleUI.SpeedLimit = value;
                speedLimit = value;
            }
        }

        private VehicleStates state = VehicleStates.Stopped;

        public VehicleStates State
        {
            get
            {
                return state;
            }
            set
            {
                VehicleUI.State = value;
                state = value;
            }
        }

        private bool isWaiting = false;

        public bool IsWaiting
        {
            get
            {
                return isWaiting;
            }
            set
            {
                VehicleUI.IsWaiting = value;
                isWaiting = value;
            }
        }

        private bool waitingOnIntersection = false;

        public bool WaitingOnIntersection
        {
            get
            {
                return waitingOnIntersection;
            }
            set
            {
                VehicleUI.WaitingOnIntersection = value;
                waitingOnIntersection = value;
            }
        }

        private bool vehicleInFront = false;

        public bool VehicleInFront
        {
            get
            {
                return vehicleInFront;
            }
            set
            {
                VehicleUI.VehicleInFront = value;
                vehicleInFront = value;
            }
        }

        private float metersFromCarInFront = 0;

        public float MetersFromCarInFront
        {
            get
            {
                return metersFromCarInFront;
            }
            set
            {
                VehicleUI.MetersFromCarInFront = value;
                metersFromCarInFront = value;
            }
        }

        private string roadType = "road";

        public string RoadName
        {
            get
            {
                return currentRoadEdge.Metadata.RoadName;
            }
            set
            {
                roadType = value;
                VehicleUI.RoadName = value;
            }
        }

        public Vehicle(PointFeature feature, RoadEdge currentRoadEdge, string carType, int updateGroup, RoadGraph roadGraph)
        {
            graph = roadGraph;
            VehicleUI.VehicleType = Helper.TryGetFeatureKVPToString(feature, "VehicleType", "");
            VehicleUI.Id = Helper.TryGetFeatureKVPToInt(feature, "VehicleNumber", 0);

            settings = new VehicleSettings(carType);
            currentTrafficRule = TrafficRule.SetDefaultTrafficRule();

            this.currentRoadEdge = SetCurrentRoadEdge(currentRoadEdge);

            if (!settings.IsValid())
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Failed Adding Car that had {carType} as type"));
                return;
            }

            this.feature = feature;
            this.updateGroup = updateGroup;

            FRect rect = new FRect(startPos.X - (settings.GetLength()) / 2, startPos.Y - (settings.GetWidth()) / 2, settings.GetLength(), settings.GetWidth());

            overlapDelegateVehicle = OverlapCallbackVehicle;
            overlapDelegateIntersection = OverlapCallbackIntersection;

            queryFilter.categoryBits = 0xFFFF;
            queryFilter.maskBits = 0xFFFF;

            Body = new VehicleBody(this, rect);

            IsCreated = true;
        }

        private void AdvanceToNextRoad()
        {
            if (path == null || pathSteps == null || graph == null || Body == null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Could not run advance to next road"));
                return;
            }

            if (pathSegmentIndex >= pathSteps.Count)
            {
                int currentNodeId = path[path.Count - 1];
                setNewPath(currentNodeId);
            }

            stepThroughPath();
        }

        private void setNewPath(int currentNodeId)
        {
            pathSegmentIndex = 0;
            if (graph == null || Body == null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Could not run advance to next road"));
                return;
            }

            var nodes = graph.Nodes.Keys.ToList();
            if (nodes.Count < 2)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Nodes in graph were less than 2"));
                ResetVehicleToNewPos();
                return;
            }
            var pathfinder = new AStarPathfinder(graph, Sim.NodePenalties);
            int goalNode = TrafficVolumeLoader.PickWeightedDestination(graph, currentNodeId);

            var newPathEdges = pathfinder.FindPathEdges(currentNodeId, goalNode);
            if (newPathEdges.Count < 1)
            {
                ResetVehicleToNewPos();
                return;
            }
            pathSteps = PathStepBuilder.Build(newPathEdges, graph);
            path = new List<int> { newPathEdges[0].From };
            for (int i = 0; i < newPathEdges.Count; i++)
                path.Add(newPathEdges[i].To);
            // Old: var newPath = pathfinder.FindPath(currentNodeId, goalNode).ToList();
            // Old: if (newPath.Count < 2) { ResetVehicleToNewPos(); return; }
            // Old: path = newPath;
        }

        private void stepThroughPath()
        {
            if (pathSteps == null || pathSegmentIndex >= pathSteps.Count)
            {
                ResetVehicleToNewPos();
                return;
            }

            PathStep step = pathSteps[pathSegmentIndex];

            if (step.Edge.Feature is GeometryFeature theRoad && theRoad.Geometry is LineString newLineString)
            {
                currentRoadEdge = SetCurrentRoadEdge(step.Edge, step.Turn);

                StepThroughLineString(true);

                pathSegmentIndex++;
            }
            else
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Failed to advance to next road"));
                ResetVehicleToNewPos();
                return;
            }
        }

        // Old stepThroughPath that searched by node IDs (could pick wrong parallel edge):
        // private void stepThroughPath(List<int> path)
        // {
        //     int fromId = path[pathSegmentIndex];
        //     int toId = path[pathSegmentIndex + 1];
        //     if (graph is null) { return; }
        //     RoadEdge? nextEdge = null;
        //     foreach (var edge in graph.GetOutgoingEdges(fromId))
        //     {
        //         if (edge.To == toId) { nextEdge = edge; break; }
        //     }
        //     if (nextEdge == null) { ResetVehicleToNewPos(); return; }
        //     if (nextEdge.Feature is GeometryFeature theRoad && theRoad.Geometry is LineString newLineString)
        //     {
        //         currentRoadEdge = SetCurrentRoadEdge(nextEdge);
        //         StepThroughLineString(true);
        //         pathSegmentIndex++;
        //     }
        // }

        public void SetIntersectionLastAt(b2ShapeId shapeId)
        {
            //If it isn't same shape again get the shapes userdata
            if (intersectionShapeLastAt != shapeId && hasClearedIntersection == true)
            {
                hasClearedIntersection = false;
                intersectionShapeLastAt = shapeId;
                IntPtr intPtr = B2Api.b2Shape_GetUserData(shapeId);
                intersectionLastAt = NativeHandle.GetObject<RoadIntersection>(intPtr);

                EdgeTrafficRule? edgeTrafficRule = intersectionLastAt.EdgesInto.Find(e => e.RoadEdge == currentRoadEdge);
                if (edgeTrafficRule != null)
                {
                    currentTrafficRule = edgeTrafficRule.TrafficRule;
                }
                else
                {
                    currentTrafficRule = intersectionLastAt.FallBackTrafficRule;
                }

                if (currentTrafficRule.IsBlockingTraffic())
                {
                    IsWaiting = true;
                }

                if (currentTrafficRule.IsStopSign() && !currentTrafficRule.IsNeverBlockingTraffic())
                {
                    IsWaiting = true;
                }

                if (IsWaiting == true)
                {
                    whenToStopWaiting = Sim.GetSimTime() + minimumStopWaiting;
                }
            }

            if (intersectionShapeLastAt == shapeId)
            {
                intersectionInFrontCount++;
            }
        }

        public bool SetVehicleInFrontCount(b2ShapeId shapeId, float howFar)
        {
            bool counted = false;
            if (shapeId != Body.ShapeId)
            {
                IntPtr intPtr = B2Api.b2Shape_GetUserData(shapeId);

                Vehicle otherVehicle = NativeHandle.GetObject<Vehicle>(intPtr);

                if (IsCollidedVehicleSameEdgeOrIntersection(otherVehicle.currentRoadEdge))
                {
                    MetersFromCarInFront = rayDistance * howFar;

                    if (!VehicleInFront)
                    {
                        vehicleInFrontStartTime = Sim.GetSimTime();
                    }
                    vehicleInFrontCount++;
                    counted = true;

                    if (IsWaiting == true)
                    {
                        whenToStopWaiting = Sim.GetSimTime() + minimumStopWaiting;
                    }
                }
            }
            return counted;
        }

        public RoadEdge GetRoadEdge()
        {
            return currentRoadEdge;
        }

        public bool IsCollidedVehicleSameEdgeOrIntersection(RoadEdge otherVehicleEdge)
        {
            if (currentRoadEdge == otherVehicleEdge)
            {
                return true;
            }
            if (graph is not null)
            {
                IReadOnlyList<RoadEdge> otherOutGoingFrom = graph.GetOutgoingEdges(otherVehicleEdge.From);

                if (otherOutGoingFrom.Contains(currentRoadEdge))
                {
                    return true;
                }

                IReadOnlyList<RoadEdge> outGoingFrom = graph.GetOutgoingEdges(currentRoadEdge.From);

                if (outGoingFrom.Contains(otherVehicleEdge))
                {
                    return true;
                }

                IReadOnlyList<RoadEdge> otherIncomingTo = graph.GetIncomingEdges(otherVehicleEdge.To);

                if (otherIncomingTo.Contains(currentRoadEdge))
                {
                    return true;
                }

                IReadOnlyList<RoadEdge> inGoingTo = graph.GetIncomingEdges(currentRoadEdge.To);

                if (inGoingTo.Contains(otherVehicleEdge))
                {
                    return true;
                }
            }
            return false;
        }

        public void ResetVehicleInFrontCount()
        {
            vehicleInFrontCount = 0;
        }

        public void Update()
        {
            if (didFirstUpdate)//Only do the update after path initially set
            {
                Pos = B2Api.b2Body_GetPosition(Body.BodyId);

                if (IsForceStopped)
                {
                    B2Api.b2Body_SetLinearVelocity(Body.BodyId, Vector2.Zero);
                    State = VehicleStates.Stopped;
                    return;
                }

                Pos = B2Api.b2Body_GetPosition(Body.BodyId);

                float distanceToTarget = Vector2.Distance(Pos, endPos);

                if (distanceToTarget <= distanceThresholdReachedTarget)
                {
                    StepThroughLineString(false);
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

                currentAngle = B2Api.b2Body_GetRotation(Body.BodyId);
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

                if (IsWaiting)
                {
                    if (currentTrafficRule.IsBlockingTraffic())
                    {
                        WaitingOnIntersection = true;
                    }
                    else
                    {
                        if (Sim.GetSimTime() > whenToStopWaiting)
                        {
                            if (Sim.GroupToUpdate == updateGroup)
                            {
                                if (intersectionLastAt is not null)
                                {
                                    RoadIntersection intersection = intersectionLastAt;

                                    Vector2[] vertices = intersection.Body.GetShapeVertices();
                                    b2Rot zeroRotation = b2Rot.FromAngle(0);
                                    b2ShapeProxy b2ShapeProxy = B2Api.b2MakeOffsetProxy(vertices, vertices.Length, 0.0f, intersection.Center, zeroRotation);

                                    queryFilter.maskBits = (ulong)ShapeCategories.Vehicle;
                                    intersectionOccupied = false;
                                    B2Api.b2World_OverlapShape(World.WorldId, b2ShapeProxy, queryFilter, overlapDelegateIntersection, 1);

                                    if (!(intersectionOccupied))
                                    {
                                        WaitingOnIntersection = false;
                                        IsWaiting = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            WaitingOnIntersection = true;
                        }
                    }
                }
                else
                {
                    WaitingOnIntersection = false;
                }

                if (WaitingOnIntersection == true)
                {
                    targetSpeed = 0;
                }
                else
                {
                    targetSpeed = SpeedLimit;
                }

                float updateToSpeed = Kmh;

                if (VehicleInFront == false)
                {
                    if (Kmh <= 0.1f && targetSpeed <= 0.1f)
                    {
                        State = VehicleStates.Stopped;
                        if (!startedStoppedTimer)
                        {
                            startedStoppedTimer = true;
                            stoppedStartTime = Sim.GetSimTime();
                        }
                    }
                    else
                    {
                        if (Kmh >= targetSpeed && targetSpeed > 0.1f)
                        {
                            State = VehicleStates.AtTargetSpeed;
                        }
                        else
                        {
                            if (Kmh < targetSpeed)
                            {
                                State = VehicleStates.Accelerating;
                            }
                            else
                            {
                                State = VehicleStates.Decelerating;
                            }
                        }

                        if (angleAboveThreshold && (!(State == VehicleStates.Decelerating || State == VehicleStates.Stopped)))
                        {
                            State = VehicleStates.SlowDownForTurn;
                        }
                    }
                }
                else
                {
                    if (Kmh <= 0.1f)
                    {
                        State = VehicleStates.Stopped;

                        if (!startedStoppedTimer)
                        {
                            startedStoppedTimer = true;
                            stoppedStartTime = Sim.GetSimTime();
                        }
                    }
                    else
                    {
                        State = VehicleStates.Decelerating;
                    }
                }

                if (State == VehicleStates.Accelerating)
                {
                    updateToSpeed = Math.Clamp(updateToSpeed + settings.GetAcceleration(), 0.0f, SpeedLimit);
                }
                if (State == VehicleStates.AtTargetSpeed)
                {
                    updateToSpeed = Math.Clamp(updateToSpeed, 0.0f, SpeedLimit);
                }
                if (State == VehicleStates.Decelerating)
                {
                    updateToSpeed = Math.Clamp(updateToSpeed - settings.GetDeceleration(), 0.0f, SpeedLimit);
                }

                if (State == VehicleStates.Stopped)
                {
                    updateToSpeed = 0.0f;
                }

                if (State == VehicleStates.SlowDownForTurn)
                {
                    updateToSpeed = Math.Clamp(updateToSpeed - settings.GetDeceleration() * settings.GetSlowDownfactor(), 0, SpeedLimit);
                }

                float speedToUseMs = Helper.Kmh2Ms(updateToSpeed);
                Vector2 velocityToSetMs = new Vector2(currentAngle.c * speedToUseMs, currentAngle.s * speedToUseMs);
                if (State != VehicleStates.Stopped)
                {
                    B2Api.b2Body_SetLinearVelocity(Body.BodyId, velocityToSetMs);
                }
                else
                {
                    B2Api.b2Body_SetLinearVelocity(Body.BodyId, Vector2.Zero);
                }
                Kmh = Helper.MS2Kmh(Vector2.Dot(velocityToSetMs/*B2Api.b2Body_GetLinearVelocity(Body.BodyId)*/, new Vector2(currentAngle.c, currentAngle.s)));
                if (float.IsNaN(Kmh))
                {
                    Kmh = 0;
                }

                if (Sim.GroupToUpdate == updateGroup)
                {
                    Vector2[] vertices = Body.GetShapeVertices();
                    b2ShapeProxy b2ShapeProxy = B2Api.b2MakeOffsetProxy(vertices, vertices.Length, 0.0f, Pos, currentAngle);

                    queryFilter.maskBits = (ulong)ShapeCategories.Vehicle;
                    if (Sim.GetSimTime() > lastTimeCheckedOverlap + overlapTestFrequency)
                    {
                        B2Api.b2World_OverlapShape(World.WorldId, b2ShapeProxy, queryFilter, overlapDelegateVehicle, 1);
                        lastTimeCheckedOverlap = Sim.GetSimTime() + (float)Random.Shared.NextDouble();//add a 1 second random offset so not all vehicles
                                                                                                      //do this at same time
                    }
                    if (!(insideAnotherVehicle))
                    {
                        //Start raycast from front of car, so ray is not against self
                        Vector2 calcRayStart = new Vector2(Pos.X + (settings.GetLength() / 2.0f + 0.1f) * currentAngle.c,
                            Pos.Y + (settings.GetLength() / 2.0f + 0.1f) * currentAngle.s);
                        //Vector2 calcRayStart = new Vector2(Pos.X, Pos.Y);

                        b2Rot angleForRay = b2Rot.FromAngle(currentFloatAngle);
                        rayDistance = 15.0f;
                        ray = new Ray(calcRayStart, new Vector2(angleForRay.c * rayDistance, angleForRay.s * rayDistance));
                        ResetVehicleInFrontCount();//Has to be before the raycast else will always be false

                        queryFilter.maskBits = (ulong)ShapeCategories.Vehicle;
                        b2RayResult rayResult = B2Api.b2World_CastRayClosest(World.WorldId, ray.Start, ray.Translation, queryFilter);

                        if (rayResult.hit)
                        {
                            float distance = rayDistance * rayResult.fraction;

                            if (rayResult.shapeId != Body.ShapeId)
                            {
                                b2Filter filter = B2Api.b2Shape_GetFilter(rayResult.shapeId);
                                if (filter.categoryBits == (ulong)ShapeCategories.Vehicle)
                                {
                                    bool counted = SetVehicleInFrontCount(rayResult.shapeId, rayResult.fraction);
                                }
                                else
                                {
                                    EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Should query vehicle not something else"));
                                }
                            }
                            else
                            {
                                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Shape collided with self, raycast start point incorrect"));
                            }
                        }

                        // B2Api.b2World_CastRay(World.WorldId, ray.Start, ray.Translation, queryFilter, rayCastDelegate, 1);
                        //Only query intersections if no car already in front
                        if (vehicleInFrontCount == 0)
                        {
                            rayDistance = 15.0f;
                            ray = new Ray(calcRayStart, new Vector2(angleForRay.c * rayDistance, angleForRay.s * rayDistance));
                            intersectionInFrontCount = 0;
                            queryFilter.maskBits = (ulong)ShapeCategories.Intersection;
                            b2RayResult rayResultIntersect = B2Api.b2World_CastRayClosest(World.WorldId, ray.Start, ray.Translation, queryFilter);

                            if (rayResultIntersect.hit)
                            {
                                float distance = rayDistance * rayResultIntersect.fraction;

                                if (rayResultIntersect.shapeId != Body.ShapeId)
                                {
                                    b2Filter filter = B2Api.b2Shape_GetFilter(rayResultIntersect.shapeId);
                                    if (filter.categoryBits == (ulong)ShapeCategories.Intersection)
                                    {
                                        SetIntersectionLastAt(rayResultIntersect.shapeId);
                                    }
                                    else
                                    {
                                        EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Should query intersection not something else"));
                                    }
                                }
                                else
                                {
                                    EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Shape collided with self, raycast start point incorrect"));
                                }
                            }
                        }
                    }

                    if (intersectionInFrontCount == 0)
                    {
                        hasClearedIntersection = true;
                    }
                    else
                    {
                        hasClearedIntersection = false;
                    }

                    if (State == VehicleStates.Stopped)
                    {
                        stoppedElaspedTime = Sim.GetSimTime() - stoppedStartTime;

                        if (stoppedElaspedTime > stoppedThresholdWaitTime)
                        {
                            ResetVehicleToNewPos();
                            stoppedElaspedTime = 0;
                            startedStoppedTimer = false;
                            vehicleInFrontCount = 0;//Reset this so resetVehicle to new position isn't called twice
                            insideAnotherVehicle = false;//Reset this so resetVehicle to new position isn't called twice
                        }
                    }
                    else
                    {
                        stoppedStartTime = 0;
                        startedStoppedTimer = false;
                        stoppedElaspedTime = 0;
                    }

                    if (vehicleInFrontCount > 0)
                    {
                        VehicleInFront = true;
                        vehicleInFrontElaspedTime = Sim.GetSimTime() - vehicleInFrontStartTime;

                        if (vehicleInFrontElaspedTime > vehicleInFrontThresholdWaitTime)
                        {
                            ResetVehicleToNewPos();
                            insideAnotherVehicle = false;//Reset this so resetVehicle to new position isn't called twice
                        }
                    }
                    else
                    {
                        VehicleInFront = false;
                        vehicleInFrontElaspedTime = 0;
                    }

                    if (insideAnotherVehicle)
                    {
                        ResetVehicleToNewPos();
                        insideAnotherVehicle = false;
                    }
                }
            }

            if (didFirstUpdate == false)//Set the enabled one time after first scan
            {
                try
                {
                    feature["Hidden"] = "false";
                }
                catch (Exception ex)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Vehicle missing enable feature + {ex.ToString()}"));
                }
                if (path == null || pathSteps == null)
                {
                    ResetVehicleToNewPos();
                }
            }

            didFirstUpdate = true;
        }

        private bool OverlapCallbackVehicle(b2ShapeId shapeId, nint context)
        {
            bool returnValue = true;

            if (shapeId != Body.ShapeId)
            {
                IntPtr intPtr = B2Api.b2Shape_GetUserData(shapeId);

                Vehicle otherVehicle = NativeHandle.GetObject<Vehicle>(intPtr);

                if (IsCollidedVehicleSameEdgeOrIntersection(otherVehicle.currentRoadEdge))
                {
                    insideAnotherVehicle = true;
                    returnValue = false;
                }
            }
            return returnValue;//return false to terminate
        }

        private bool OverlapCallbackIntersection(b2ShapeId shapeId, nint context)
        {
            bool keepCheckingOverlap = true;

            if (shapeId != Body.ShapeId)
            {
                intersectionOccupied = true;
            }
            else
            {
                //If this vehicle is the one in the intersection don't mark it as occupied
                //that way it will leave the intersection if no car infront and it is blocking
                //intersection
                intersectionOccupied = false;
                keepCheckingOverlap = false;//Don't do any more checks if false
            }

            return keepCheckingOverlap;
        }

        public void ResetVehicleToNewPos(bool useCurrentPos = false)
        {
            int goalNode;
            int startNode;

            if (graph == null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Tried to set path when graph was null"));
                return;
            }
            if (!useCurrentPos)
            {
                if (Sim.CensusSpawn != null)
                {
                    if (Sim.CensusSpawn.IsLoaded)
                    {
                        startNode = Sim.CensusSpawn.PickWeightedSpawnNode();
                    }
                    else
                    {
                        startNode = TrafficVolumeLoader.PickWeightedDestination(graph, -1);
                    }
                }
                else
                {
                    startNode = TrafficVolumeLoader.PickWeightedDestination(graph, -1);
                }
            }
            else
            {
                startNode = currentRoadEdge.From;
            }

            goalNode = TrafficVolumeLoader.PickWeightedDestination(graph, startNode); //  we donmt use goal node here but it is needed to call pick weighted destination which also sets the path for the vehicle

            setNewPath(startNode);
            pathSegmentIndex = 0;

            if (path is not null)
            {
                AdvanceToNextRoad();
            }
            else
            {
                ResetVehicleToNewPos();
            }
            B2Api.b2Body_SetLinearVelocity(Body.BodyId, Vector2.Zero);
            B2Api.b2Body_SetAngularVelocity(Body.BodyId, 0);
            b2Rot rot = b2Rot.FromAngle(GetTargetAngle());
            Vector2 initialPosition = GetRandomOffsetFromRoad();
            B2Api.b2Body_SetTransform(Body.BodyId, initialPosition, rot);
        }

        private Vector2 GetRandomOffsetFromRoad()
        {
            Vector2 returnValue = startPos;
            if (currentRoadEdge.Feature is GeometryFeature gf)
            {
                if (gf.Geometry is LineString lineString)
                {
                    Vector2 startPosRoad = Helper.Convert2Box2dWorldPosition(lineString.Coordinates[prevIndexLineString].X, lineString.Coordinates[prevIndexLineString].Y);
                    Vector2 endPosRoad = Helper.Convert2Box2dWorldPosition(lineString.Coordinates[indexLineString].X, lineString.Coordinates[indexLineString].Y);

                    Vector2 roadDirectionNormalized = Vector2.Normalize(new Vector2(endPosRoad.X - startPosRoad.X, endPosRoad.Y - startPosRoad.Y));
                    float angleForLaneOffset = MathF.Atan2(roadDirectionNormalized.Y, roadDirectionNormalized.X);
                    if (float.IsNaN(angleForLaneOffset))
                        angleForLaneOffset = 0;

                    Vector2 laneOffset = new Vector2(
                        MathF.Cos(angleForLaneOffset + Helper.Deg2Rad(-90.0f - 45.0f)) * Helper.DefaultLaneWidth,
                        MathF.Sin(angleForLaneOffset + Helper.Deg2Rad(-90.0f - 45.0f)) * Helper.DefaultLaneWidth);

                    returnValue = new Vector2(startPosRoad.X + laneOffset.X * (float)(5.0f + 10.0f * Random.Shared.NextDouble()), startPosRoad.Y + laneOffset.Y * (float)(5.0f + 10.0f * Random.Shared.NextDouble()));
                }
            }

            return returnValue;
        }

        private float GetTargetAngle()
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

            if (float.IsNaN(targetAngle))
            {
                targetAngle = 0;
            }

            return targetAngle = MathF.Atan2(directionNormalized.Y, directionNormalized.X);

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
        }

        private void StepThroughLineString(bool isNewRoad)
        {
            if (currentRoadEdge.Feature is GeometryFeature g)
            {
                if (g.Geometry is LineString lineString)
                {
                    if (isNewRoad)
                    {
                        if (lineString.Count >= 2)
                        {
                            prevIndexLineString = currentRoadEdge.IsFromStartOfLineString ? 0 : lineString.Count - 1;
                            indexLineString = currentRoadEdge.IsFromStartOfLineString ? 1 : lineString.Count - 2;
                        }
                        else
                        {
                            EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"A line string was less than 2 points"));
                        }
                    }
                    else
                    {
                        if (currentRoadEdge.IsFromStartOfLineString)
                        {
                            if (indexLineString + 1 < lineString.Count)
                            {
                                prevIndexLineString = indexLineString;
                                indexLineString++;
                            }
                            else
                            {
                                AdvanceToNextRoad();
                            }
                        }
                        else
                        {
                            if (indexLineString - 1 >= 0 && lineString.Count >= 1)
                            {
                                prevIndexLineString = indexLineString;
                                indexLineString--;
                            }
                            else
                            {
                                AdvanceToNextRoad();
                            }
                        }
                    }
                    UpdateEndPos();
                }
            }
        }

        private void UpdateEndPos()
        {
            if (currentRoadEdge.Feature is GeometryFeature gf)
            {
                if (gf.Geometry is LineString lineString)
                {
                    Vector2 startPosRoad = Helper.Convert2Box2dWorldPosition(lineString.Coordinates[prevIndexLineString].X, lineString.Coordinates[prevIndexLineString].Y);
                    Vector2 endPosRoad = Helper.Convert2Box2dWorldPosition(lineString.Coordinates[indexLineString].X, lineString.Coordinates[indexLineString].Y);

                    Vector2 roadDirectionNormalized = Vector2.Normalize(new Vector2(endPosRoad.X - startPosRoad.X, endPosRoad.Y - startPosRoad.Y));
                    float angleForLaneOffset = MathF.Atan2(roadDirectionNormalized.Y, roadDirectionNormalized.X);
                    if (float.IsNaN(angleForLaneOffset))
                        angleForLaneOffset = 0;
                    //1/0.707f so lane is wider since we are measuring at a 135angle
                    Vector2 laneOffset = new Vector2(
                        MathF.Cos(angleForLaneOffset + Helper.Deg2Rad(-90.0f - 45.0f)) * Helper.DefaultLaneWidth * (calculatedOffsetForLane) / 0.707f,
                        MathF.Sin(angleForLaneOffset + Helper.Deg2Rad(-90.0f - 45.0f)) * Helper.DefaultLaneWidth * (calculatedOffsetForLane) / 0.707f);

                    startPos = new Vector2(startPosRoad.X + laneOffset.X, startPosRoad.Y + laneOffset.Y);

                    endPos = new Vector2(endPosRoad.X + laneOffset.X, endPosRoad.Y + laneOffset.Y);
                }
            }
        }

        private void SetAngle(float currentAngle)
        {
            float targetAngle = GetTargetAngle();

            float angle = targetAngle - currentAngle;

            if (float.IsNaN(angle))
            {
                angle = 0;
            }
            else
            {
                //https://phaser.io/tutorials/box2d-tutorials/rotate-to-angle#:~:text=This%20topic%20covers%20rotating%20a%20body%20to,directly%20setting%20the%20angle%20or%20using%20torque/
                angle = (float)(((angle + Math.PI) % (2 * Math.PI) + 2 * Math.PI) % (2 * Math.PI) - Math.PI);

                B2Api.b2Body_SetAngularVelocity(Body.BodyId, angle * settings.GetTurnSpeed());

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

        private RoadEdge SetCurrentRoadEdge(RoadEdge updatedRoadEdge, TurnDirection turn = TurnDirection.Straight)
        {
            float newSpeedLimit = Helper.TryGetFeatureKVPToFloat(updatedRoadEdge.Feature, "SPEED_LIMI", SpeedLimit);
            if (newSpeedLimit < 30.0f)
            {
                newSpeedLimit = 30.0f;
            }
            SpeedLimit = Helper.DoMapCorrection(newSpeedLimit);

            if (updatedRoadEdge.Feature is GeometryFeature g)
            {
                if (g.Geometry is LineString lineString)
                {
                    if (lineString.Count >= 2)
                    {
                        prevIndexLineString = updatedRoadEdge.IsFromStartOfLineString ? 0 : lineString.Count - 1;
                        indexLineString = updatedRoadEdge.IsFromStartOfLineString ? 1 : lineString.Count - 2;
                    }
                }
            }

            SetLane(updatedRoadEdge, turn);

            return updatedRoadEdge;
        }

        private void SetLane(RoadEdge roadEdge, TurnDirection turn = TurnDirection.Straight)
        {
            bool hasSamePropertiesAsOldEdge = true;

            RoadName = roadEdge.Metadata.RoadName;

            int numberOfLanes = Helpers.Helper.TryGetFeatureKVPToInt(roadEdge.Feature, "LANES", 2);
            if (currentRoadEdge is null || currentRoadEdge.Metadata.OneWay != roadEdge.Metadata.OneWay)
            {
                hasSamePropertiesAsOldEdge = false;
            }

            if (currentRoadEdge is null || Helpers.Helper.TryGetFeatureKVPToInt(currentRoadEdge.Feature, "LANES", 2) !=
                numberOfLanes)
            {
                hasSamePropertiesAsOldEdge = false;
            }

            if (currentRoadEdge is null || Helpers.Helper.TryGetFeatureKVPToInt(currentRoadEdge.Feature, "LANES", 2) !=
                numberOfLanes)
            {
                hasSamePropertiesAsOldEdge = false;
            }

            if (currentRoadEdge is null || turn != oldTurn)
            {
                hasSamePropertiesAsOldEdge = false;
            }

            oldTurn = turn;

            //if different properties allow picking a new lane
            if (!hasSamePropertiesAsOldEdge)
            {
                if (numberOfLanes <= 1)
                {
                    calculatedOffsetForLane = 0;
                    lanePicked = 0;
                }
                else
                {
                    if (roadEdge.Metadata.OneWay)
                    {
                        float offsetToUse = (numberOfLanes - 1) / 2.0f;

                        lanePicked = PickLaneForTurn(turn, numberOfLanes);
                        calculatedOffsetForLane = (lanePicked - offsetToUse);
                    }
                    else
                    {
                        bool isEven = (numberOfLanes % 2) == 0;
                        float offsetToUse = isEven ? 0.5f : 1.5f;
                        int chooseableLanes = isEven ? (numberOfLanes / 2) : ((numberOfLanes - 1) / 2);

                        lanePicked = PickLaneForTurn(turn, chooseableLanes);
                        calculatedOffsetForLane = (lanePicked + offsetToUse);
                    }
                }
            }
        }

        /// <summary>
        /// Pick a lane index based on the upcoming turn direction.
        /// Higher index = further right. Right turn → rightmost, left turn → leftmost.
        /// </summary>
        private static int PickLaneForTurn(TurnDirection turn, int laneCount)
        {
            if (laneCount <= 1) return 0;

            return turn switch
            {
                TurnDirection.Right => laneCount - 1,
                TurnDirection.Left => 0,
                _ => Random.Shared.Next(laneCount),
            };
        }
    }
}