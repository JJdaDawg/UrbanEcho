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
using UrbanEcho.ViewModels;
using static Box2dNet.Interop.B2Api;

namespace UrbanEcho.Physics
{
    public class IntersectionBody : IDisposable
    {
        public b2ShapeId ShapeId;

        public b2BodyId BodyId;

        //Used for when no connection points line up with intersection
        private static float defaultSize = 30.0f;

        public b2Polygon polygon;

        private RoadIntersection parent;

        private nint intPtr;

        private Vector2[] vertices;

        private bool bodyCreated;

        public IntersectionBody(RoadIntersection parent, List<(Vector2 pos, float width)> connectingPoints)
        {
            this.parent = parent;
            // Define the intersection body.
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = parent.Center;
            bodyDef.type = b2BodyType.b2_staticBody;

            BodyId = b2CreateBody(World.WorldId, bodyDef);
            bodyCreated = true;
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.isSensor = true;

            intPtr = NativeHandle.Alloc(parent);
            shapeDef.userData = intPtr;

            shapeDef.filter.categoryBits = (ulong)ShapeCategories.Intersection;
            if (connectingPoints.Count > 4 || connectingPoints.Count <= 1)
            {
                Vector2[]? Points = CircleOfPoints();
                polygon = Helper.CreatePolygon(Points);
                vertices = new Vector2[polygon.count];
                for (int i = 0; i < polygon.count; i++)
                {
                    vertices[i] = polygon.vertices(i);
                }
                ShapeId = b2CreatePolygonShape(BodyId, in shapeDef, in polygon);
            }
            else
            {
                Vector2[]? Points = PointsFromRoadConnections(connectingPoints);
                polygon = Helper.CreatePolygon(Points);
                vertices = new Vector2[polygon.count];
                for (int i = 0; i < polygon.count; i++)
                {
                    vertices[i] = polygon.vertices(i);
                }
                ShapeId = b2CreatePolygonShape(BodyId, in shapeDef, in polygon);
            }
        }

        public Vector2[] GetShapeVertices()
        {
            return vertices;
        }

        private Vector2[] CircleOfPoints()
        {
            Vector2[] points = new Vector2[8];

            for (int i = 0; i < 8; i++)
            {
                b2Rot angle = b2Rot.FromAngle(Helper.Deg2Rad(0));
                points[i].X = MathF.Cos(Helper.Deg2Rad(i * 360.0f / 8.0f)) * defaultSize;
                points[i].Y = MathF.Sin(Helper.Deg2Rad(i * 360.0f / 8.0f)) * defaultSize;
            }

            return points;
        }

        private Vector2[] PointsFromRoadConnections(List<(Vector2 pos, float width)> connectingPoints)
        {
            Vector2[] points = new Vector2[connectingPoints.Count * 2];
            int pointsAdded = 0;
            foreach ((Vector2 pos, float width) connection in connectingPoints)
            {
                float widthToUse = connection.width;
                Vector2 startPoint = parent.Center;
                Vector2 endPoint = connection.pos;
                Vector2 direction = endPoint - startPoint;

                float angle = MathF.Atan2(direction.Y, direction.X);
                if (float.IsNaN(angle))
                {
                    angle = 0;
                }
                float roadDirectionAt45Angle = angle + Helper.Deg2Rad(45.0f);
                float roadDirectionAtNeg45Angle = angle + Helper.Deg2Rad(-45.0f);

                points[pointsAdded++] = new Vector2(widthToUse * MathF.Cos(roadDirectionAt45Angle), widthToUse * MathF.Sin(roadDirectionAt45Angle));
                points[pointsAdded++] = new Vector2(widthToUse * MathF.Cos(roadDirectionAtNeg45Angle), widthToUse * MathF.Sin(roadDirectionAtNeg45Angle));
            }

            return points;
        }

        public void Dispose()
        {
            try
            {
                if (intPtr != nint.Zero)
                {
                    NativeHandle.Free(intPtr);
                }
                if (bodyCreated)
                {
                    B2Api.b2DestroyBody(BodyId);
                    bodyCreated = false;
                }
            }
            catch (Exception e)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(),
               $"Failed to destroy intersection body: {BodyId.ToString()}"));
            }
        }
    }
}