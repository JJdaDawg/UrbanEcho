using Box2dNet;
using Box2dNet.Interop;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Helpers;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Point = NetTopologySuite.Geometries.Point;

namespace UrbanEcho.Sim
{
    public enum VehicleStates
    {
        Stopped = 0,
        Accelerating = 1,
        Decelerating = 2,
        AtTargetSpeed = 3
    }

    public class Vehicle
    {
        public b2CastResultFcn rayCastDelegate;

        private b2ShapeId intersectionShapeLastAt;

        public RoadIntersection? intersectionLastAt;

        private float whenToStopWaiting = 0;

        public Vector2 Pos;

        private Rectangle carRectImage = new Rectangle(0, 0, 48, 24);

        //from google normal car 4.5m long and width 2.25m
        private float carLength = 4.5f;

        private float carWidth = 2.25f;

        private VehicleBody body;

        private Vector2 startPos = Vector2.Zero;
        private Vector2 endPos = Vector2.Zero;

        private Vector2 nextPointOnPath = Vector2.Zero;
        private bool indexingForwardThroughLineString = true;

        private float distanceToTravel = 100.0f;

        private b2QueryFilter queryFilter = B2Api.b2DefaultQueryFilter();

        private Ray ray = new Ray(Vector2.Zero, Vector2.Zero);
        private float rayDistance = 10.0f;
        private b2Rot angle;

        private bool waitingOnIntersection = false;
        private bool isWaiting = false;

        private float targetSpeed = 0;
        private float speedLimit = 50;
        private float acceleration = 1.0f;
        private float deceleration = 1.5f;

        private bool carInFront = false;
        private float metersFromCarInFront = 0;

        private float kmh = 0;

        private VehicleStates state = VehicleStates.Stopped;

        private PointFeature feature;//The feature this vehicle is connected to

        private float startingAngle = 0;

        private RoadNode nodeFrom;
        private RoadNode nodeTo;

        private IFeature currentRoad;

        public bool IsCreated = false;

        public Vehicle(PointFeature feature, RoadNode roadNodeFrom, RoadNode roadNodeTo, IFeature currentRoad)
        {
            nodeFrom = roadNodeFrom;
            nodeTo = roadNodeTo;

            this.feature = feature;

            double startX = feature.Point.X - World.Offset.X;
            double startY = feature.Point.Y - World.Offset.Y;

            double endX = roadNodeTo.X - World.Offset.X;
            double endY = roadNodeTo.Y - World.Offset.Y;

            double distance;

            this.currentRoad = currentRoad;

            bool foundStartAndEnd = false;

            if (currentRoad is GeometryFeature g)
            {
                if (g.Geometry is LineString lineString)
                {
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
                        EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Failed Adding Car with From Node{nodeFrom.X:F2},{nodeFrom.Y:F2} and To Node {nodeTo.X:F2}, {nodeTo.Y:F2} and road with Start {lineString.StartPoint:F2} and End {lineString.EndPoint:F2}"));
                        IsCreated = false;
                    }
                }
            }

            if (foundStartAndEnd)
            {
                //if(distance>5)

                startPos = new Vector2((float)startX, (float)startY);
                Vector2 endPos = new Vector2((float)endX, (float)endY);

                Vector2 directionNormalized = Vector2.Normalize(new Vector2(endPos.X - startPos.X, endPos.Y - startPos.Y));

                startingAngle = MathF.Atan2(directionNormalized.Y, directionNormalized.X);

                //move start and end so car is using
                //right hand lane and heading to right hand lane

                startPos = new Vector2(startPos.X + MathF.Cos(startingAngle + Helper.Deg2Rad(-90.0f)) * Helper.DefaultLaneWidth * 0.75f, startPos.Y + MathF.Sin(startingAngle + Helper.Deg2Rad(-90.0f)) * Helper.DefaultLaneWidth * 0.75f);
                endPos = new Vector2(endPos.X + MathF.Cos(startingAngle + Helper.Deg2Rad(-90.0f)) * Helper.DefaultLaneWidth * 0.75f, endPos.Y + MathF.Sin(startingAngle + Helper.Deg2Rad(-90.0f)) * Helper.DefaultLaneWidth * 0.75f);

                FRect rect = new FRect(startPos.X - carLength / 2, startPos.Y - carWidth / 2, carLength, carWidth);

                rayCastDelegate = RayCastCallback;

                queryFilter.categoryBits = 0xFFFF;
                queryFilter.maskBits = 0xFFFF;

                body = new VehicleBody(rect);

                b2Rot rot = b2Rot.FromAngle(startingAngle);
                B2Api.b2Body_SetTransform(body.BodyId, startPos, rot);

                IsCreated = true;
            }
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

        public void SetVehicleInFront(float howFar)
        {
            metersFromCarInFront = rayDistance * howFar;

            carInFront = true;
        }

        public void ResetVehicleInFront()
        {
            carInFront = false;
        }

        public void Update()
        {
            Pos = B2Api.b2Body_GetPosition(body.BodyId);

            if (Vector2.Distance(startPos, Pos) >= distanceToTravel)
            {
                b2Rot rot = b2Rot.FromAngle(startingAngle);
                B2Api.b2Body_SetTransform(body.BodyId, startPos, rot);
                Pos = B2Api.b2Body_GetPosition(body.BodyId);
            }

            feature.Point.X = (double)Pos.X + World.Offset.X;
            feature.Point.Y = (double)Pos.Y + World.Offset.Y;

            B2Api.b2Body_SetAngularVelocity(body.BodyId, 0);
            angle = B2Api.b2Body_GetRotation(body.BodyId);

            try
            {
                feature["Angle"] = Helper.Rad2Deg(angle.GetAngle());
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Vehicle missing angle feature + {ex.ToString()}"));
            }
            feature.Modified();

            if (isWaiting)
            {
                if (Sim.SimTime < whenToStopWaiting)
                {
                    waitingOnIntersection = true;
                }
                else
                {
                    waitingOnIntersection = false;
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
                updateToSpeed = Math.Clamp(updateToSpeed + acceleration, 0, speedLimit);
            }
            if (state == VehicleStates.Decelerating)
            {
                updateToSpeed = Math.Clamp(updateToSpeed - deceleration, 0, speedLimit);
            }

            if (updateToSpeed > 0)
            {
                float speedToUseMs = Helper.Kmh2Ms(updateToSpeed);
                Vector2 velocityToSetMs = new Vector2(angle.c * speedToUseMs, angle.s * speedToUseMs);

                B2Api.b2Body_SetLinearVelocity(body.BodyId, velocityToSetMs);
            }
            else
            {
                B2Api.b2Body_SetLinearVelocity(body.BodyId, Vector2.Zero);
            }

            kmh = Helper.MS2Kmh(Vector2.Dot(B2Api.b2Body_GetLinearVelocity(body.BodyId), new Vector2(angle.c, angle.s)));

            if (carInFront == false)
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

            ray = new Ray(Pos, new Vector2(angle.c * rayDistance, angle.s * rayDistance));
            ResetVehicleInFront();

            B2Api.b2World_CastRay(World.WorldId, ray.Start, ray.Translation, queryFilter, rayCastDelegate, 1);
        }

        public void SetPos(Vector2 pos)
        {
            b2Rot rot = b2Rot.FromAngle(0);
            B2Api.b2Body_SetTransform(body.BodyId, pos, rot);
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
                SetVehicleInFront(fraction);
            }

            return 1.0f;
        }
    }
}