using Box2dNet.Interop;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Helpers;
using static Box2dNet.Interop.B2Api;

namespace UrbanEcho.Sim
{
    public class VehicleBody
    {
        public b2ShapeId ShapeId;

        public b2BodyId BodyId;

        public VehicleBody(FRect rect)
        {
            // Define the vehicle body.
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new Vector2(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
            bodyDef.type = b2BodyType.b2_kinematicBody;

            bodyDef.linearDamping = 0.01f;

            bodyDef.angularDamping = 0.01f;
            BodyId = b2CreateBody(World.WorldId, bodyDef);
            b2ShapeDef shapeDef = B2Api.b2DefaultShapeDef();
            b2Polygon polygon = Helper.CreatePolygon([new(-rect.Width / 2, -rect.Height / 2), new(-rect.Width / 2, rect.Height / 2), new(rect.Width / 2, rect.Height / 2), new(rect.Width / 2, -rect.Height / 2)]);
            shapeDef.isSensor = true;
            shapeDef.filter.categoryBits = (ulong)ShapeCategories.Vehicle;

            ShapeId = B2Api.b2CreatePolygonShape(BodyId, in shapeDef, in polygon);
        }
    }
}