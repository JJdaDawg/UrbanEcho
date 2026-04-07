using Box2dNet.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Models;

namespace UrbanEcho.Physics
{
    /// <summary>
    /// Class for performing a test if any vehicle is in the intersection
    /// </summary>
    public class OverlapTestVehicleInAnyIntersection
    {
        private b2OverlapResultFcn overlapDelegateThisVehicleInAnyIntersection;
        private b2QueryFilter queryFilter = B2Api.b2DefaultQueryFilter();
        private b2ShapeId casterShapeId;
        private Vehicle parent;
        private bool thisVehicleIsInAIntersection;

        public OverlapTestVehicleInAnyIntersection(Vehicle parent)
        {
            this.parent = parent;
            queryFilter.categoryBits = 0xFFFF;
            queryFilter.maskBits = (ulong)ShapeCategories.Intersection;
            thisVehicleIsInAIntersection = false;
            overlapDelegateThisVehicleInAnyIntersection = OverlapCallbackThisVehicleInIntersection;
        }

        /// <summary>
        /// Does a overlap test to see if the vehicle is in a intersection
        /// </summary>
        public bool DoOverlapTest(b2ShapeProxy b2ShapeProxy, b2ShapeId casterShapeId)
        {
            thisVehicleIsInAIntersection = false;
            this.casterShapeId = casterShapeId;
            B2Api.b2World_OverlapShape(World.WorldId, b2ShapeProxy, queryFilter, overlapDelegateThisVehicleInAnyIntersection, 1);
            return thisVehicleIsInAIntersection;
        }

        /// <summary>
        /// Call back function that is called if overlap is detected
        /// </summary>
        private bool OverlapCallbackThisVehicleInIntersection(b2ShapeId shapeId, nint context)
        {
            thisVehicleIsInAIntersection = true;

            return false;
        }
    }
}