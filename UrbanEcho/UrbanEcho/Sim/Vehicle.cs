using Box2dNet;
using Box2dNet.Interop;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Helpers;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        //from google normal car 4m long and width 1.7m
        private float carLength = 4.0f;

        private float carWidth = 1.7f;

        private VehicleBody body;

        private Vector2 startPos = new Vector2(350, 100);
        private Vector2 endPos = new Vector2(600, 100);

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

        private float textOffsetPos;

        public Vehicle(float textOffsetPos)
        {
            this.textOffsetPos = textOffsetPos;

            FRect rect = new FRect(startPos.X - carLength / 2, startPos.Y - carWidth / 2, carLength, carWidth);

            rayCastDelegate = RayCastCallback;

            queryFilter.categoryBits = 0xFFFF;
            queryFilter.maskBits = 0xFFFF;

            body = new VehicleBody(rect);
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

            if (Pos.X > endPos.X)
            {
                b2Rot rot = b2Rot.FromAngle(0);
                B2Api.b2Body_SetTransform(body.BodyId, startPos, rot);
            }

            B2Api.b2Body_SetAngularVelocity(body.BodyId, 0);
            angle = B2Api.b2Body_GetRotation(body.BodyId);

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