using Box2dNet;
using Box2dNet.Interop;
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
    public class RayCasterIntersection
    {
        private b2QueryFilter queryFilter = B2Api.b2DefaultQueryFilter();
        private Vehicle parent;

        public RayCasterIntersection(Vehicle parent)
        {
            queryFilter.categoryBits = 0xFFFF;
            queryFilter.maskBits = (ulong)ShapeCategories.Intersection;
            this.parent = parent;
        }

        public (bool hit, b2ShapeId intersectionShapeId) DoRayCast(Vector2 rayCastStartPos, float currentFloatAngle, bool usingShorterRayForTurn, float speedMultiplier, b2ShapeId casterShapeId)
        {
            bool rayHit = false;
            float rayDistance;
            b2ShapeId intersectionShapeId = new b2ShapeId();

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

            rayDistance = 5.0f + ((SimManager.Instance.SimSpeed - 1) * 3.5f);
            b2Rot angleForRay = b2Rot.FromAngle(currentFloatAngle);

            Ray ray = new Ray(rayCastStartPos, new Vector2(angleForRay.c * rayDistance, angleForRay.s * rayDistance));

            queryFilter.maskBits = (ulong)ShapeCategories.Intersection;
            b2RayResult rayResultIntersect = B2Api.b2World_CastRayClosest(World.WorldId, ray.Start, ray.Translation, queryFilter);

            if (rayResultIntersect.hit)
            {
                float distance = rayDistance * rayResultIntersect.fraction;

                if (rayResultIntersect.shapeId != casterShapeId)
                {
                    b2Filter filter = B2Api.b2Shape_GetFilter(rayResultIntersect.shapeId);
                    if (filter.categoryBits == (ulong)ShapeCategories.Intersection)
                    {
                        rayHit = true;
                        intersectionShapeId = rayResultIntersect.shapeId;
                    }
                    else
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Should query intersection not something else"));
                    }
                }
                else
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Shape collided with self, raycast start point incorrect"));
                }
            }

            return (rayHit, intersectionShapeId);
        }
    }
}