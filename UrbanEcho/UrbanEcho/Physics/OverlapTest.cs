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
using UrbanEcho.Models;
using UrbanEcho.Sim;

namespace UrbanEcho.Physics
{
    /// <summary>
    /// Class for performing a overlap test if a vehicle is touching another vehicle
    /// </summary>
    public class OverlapTest
    {
        private b2QueryFilter queryFilter = B2Api.b2DefaultQueryFilter();
        private bool overlappedDuringThisScan;
        private int insideAnotherVehicleCount;

        private Vehicle parent;
        private b2ShapeId casterShapeId;
        private b2OverlapResultFcn overlapDelegateVehicle;

        public OverlapTest(Vehicle parent)
        {
            this.parent = parent;
            queryFilter.categoryBits = 0xFFFF;
            queryFilter.maskBits = (ulong)ShapeCategories.Vehicle;
            overlappedDuringThisScan = false;
            insideAnotherVehicleCount = 0;

            overlapDelegateVehicle = OverlapCallbackVehicle;
        }

        /// <summary>
        /// Does a overlap test to see if two vehicles are touching
        /// </summary>
        public bool DoOverlapTest(b2ShapeProxy b2ShapeProxy, b2ShapeId casterShapeId)
        {
            this.casterShapeId = casterShapeId;
            bool insideAnotherVehicle = false;

            queryFilter.maskBits = (ulong)ShapeCategories.Vehicle;

            overlappedDuringThisScan = false;
            B2Api.b2World_OverlapShape(World.WorldId, b2ShapeProxy, queryFilter, overlapDelegateVehicle, 1);
            if (!(overlappedDuringThisScan))
            {
                insideAnotherVehicleCount = 0;
            }

            if (insideAnotherVehicleCount > 1)//Only have it indicate if overlapping after two tests
            {
                insideAnotherVehicle = true;
            }

            return insideAnotherVehicle;
        }

        /// <summary>
        /// Resets the vehicle count
        /// </summary>
        public void ResetInsideAnotherVehicleCount()
        {
            insideAnotherVehicleCount = 0;
        }

        /// <summary>
        /// Call back function that is called if overlap is detected
        /// </summary>
        private bool OverlapCallbackVehicle(b2ShapeId shapeId, nint context)
        {
            bool returnValue = true;

            if (shapeId != casterShapeId)
            {
                IntPtr intPtr = B2Api.b2Shape_GetUserData(shapeId);

                Vehicle otherVehicle = NativeHandle.GetObject<Vehicle>(intPtr);

                if (parent.IsCollidedVehicleSameEdgeOrIntersection(otherVehicle))
                {
                    insideAnotherVehicleCount++;
                    overlappedDuringThisScan = true;

                    returnValue = false;
                }
            }
            return returnValue;//return false to terminate
        }
    }
}