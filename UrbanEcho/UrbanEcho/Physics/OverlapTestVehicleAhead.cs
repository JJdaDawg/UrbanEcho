using Box2dNet;
using Box2dNet.Interop;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Models;
using UrbanEcho.Sim;

namespace UrbanEcho.Physics
{
    public class OverlapTestVehicleAhead
    {
        private b2OverlapResultFcn overlapDelegateVehicleAhead;
        private b2QueryFilter queryFilter = B2Api.b2DefaultQueryFilter();
        private b2ShapeId casterShapeId;
        private Vehicle parent;
        private bool anotherVehicleAhead;

        public OverlapTestVehicleAhead(Vehicle parent)
        {
            this.parent = parent;
            queryFilter.categoryBits = 0xFFFF;
            queryFilter.maskBits = (ulong)ShapeCategories.Vehicle;

            overlapDelegateVehicleAhead = OverlapCallbackVehicleAhead;
            anotherVehicleAhead = false;
        }

        public bool DoOverlapTest(b2ShapeProxy b2ShapeProxy, b2ShapeId casterShapeId)
        {
            anotherVehicleAhead = false;
            this.casterShapeId = casterShapeId;
            B2Api.b2World_OverlapShape(World.WorldId, b2ShapeProxy, queryFilter, overlapDelegateVehicleAhead, 1);
            return anotherVehicleAhead;
        }

        private bool OverlapCallbackVehicleAhead(b2ShapeId shapeId, nint context)
        {
            bool returnValue = true;

            if (shapeId != casterShapeId)
            {
                IntPtr intPtr = B2Api.b2Shape_GetUserData(shapeId);

                Vehicle otherVehicle = NativeHandle.GetObject<Vehicle>(intPtr);

                if (parent.IsCollidedVehicleSameEdgeOrIntersection(otherVehicle))
                {
                    anotherVehicleAhead = true;
                    returnValue = false;
                }
            }
            return returnValue;//return false to terminate
        }
    }
}