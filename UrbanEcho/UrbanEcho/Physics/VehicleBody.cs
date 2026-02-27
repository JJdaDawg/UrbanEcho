using Box2dNet;
using Box2dNet.Interop;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Helpers;
using UrbanEcho.Models;
using UrbanEcho.Sim;
using static Box2dNet.Interop.B2Api;

namespace UrbanEcho.Physics
{
    public class VehicleBody : IDisposable
    {
        public b2ShapeId ShapeId;

        public b2BodyId BodyId;

        private Vehicle parent;

        private nint intPtr;

        public VehicleBody(Vehicle parent, FRect rect)
        {
            this.parent = parent;
            // Define the vehicle body.
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new Vector2(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
            bodyDef.type = b2BodyType.b2_kinematicBody;

            bodyDef.linearDamping = 0.01f;

            bodyDef.angularDamping = 0.01f;
            BodyId = b2CreateBody(World.WorldId, bodyDef);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Polygon polygon = Helper.CreatePolygon([new(-rect.Width / 2, -rect.Height / 2), new(-rect.Width / 2, rect.Height / 2), new(rect.Width / 2, rect.Height / 2), new(rect.Width / 2, -rect.Height / 2)]);
            shapeDef.isSensor = true;
            shapeDef.filter.categoryBits = (ulong)ShapeCategories.Vehicle;
            intPtr = NativeHandle.Alloc(parent);
            shapeDef.userData = intPtr;

            ShapeId = b2CreatePolygonShape(BodyId, in shapeDef, in polygon);
        }

        public void Dispose()
        {
            if (intPtr != nint.Zero)
            {
                NativeHandle.Free(intPtr);
            }
        }
    }
}