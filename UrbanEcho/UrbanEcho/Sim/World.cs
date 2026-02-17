using Box2dNet.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using static Box2dNet.Interop.B2Api;

namespace UrbanEcho.Sim
{
    public enum ShapeCategories
    {
        Vehicle = 0x00000001,
        Intersection = 0x00000002
    }

    public static class World
    {
        public static b2WorldId WorldId;

        // Define the gravity vector.

        public static void Init()
        {
            Vector2 gravity = new Vector2(0.0f, 0.0f);

            // Construct a world object, which will hold and simulate the rigid bodies.

            //b2CreateWorld world(gravity);
            b2WorldDef worldDef = b2DefaultWorldDef();
            worldDef.gravity = gravity;
            WorldId = B2Api.b2CreateWorld(worldDef);
        }
    }
}