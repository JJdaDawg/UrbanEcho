using Box2dNet;
using Box2dNet.Interop;
using ExCSS;
using Mapsui;
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
using UrbanEcho.Physics;
using UrbanEcho.Reporting;
using UrbanEcho.Sim;

namespace UrbanEcho.Models
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
        public bool IsForceStopped = false;
        public bool IsDormant = false;
        private static readonly Vector2 DormantPosition = new Vector2(-999999, -999999);

        private b2ShapeId? intersectionShapeLastAt;

        private RoadIntersection? intersectionLastAt;

        private float whenToStopWaiting = 0;
        private float minimumStopWaiting = 3.0f;
        private TrafficRule currentTrafficRule;

        public Vector2 Pos;

        public VehicleBody Body;

        private Vector2 startPos = Vector2.Zero;

        private Vector2 endPos = Vector2.Zero;

        private float distanceThresholdReachedTarget = 10.0f;

        private b2Rot currentAngle;

        private float targetSpeed = 0;

        private VehicleSettings settings;

        private float angleThresholdToDecelerate = Helper.Deg2Rad(25.0f);//How many degrees off target angle before decelerate
        private bool angleAboveThreshold = false;
        private float angleDifference;

        private PointFeature? feature;//The feature this vehicle is connected to

        private RoadEdge currentRoadEdge
        {
            get
            {
                if (vehiclePath == null)
                {
                    return null;
                }
                else
                {
                    return vehiclePath.GetCurrentRoadEdge();
                }
            }
        }

        public bool IsCreated = false;

        public bool IsTruck { get; }

        private int prevIndexLineString;
        private int indexLineString;

        private int updateGroup = 0;

        private float vehicleInFrontThresholdWaitTime = 120.0f;//Set long so we know it isn't just a light

        private float vehicleInFrontStartTime = 0;
        private float vehicleInFrontElapsedTime = 0;

        private int vehicleInFrontCount = 0;

        private bool insideAnotherVehicle = false;

        private bool anotherVehicleAhead = false;
        private int intersectionInFrontCount = 0;
        private bool hasClearedIntersection = true;
        private float hasClearedAtTime = 0;
        private float hasClearedElapsedTime = 0;
        private bool usingShorterRayForTurn = false;
        private float rayLengthSpeedFactor = 0.05f;
        private bool intersectionOccupied = false;
        private bool thisVehicleIsInAIntersection = false;

        private int lanePicked = 1;
        private float calculatedOffsetForLane = 0.5f;

        private float lastTimeCheckedOverlap = 0;
        private float overlapTestFrequency = 5.0f;//Check for overlap every five seconds
        private float stoppedThresholdWaitTime = 120.0f;//If vehicle hasn't moved for this long respawn it
        private float stoppedStartTime = 0;
        private float stoppedElapsedTime = 0;
        private bool startedStoppedTimer = false;

        private TurnDirection oldTurn = TurnDirection.Straight;
        private bool didFirstUpdate = false;//used to hide vehicles while loading up paths

        private float kmh = 0;

        private float currentEdgeStartTime = 0;
        private double lastUpdateSimTime = 0;
        private Stats stats = new Stats();
        private double allSpeedValues = 0;

        private RequestDestination? requestDestination = null;
        private RequestResetPosition? requestResetPosition = null;

        public float Kmh
        {
            get
            {
                return kmh;
            }
            set
            {
                kmh = Math.Clamp(value, 0.0f, 120.0f);
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
                metersFromCarInFront = value;
            }
        }

        private string roadName = "road";

        public string RoadName
        {
            get
            {
                return currentRoadEdge.Metadata.RoadName;
            }
            set
            {
                roadName = value;
            }
        }

        public int Id;
        public string VehicleType = string.Empty;

        private VehiclePath vehiclePath;

        private RayCasterVehicle rayCasterVehicle;
        private RayCasterIntersection rayCasterIntersection;
        private OverlapTest overlapTest;
        private OverlapTestVehicleAhead overlapTestVehicleAhead;
        private OverlapTestIntersectionOccupied overlapTestIntersectionOccupied;
        private OverlapTestVehicleInAnyIntersection overlapTestVehicleInAnyIntersection;

        private TurnDirection turnDirection = TurnDirection.Straight;
        private bool didExtraWaitForTurningVehicles = false;

        public Vehicle(PointFeature feature, RoadEdge currentRoadEdge, string carType, int updateGroup, RoadGraph roadGraph)
        {
            rayCasterVehicle = new RayCasterVehicle(this);
            rayCasterIntersection = new RayCasterIntersection(this);
            overlapTest = new OverlapTest(this);
            overlapTestVehicleAhead = new OverlapTestVehicleAhead(this);
            overlapTestIntersectionOccupied = new OverlapTestIntersectionOccupied(this);
            overlapTestVehicleInAnyIntersection = new OverlapTestVehicleInAnyIntersection(this);

            vehiclePath = new VehiclePath(this, roadGraph, currentRoadEdge);

            VehicleType = Helper.TryGetFeatureKVPToString(feature, "VehicleType", string.Empty);
            Id = Helper.TryGetFeatureKVPToInt(feature, "VehicleNumber", 0);

            settings = new VehicleSettings(carType);

            if (!settings.IsValid())
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed Adding Car that had {carType} as type adding as regular car"));
                settings = new VehicleSettings("RegularCar");
            }
            IsTruck = carType == "TransportTruck";
            currentTrafficRule = TrafficRule.SetDefaultTrafficRule();

            this.feature = feature;
            this.updateGroup = updateGroup;

            //FRect rect = new FRect(startPos.X - (settings.GetLength()) / 2, startPos.Y - (settings.GetWidth()) / 2, settings.GetLength(), settings.GetWidth());
            FRect rect = new FRect(startPos.X, startPos.Y - (settings.GetWidth()) / 2, settings.GetLength(), settings.GetWidth());

            Body = new VehicleBody(this, rect);

            // Compute the starting road position so the body starts on the road.
            // Without this, a failed ResetVehicleToNewPos leaves the body at (0,0)
            // and the vehicle drives through the map in a straight line to reach
            // its assigned road edge.
            UpdateEndPos();

            Vector2 initialPosition = startPos;
            Vector2 roadDir = endPos - startPos;
            float initialAngle = (roadDir != Vector2.Zero) ? MathF.Atan2(roadDir.Y, roadDir.X) : 0f;
            if (float.IsNaN(initialAngle)) initialAngle = 0;
            b2Rot rot = b2Rot.FromAngle(initialAngle);
            B2Api.b2Body_SetTransform(Body.BodyId, initialPosition, rot);
            B2Api.b2Body_SetLinearVelocity(Body.BodyId, Vector2.Zero);
            Pos = B2Api.b2Body_GetPosition(Body.BodyId);
            if (float.IsNaN(Pos.X) || float.IsNaN(Pos.Y))
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Vehicle position set to a NaN value"));
            }
            else
            {
                IsCreated = true;
            }
        }

        public void Update()
        {
            Vector2 LastPos = Pos;

            if (didFirstUpdate)//Only do the update after path initially set
            {
                if (!B2Api.b2Body_IsValid(Body.BodyId))
                {
                    return;
                }
                Pos = B2Api.b2Body_GetPosition(Body.BodyId);
                if (float.IsNaN(Pos.X) || float.IsNaN(Pos.Y))
                {
                    intersectionLastAt = null;
                    intersectionShapeLastAt = null;
                    vehiclePath.ResetVehicleToNewPos();
                    Pos = B2Api.b2Body_GetPosition(Body.BodyId);
                    return;
                }
                if (IsForceStopped)
                {
                    B2Api.b2Body_SetLinearVelocity(Body.BodyId, Vector2.Zero);
                    State = VehicleStates.Stopped;
                    Kmh = 0;
                    return;
                }
                if (requestDestination != null)
                {
                    vehiclePath.SetDestination(requestDestination.NodeId);
                    requestDestination = null;
                }

                if (requestResetPosition != null)
                {
                    intersectionLastAt = null;
                    intersectionShapeLastAt = null;
                    vehiclePath.ResetVehicleToNewPos();
                    requestResetPosition = null;
                }

                UpdateStats();

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
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Feature that is assigned to the vehicle is null"));
                }

                currentAngle = B2Api.b2Body_GetRotation(Body.BodyId);
                float currentFloatAngle = currentAngle.GetAngle();
                if (!(anotherVehicleAhead))
                {
                    SetAngle(currentFloatAngle);
                }
                else
                {
                    B2Api.b2Body_SetAngularVelocity(Body.BodyId, 0);
                }
                try
                {
                    feature["Angle"] = Helper.Rad2Deg(currentFloatAngle);
                }
                catch (Exception ex)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Vehicle missing angle feature + {ex.ToString()}"));
                }

                CollisionChecks(currentFloatAngle);
                thisVehicleIsInAIntersection = false;
                intersectionOccupied = false;
                if (IsWaiting)
                {
                    ChecksWhileWaiting();
                }
                else
                {
                    WaitingOnIntersection = false;
                    didExtraWaitForTurningVehicles = false;
                }

                UpdateSpeedAndState();
            }
            else
            {
                if (vehiclePath.SetInitialPath())
                {
                    try
                    {
                        feature["Hidden"] = "false";
                    }
                    catch (Exception ex)
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Vehicle missing enable feature + {ex.ToString()}"));
                    }

                    didFirstUpdate = true;
                }
            }
        }

        public void ResetBodyTransform()
        {
            stoppedElapsedTime = 0;
            startedStoppedTimer = false;
            vehicleInFrontCount = 0;//Reset this so resetVehicle to new position isn't called twice
            insideAnotherVehicle = false;//Reset this so resetVehicle to new position isn't called twice
            anotherVehicleAhead = false;//Reset this so resetVehicle to new position isn't called twice
            overlapTest.ResetInsideAnotherVehicleCount();
            VehicleInFront = false;

            B2Api.b2Body_SetLinearVelocity(Body.BodyId, Vector2.Zero);
            B2Api.b2Body_SetAngularVelocity(Body.BodyId, 0);
            float getAngle = GetTargetAngle();
            if (float.IsNaN(getAngle))
            {
                getAngle = 0;
            }
            b2Rot rot = b2Rot.FromAngle(getAngle);
            Vector2 initialPosition = GetRandomOffsetFromRoad();
            if (float.IsNaN(initialPosition.X) || float.IsNaN(initialPosition.Y))
            {
                return;
            }

            B2Api.b2Body_SetTransform(Body.BodyId, initialPosition, rot);
        }

        public void RefreshTrafficRuleReferences()
        {
            if (currentRoadEdge != null && intersectionLastAt != null)
            {
                EdgeTrafficRule? edgeTrafficRule = intersectionLastAt.EdgesInto.Find(e => e.RoadEdge == currentRoadEdge);
                if (edgeTrafficRule != null)
                {
                    currentTrafficRule = edgeTrafficRule.TrafficRule;
                }
                else
                {
                    currentTrafficRule = intersectionLastAt.FallBackTrafficRule;
                }
            }
        }

        private void SetIntersectionLastAt(b2ShapeId shapeId)
        {
            //If it isn't same shape again get the shapes userdata
            if (intersectionShapeLastAt == null || (intersectionShapeLastAt != shapeId && hasClearedIntersection == true))
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
                    whenToStopWaiting = SimManager.Instance.GetSimTime() + minimumStopWaiting;
                }
            }

            if (intersectionShapeLastAt != null && intersectionShapeLastAt == shapeId)
            {
                intersectionInFrontCount++;
            }
        }

        public void RequestSetDestination(int goalNodeId)
        {
            requestDestination = new RequestDestination(goalNodeId);
        }

        public RoadEdge GetRoadEdge()
        {
            return currentRoadEdge;
        }

        /// <summary>
        /// Returns the line-string features for the edge currently being traversed
        /// plus every remaining edge in the vehicle's path. Used by the path overlay.
        /// </summary>
        public IReadOnlyList<IFeature> GetRemainingPathFeatures()
        {
            return vehiclePath.GetRemainingPathFeatures();
        }

        /// <summary>
        /// If <paramref name="closedEdge"/> appears in the vehicle's remaining route (or is the
        /// edge currently being traversed), discards the stale path and builds a new one from
        /// the current destination node, which A* will automatically route around the closed edge.
        /// </summary>
        public void RerouteAroundEdge(RoadEdge closedEdge)
        {
            vehiclePath.RerouteAroundEdge(closedEdge);
        }

        public bool IsCollidedVehicleSameEdgeOrIntersection(Vehicle otherVehicle)
        {
            RoadEdge otherVehicleEdge = otherVehicle.currentRoadEdge;
            if (currentRoadEdge == otherVehicleEdge)
            {
                return true;
            }

            RoadGraph? graph = vehiclePath.GetRoadGraph();

            if (graph is not null)
            {
                if ((Vector2.Distance(Pos, endPos) < 30 || Vector2.Distance(Pos, startPos) < 30) && (Vector2.Distance(otherVehicle.Pos, otherVehicle.endPos) < 30 || Vector2.Distance(otherVehicle.Pos, otherVehicle.startPos) < 30))
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
            }
            return false;
        }

        private void ResetVehicleInFrontCount()
        {
            vehicleInFrontCount = 0;
        }

        private void CollisionChecks(float currentFloatAngle)
        {
            if (SimManager.Instance.GetGroupToUpdate() == updateGroup)
            {
                if (SimManager.Instance.GetSimTime() > lastTimeCheckedOverlap + overlapTestFrequency)
                {
                    lastTimeCheckedOverlap = SimManager.Instance.GetSimTime() + (float)Random.Shared.NextDouble();//add a 1 second random offset so not all vehicles
                                                                                                                  //do this at same time

                    Vector2[] vertices = Body.GetShapeVertices();
                    b2ShapeProxy b2ShapeProxy = B2Api.b2MakeOffsetProxy(vertices, vertices.Length, 0.0f, Pos, currentAngle);
                    insideAnotherVehicle = overlapTest.DoOverlapTest(b2ShapeProxy, Body.ShapeId);
                }

                if (!(insideAnotherVehicle))
                {
                    ResetVehicleInFrontCount();//Has to be before the raycast else will always be zero value
                                               //Start raycast from front of car, so ray is not against self
                    Vector2 calcRayStart = new Vector2(Pos.X + (settings.GetLength() * 0.75f + 0.1f) * currentAngle.c,
                       Pos.Y + (settings.GetLength() * 0.75f + 0.1f) * currentAngle.s);

                    (bool hitCounted, float hitDistance) = rayCasterVehicle.DoRayCast(calcRayStart, currentFloatAngle, usingShorterRayForTurn, rayLengthSpeedFactor * Kmh, Body.ShapeId);

                    if (hitCounted)
                    {
                        MetersFromCarInFront = hitDistance;

                        if (!VehicleInFront)
                        {
                            vehicleInFrontStartTime = SimManager.Instance.GetSimTime();
                        }
                        vehicleInFrontCount++;
                        hitCounted = true;

                        //if (IsWaiting == true)
                        //{
                        //    whenToStopWaiting = SimManager.Instance.GetSimTime() + minimumStopWaiting;
                        //}
                    }

                    anotherVehicleAhead = false;
                    if (vehicleInFrontCount == 0 && (state == VehicleStates.SlowDownForTurn || state == VehicleStates.Decelerating))
                    {
                        (Vector2 pos, b2Rot angle, bool valid) = GetLookAheadPosAndAngle(2.0f);
                        if (valid)
                        {
                            Vector2[] vertices = Body.GetShapeVertices();

                            b2ShapeProxy b2ShapeProxyAhead = B2Api.b2MakeOffsetProxy(vertices, vertices.Length, 0.0f, pos, angle);
                            if (overlapTestVehicleAhead.DoOverlapTest(b2ShapeProxyAhead, Body.ShapeId))
                            {
                                if (!VehicleInFront)
                                {
                                    vehicleInFrontStartTime = SimManager.Instance.GetSimTime();
                                }
                                vehicleInFrontCount++;
                            }
                        }
                    }

                    intersectionInFrontCount = 0;

                    (bool hitIntersectionCounted, b2ShapeId intersectionShapeId) = rayCasterIntersection.DoRayCast(calcRayStart, currentFloatAngle, usingShorterRayForTurn, rayLengthSpeedFactor * Kmh, Body.ShapeId);

                    if (hitIntersectionCounted)
                    {
                        SetIntersectionLastAt(intersectionShapeId);
                    }

                    if (intersectionInFrontCount == 0)
                    {
                        if (hasClearedIntersection == false)
                        {
                            hasClearedIntersection = true;
                            hasClearedAtTime = SimManager.Instance.GetSimTime();
                            usingShorterRayForTurn = true;
                        }
                        if (state == VehicleStates.AtTargetSpeed || hasClearedElapsedTime > 15.0f)
                        {
                            usingShorterRayForTurn = false;
                        }
                        hasClearedElapsedTime = SimManager.Instance.GetSimTime() - hasClearedAtTime;
                    }
                    else
                    {
                        hasClearedIntersection = false;
                    }
                }

                if (State == VehicleStates.Stopped)
                {
                    stoppedElapsedTime = SimManager.Instance.GetSimTime() - stoppedStartTime;

                    if (stoppedElapsedTime > stoppedThresholdWaitTime)
                    {
                        intersectionLastAt = null;
                        intersectionShapeLastAt = null;
                        vehiclePath.ResetVehicleToNewPos();
                        stoppedElapsedTime = 0;
                        startedStoppedTimer = false;
                        vehicleInFrontCount = 0;//Reset this so resetVehicle to new position isn't called twice
                        insideAnotherVehicle = false;//Reset this so resetVehicle to new position isn't called twice
                        anotherVehicleAhead = false;//Reset this so resetVehicle to new position isn't called twice
                        overlapTest.ResetInsideAnotherVehicleCount();
                    }
                }
                else
                {
                    stoppedStartTime = 0;
                    startedStoppedTimer = false;
                    stoppedElapsedTime = 0;
                }

                if (vehicleInFrontCount > 0)
                {
                    VehicleInFront = true;
                    if (state != VehicleStates.Stopped)
                    {
                        vehicleInFrontStartTime = SimManager.Instance.GetSimTime();
                    }
                    vehicleInFrontElapsedTime = SimManager.Instance.GetSimTime() - vehicleInFrontStartTime;

                    if (vehicleInFrontElapsedTime > vehicleInFrontThresholdWaitTime)
                    {
                        intersectionLastAt = null;
                        intersectionShapeLastAt = null;
                        vehiclePath.ResetVehicleToNewPos();
                        insideAnotherVehicle = false;//Reset this so resetVehicle to new position isn't called twice
                        anotherVehicleAhead = false;
                        overlapTest.ResetInsideAnotherVehicleCount();
                    }
                    else
                    {
                        if (vehicleInFrontElapsedTime > vehicleInFrontThresholdWaitTime * 0.25f && thisVehicleIsInAIntersection)
                        {
                            intersectionLastAt = null;
                            intersectionShapeLastAt = null;
                            vehiclePath.ResetVehicleToNewPos();
                            insideAnotherVehicle = false;//Reset this so resetVehicle to new position isn't called twice
                            anotherVehicleAhead = false;
                            overlapTest.ResetInsideAnotherVehicleCount();
                        }
                    }
                }
                else
                {
                    VehicleInFront = false;
                    vehicleInFrontElapsedTime = 0;
                }

                if (insideAnotherVehicle)
                {
                    intersectionLastAt = null;
                    intersectionShapeLastAt = null;
                    vehiclePath.ResetVehicleToNewPos();
                    insideAnotherVehicle = false;
                    anotherVehicleAhead = false;
                    overlapTest.ResetInsideAnotherVehicleCount();
                }
            }
        }

        private void ChecksWhileWaiting()
        {
            if (currentTrafficRule.IsBlockingTraffic())
            {
                WaitingOnIntersection = true;
            }
            else
            {
                if (SimManager.Instance.GetSimTime() > whenToStopWaiting)
                {
                    if (SimManager.Instance.GetGroupToUpdate() == updateGroup)
                    {
                        if (intersectionLastAt is not null)
                        {
                            RoadIntersection intersection = intersectionLastAt;
                            bool doExtraWaitForTurningVehicles = false;
                            if (intersection.TheSignalType == RoadIntersection.SignalType.FullSignal && didExtraWaitForTurningVehicles == false)
                            {
                                if (turnDirection == TurnDirection.Straight)//Wait bit longer so other vehicles can turn
                                {
                                    whenToStopWaiting = SimManager.Instance.GetSimTime() + 10;
                                    doExtraWaitForTurningVehicles = true;
                                    didExtraWaitForTurningVehicles = true;
                                }
                            }
                            if (!doExtraWaitForTurningVehicles)
                            {
                                Vector2[] vertices = intersection.Body.GetShapeVertices();
                                b2Rot zeroRotation = b2Rot.FromAngle(0);
                                b2ShapeProxy b2ShapeProxy = B2Api.b2MakeOffsetProxy(vertices, vertices.Length, 0.0f, intersection.Center, zeroRotation);

                                intersectionOccupied = overlapTestIntersectionOccupied.DoOverlapTest(b2ShapeProxy, Body.ShapeId);

                                if (!(intersectionOccupied))
                                {
                                    WaitingOnIntersection = false;
                                    IsWaiting = false;
                                    didExtraWaitForTurningVehicles = false;
                                }

                                //Check if this car is in any intersection if it is then we can set isWaiting to false

                                Vector2[] vehicleVertices = Body.GetShapeVertices();

                                b2ShapeProxy b2VehicleShapeProxy = B2Api.b2MakeOffsetProxy(vehicleVertices, vehicleVertices.Length, 0.0f, Pos, currentAngle);

                                thisVehicleIsInAIntersection = overlapTestVehicleInAnyIntersection.DoOverlapTest(b2ShapeProxy, Body.ShapeId);

                                if (thisVehicleIsInAIntersection == true || stoppedElapsedTime > 100)//or if vehicle has waited a long time then other side of intersection may be blocked and try moving forward
                                {
                                    WaitingOnIntersection = false;
                                    IsWaiting = false;
                                    didExtraWaitForTurningVehicles = false;
                                }
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

        private void UpdateSpeedAndState()
        {
            if (WaitingOnIntersection == true)
            {
                targetSpeed = 0;
            }
            else
            {
                targetSpeed = SpeedLimit;
            }

            float updateToSpeed = Kmh;

            if (VehicleInFront == false && anotherVehicleAhead == false)
            {
                if (Kmh <= 0.1f && targetSpeed <= 0.1f)
                {
                    State = VehicleStates.Stopped;
                    if (!startedStoppedTimer)
                    {
                        startedStoppedTimer = true;
                        stoppedStartTime = SimManager.Instance.GetSimTime();
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
                        stoppedStartTime = SimManager.Instance.GetSimTime();
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
                updateToSpeed = Math.Clamp(updateToSpeed - settings.GetDeceleration() * settings.GetSlowDownfactor() * Math.Abs(angleDifference) / Helper.Deg2Rad(180.0f), 0, SpeedLimit);
            }

            float speedToUseMs = Helper.Kmh2Ms(updateToSpeed);
            Vector2 velocityToSetMs = new Vector2(currentAngle.c * speedToUseMs, currentAngle.s * speedToUseMs);
            if (State != VehicleStates.Stopped)
            {
                if (!float.IsNaN(velocityToSetMs.X) && (!float.IsNaN(velocityToSetMs.Y)))
                {
                    B2Api.b2Body_SetLinearVelocity(Body.BodyId, velocityToSetMs);
                }
            }
            else
            {
                B2Api.b2Body_SetLinearVelocity(Body.BodyId, Vector2.Zero);
            }
            Kmh = Helper.MS2Kmh(Vector2.Dot(velocityToSetMs, new Vector2(currentAngle.c, currentAngle.s)));
            if (float.IsNaN(Kmh))
            {
                Kmh = 0;
            }
            else
            {
                if (Kmh > 0.1f)
                {
                    startedStoppedTimer = false;
                }
            }
        }

        public void SetUsingShorterRayForTurn(bool value)
        {
            usingShorterRayForTurn = value;
        }

        private (Vector2 pos, b2Rot angle, bool valid) GetLookAheadPosAndAngle(float lookAheadValue)
        {
            Vector2 pos = new Vector2(Pos.X + currentAngle.c * lookAheadValue, Pos.Y + currentAngle.s * lookAheadValue);
            b2Rot angle = b2Rot.FromAngle(0);
            bool valid = false;

            if (startPos != endPos)
            {
                Vector2 roadDirectionNormalized = Vector2.Normalize(new Vector2(endPos.X - startPos.X, endPos.Y - startPos.Y));

                float angleToUse = MathF.Atan2(roadDirectionNormalized.Y, roadDirectionNormalized.X);

                if (!float.IsNaN(pos.X) && !float.IsNaN(pos.Y) && !float.IsNaN(angleToUse))
                {
                    angle = b2Rot.FromAngle(angleToUse);
                    pos = new Vector2(pos.X + lookAheadValue * angle.c, pos.Y + lookAheadValue * angle.s);
                    valid = true;
                }
            }

            return (pos, angle, valid);
        }

        public void SetForceStop(bool stopCommand)
        {
            IsForceStopped = stopCommand;
        }

        /// <summary>
        /// Teleports the vehicle off-screen, hides it, and zeroes velocity.
        /// The Box2D body stays alive — no creation/destruction needed.
        /// </summary>
        public void GoDormant()
        {
            if (IsDormant || Body == null) return;
            IsDormant = true;
            B2Api.b2Body_SetLinearVelocity(Body.BodyId, Vector2.Zero);
            B2Api.b2Body_SetAngularVelocity(Body.BodyId, 0);
            b2Rot rot = b2Rot.FromAngle(0);
            B2Api.b2Body_SetTransform(Body.BodyId, DormantPosition, rot);
            Pos = DormantPosition;
            if (feature != null)
            {
                feature["Hidden"] = "true";
                feature.Point.X = DormantPosition.X;
                feature.Point.Y = DormantPosition.Y;
            }
        }

        /// <summary>
        /// Wakes a dormant vehicle: picks a fresh path and un-hides it.
        /// </summary>
        public void WakeUp()
        {
            if (!IsDormant || Body == null) return;
            IsDormant = false;
            intersectionLastAt = null;
            intersectionShapeLastAt = null;
            vehiclePath.ResetVehicleToNewPos();
            if (feature != null)
            {
                feature["Hidden"] = "false";
            }
        }

        public void RequestResetVehicleToNewPos()
        {
            requestResetPosition = new RequestResetPosition();
        }

        public void StepThroughLineString(bool isNewRoad)
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
                            EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"A line string was less than 2 points"));
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
                                vehiclePath.AdvanceToNextRoad();
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
                                vehiclePath.AdvanceToNextRoad();
                            }
                        }
                    }
                    UpdateEndPos();
                }
            }
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
            if (endPos != startPos)
            {
                Vector2 roadDirection = Vector2.Normalize(new Vector2(endPos.X - startPos.X, endPos.Y - startPos.Y));
                Vector2 closestPointToLine = findNearestPointOnLine(startPos, endPos, Pos);

                Vector2 targetPointToAimTowards = new Vector2(closestPointToLine.X + roadDirection.X * settings.GetLookAheadValueForSteerTowardsLane(), closestPointToLine.Y + roadDirection.Y * settings.GetLookAheadValueForSteerTowardsLane());
                if (Vector2.Distance(endPos, startPos) <= Vector2.Distance(targetPointToAimTowards, startPos))
                {
                    targetPointToAimTowards = endPos;
                }

                Vector2 directionNormalized = Vector2.Normalize(new Vector2(targetPointToAimTowards.X - Pos.X, targetPointToAimTowards.Y - Pos.Y));

                targetAngle = MathF.Atan2(directionNormalized.Y, directionNormalized.X);
            }
            else
            {
                Vector2 directionNormalized = Vector2.Normalize(new Vector2(endPos.X - Pos.X, endPos.Y - Pos.Y));

                targetAngle = MathF.Atan2(directionNormalized.Y, directionNormalized.X);
            }

            if (float.IsNaN(targetAngle))
            {
                targetAngle = 0;
            }

            return targetAngle;

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
                    angleDifference = angle;
                    angleAboveThreshold = true;
                }
                else
                {
                    angleAboveThreshold = false;
                }
            }
        }

        public void SetLane(RoadEdge roadEdge, TurnDirection turn = TurnDirection.Straight)
        {
            turnDirection = turn;
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

        public void UpdatePrevIndexLineString(int value)
        {
            prevIndexLineString = value;
        }

        public void UpdateIndexLineString(int value)
        {
            indexLineString = value;
        }

        public void UpdateSpeedLimit(float value)
        {
            SpeedLimit = value;
        }

        private void UpdateStats()
        {
            double timeDelta = SimManager.Instance.GetSimTime() - lastUpdateSimTime;

            if (currentEdgeStartTime != 0)//only include after currentEdgeTime has been set
            {
                stats.ElaspedTime = SimManager.Instance.GetSimTime() - currentEdgeStartTime;

                //Should always be a positive
                if (timeDelta > 0)
                {
                    if (state == VehicleStates.Stopped)
                    {
                        stats.WaitTime += timeDelta;
                    }

                    allSpeedValues += Kmh * timeDelta;
                }
            }

            lastUpdateSimTime = SimManager.Instance.GetSimTime();
        }

        public void ResetStats()
        {
            stats.Reset();
        }

        public void UpdateStatsOnNewRoad()
        {
            if (currentRoadEdge != null && currentEdgeStartTime > 0)
            {
                if (stats.ElaspedTime >= 1)//Do not include very short times
                {
                    stats.AverageSpeed = allSpeedValues / stats.ElaspedTime;
                    currentRoadEdge.VehicleLeaving(stats);
                }
            }

            currentEdgeStartTime = SimManager.Instance.GetSimTime();
            allSpeedValues = 0;
            stats.Reset();
        }

        public Vector2 GetPinPos()
        {
            return new Vector2(Pos.X + currentAngle.c * settings.GetLength() / 2, Pos.Y + currentAngle.s * settings.GetLength() / 2);
        }
    }
}