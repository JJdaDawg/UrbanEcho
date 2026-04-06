using Box2dNet.Interop;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Models;

namespace UrbanEcho.Physics
{
    /// <summary>
    /// Class for performing a test if intersection is occupied
    /// </summary>
    public class OverlapTestIntersectionOccupied
    {
        private b2OverlapResultFcn overlapDelegateIntersection;
        private b2QueryFilter queryFilter = B2Api.b2DefaultQueryFilter();
        private b2ShapeId casterShapeId;
        private Vehicle parent;
        private bool intersectionOccupied = false;

        public OverlapTestIntersectionOccupied(Vehicle parent)
        {
            this.parent = parent;
            queryFilter.categoryBits = 0xFFFF;
            queryFilter.maskBits = (ulong)ShapeCategories.Vehicle;
            overlapDelegateIntersection = OverlapCallbackIntersection;
        }

        /// <summary>
        /// Does a overlap test to see if the vehicle is in a intersection
        /// </summary>
        public bool DoOverlapTest(b2ShapeProxy b2ShapeProxy, b2ShapeId casterShapeId)
        {
            intersectionOccupied = false;
            this.casterShapeId = casterShapeId;
            B2Api.b2World_OverlapShape(World.WorldId, b2ShapeProxy, queryFilter, overlapDelegateIntersection, 1);
            return intersectionOccupied;
        }

        /// <summary>
        /// Call back function that is called if overlap is detected
        /// </summary>
        private bool OverlapCallbackIntersection(b2ShapeId shapeId, nint context)
        {
            bool keepCheckingOverlap = true;

            if (shapeId != casterShapeId)
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
    }
}