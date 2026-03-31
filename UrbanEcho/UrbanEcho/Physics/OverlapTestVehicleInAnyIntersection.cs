using Box2dNet.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Models;

namespace UrbanEcho.Physics
{
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

        public bool DoOverlapTest(b2ShapeProxy b2ShapeProxy, b2ShapeId casterShapeId)
        {
            thisVehicleIsInAIntersection = false;
            this.casterShapeId = casterShapeId;
            B2Api.b2World_OverlapShape(World.WorldId, b2ShapeProxy, queryFilter, overlapDelegateThisVehicleInAnyIntersection, 1);
            return thisVehicleIsInAIntersection;
        }

        private bool OverlapCallbackThisVehicleInIntersection(b2ShapeId shapeId, nint context)
        {
            thisVehicleIsInAIntersection = true;

            return false;
        }
    }
}