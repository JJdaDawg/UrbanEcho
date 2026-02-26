using Box2dNet.Interop;
using Mapsui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Box2dNet.Interop.B2Api;

namespace UrbanEcho.Physics
{
    public enum ShapeCategories
    {
        Vehicle = 0x00000001,
        Intersection = 0x00000002
    }

    public static class World
    {
        public static b2WorldId WorldId;

        public static bool Created = false;

        public static MPoint Offset = new MPoint();

        public static void Init(double offsetX, double offsetY)
        {
            Offset = new MPoint(offsetX, offsetY);

            Vector2 gravity = new Vector2(0.0f, 0.0f);

            // Construct a world object, which will hold all the shapes and box2d bodies.
            b2WorldDef worldDef = b2DefaultWorldDef();
            worldDef.gravity = gravity;
            WorldId = b2CreateWorld(worldDef);
            Created = true;
        }
    }
}