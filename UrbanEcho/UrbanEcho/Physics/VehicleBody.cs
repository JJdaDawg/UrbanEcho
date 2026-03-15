using Box2dNet;
using Box2dNet.Interop;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
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

        private Vector2[] vertices;
        private bool bodyCreated;

        private bool isDisposed = false;

        public VehicleBody(Vehicle parent, FRect rect)
        {
            this.parent = parent;
            // Define the vehicle body.
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new Vector2(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
            bodyDef.type = b2BodyType.b2_dynamicBody;// .b2_kinematicBody;

            bodyDef.linearDamping = 0.01f;

            bodyDef.angularDamping = 0.01f;
            BodyId = b2CreateBody(World.WorldId, bodyDef);
            bodyCreated = true;
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Polygon polygon = Helper.CreatePolygon([new(-rect.Width / 2, -rect.Height / 2), new(-rect.Width / 2, rect.Height / 2), new(rect.Width / 2, rect.Height / 2), new(rect.Width / 2, -rect.Height / 2)]);
            vertices = new Vector2[polygon.count];
            for (int i = 0; i < polygon.count; i++)
            {
                vertices[i] = polygon.vertices(i);
            }
            shapeDef.isSensor = true;

            shapeDef.filter.categoryBits = (ulong)ShapeCategories.Vehicle;
            intPtr = NativeHandle.Alloc(parent);
            shapeDef.userData = intPtr;
            //shapeDef.enableSensorEvents = true;
            ShapeId = b2CreatePolygonShape(BodyId, in shapeDef, in polygon);
        }

        public Vector2[] GetShapeVertices()
        {
            return vertices;
        }

        public bool IsDisposed()
        {
            return isDisposed;
        }

        public void Dispose()
        {
            try
            {
                if (!isDisposed)
                {
                    if (intPtr != nint.Zero)
                    {
                        NativeHandle.Free(intPtr);
                    }
                    if (bodyCreated)
                    {
                        if (World.Created)
                        {
                            //  B2Api.b2DestroyBody(BodyId); Destroy world instead so program doesn't hang up when destroying each body
                        }
                        else
                        {
                            EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Tried to free vehicle body after box2d world destroyed"));
                        }
                        bodyCreated = false;
                        isDisposed = true;
                    }
                }
            }
            catch (Exception e)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(),
               $"Failed to destroy vehicle body: {BodyId.ToString()}"));
            }
        }
    }
}