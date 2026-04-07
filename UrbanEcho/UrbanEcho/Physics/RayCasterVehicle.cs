using Box2dNet;
using Box2dNet.Interop;
using BruTile.Wms;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Models;
using UrbanEcho.Sim;

namespace UrbanEcho.Physics
{
    /// <summary>
    /// Class for a raycasting if a vehicle is ahead
    /// </summary>
    public class RayCasterVehicle
    {
        private b2QueryFilter queryFilter = B2Api.b2DefaultQueryFilter();
        private Vehicle parent;

        public RayCasterVehicle(Vehicle parent)
        {
            queryFilter.categoryBits = 0xFFFF;
            queryFilter.maskBits = (ulong)ShapeCategories.Vehicle;
            this.parent = parent;
        }

        /// <summary>
        /// Does a ray cast to check if a vehicle is ahead
        /// </summary>
        /// <returns>Returns true if a vehicle is ahead and the distance <see cref="float"/>  </returns>
        public (bool hit, float distance) DoRayCast(Vector2 rayCastStartPos, float currentFloatAngle, bool usingShorterRayForTurn, float speedMultiplier, b2ShapeId casterShapeId)
        {
            bool rayHit = false;
            float rayDistance;
            float hitDistance = 0;

            //use shorter ray if turning inside a intersection so it doesn't stop for vehicles
            //that are waiting on other side of turn while car is making a right
            if (!usingShorterRayForTurn)
            {
                //use longer ray if cleared intersection
                rayDistance = speedMultiplier + 5.0f + ((SimManager.Instance.SimSpeed - 1) * 3.5f);
            }
            else
            {
                rayDistance = 5.0f;
            }

            b2Rot angleForRay = b2Rot.FromAngle(currentFloatAngle);

            Ray ray = new Ray(rayCastStartPos, new Vector2(angleForRay.c * rayDistance, angleForRay.s * rayDistance));

            queryFilter.maskBits = (ulong)ShapeCategories.Vehicle;
            b2RayResult rayResult = B2Api.b2World_CastRayClosest(World.WorldId, ray.Start, ray.Translation, queryFilter);

            if (rayResult.hit)
            {
                float distance = rayDistance * rayResult.fraction;

                if (rayResult.shapeId != casterShapeId)
                {
                    b2Filter filter = B2Api.b2Shape_GetFilter(rayResult.shapeId);
                    if (filter.categoryBits == (ulong)ShapeCategories.Vehicle)
                    {
                        (rayHit, hitDistance) = SetVehicleInFrontCount(rayResult.shapeId, distance, casterShapeId);
                    }
                    else
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Should query vehicle not something else"));
                    }
                }
                else
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Shape collided with self, raycast start point incorrect"));
                }
            }

            return (rayHit, hitDistance);
        }

        /// <summary>
        /// Checks if the vehicle detected should count as a ray cast hit
        /// </summary>
        /// <returns>Returns true if a vehicle is ahead and the distance <see cref="float"/>  </returns>
        private (bool hit, float distance) SetVehicleInFrontCount(b2ShapeId shapeId, float howFar, b2ShapeId casterShapeId)
        {
            bool hitCounted = false;
            float hitDistance = 0;
            if (shapeId != casterShapeId)
            {
                IntPtr intPtr = B2Api.b2Shape_GetUserData(shapeId);

                Vehicle otherVehicle = NativeHandle.GetObject<Vehicle>(intPtr);

                if (parent.IsCollidedVehicleSameEdgeOrIntersection(otherVehicle))
                {
                    hitDistance = howFar;
                    hitCounted = true;
                }
            }
            return (hitCounted, hitDistance);
        }
    }
}