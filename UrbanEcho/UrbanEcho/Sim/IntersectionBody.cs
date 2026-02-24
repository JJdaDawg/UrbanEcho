using Box2dNet;
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
    public class IntersectionBody : IDisposable
    {
        public b2ShapeId ShapeId;

        public b2BodyId BodyId;

        private static float defaultSize = 5.0f * Helper.MapCorrection;

        public Vector2[]? Points;

        private RoadIntersection parent;

        private IntPtr intPtr;

        public IntersectionBody(RoadIntersection parent)
        {
            this.parent = parent;
            // Define the car body.
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = parent.Center;
            bodyDef.type = b2BodyType.b2_staticBody;

            BodyId = b2CreateBody(World.WorldId, bodyDef);
            b2ShapeDef shapeDef = B2Api.b2DefaultShapeDef();
            shapeDef.isSensor = true;

            intPtr = NativeHandle.Alloc(parent);
            shapeDef.userData = intPtr;

            shapeDef.filter.categoryBits = (ulong)ShapeCategories.Intersection;
            if (parent.Connections.Count > 4 || parent.Connections.Count <= 1)
            {
                Points = CircleOfPoints();
                b2Polygon polygon = Helper.CreatePolygon(Points);
                ShapeId = B2Api.b2CreatePolygonShape(BodyId, in shapeDef, in polygon);
            }
            else
            {
                Points = PointsFromRoadConnections();
                b2Polygon polygon = Helper.CreatePolygon(Points);
                ShapeId = B2Api.b2CreatePolygonShape(BodyId, in shapeDef, in polygon);
            }
        }

        private Vector2[] CircleOfPoints()
        {
            Vector2[] points = new Vector2[8];

            for (int i = 0; i < 8; i++)
            {
                b2Rot angle = b2Rot.FromAngle(Helper.Deg2Rad(0));
                points[i].X = MathF.Cos(Helper.Deg2Rad((float)(i) * 360.0f / 8.0f)) * defaultSize;
                points[i].Y = MathF.Sin(Helper.Deg2Rad((float)(i) * 360.0f / 8.0f)) * defaultSize;
            }

            return points;
        }

        private Vector2[] PointsFromRoadConnections()
        {
            Vector2[] points = new Vector2[parent.Connections.Count * 2];
            int pointsAdded = 0;
            foreach (ConnectionData connection in parent.Connections)
            {
                Vector2 startPoint = (connection.IndexValue == 0) ? connection.RoadSegment.Pos[1] : connection.RoadSegment.Pos[0];
                Vector2 endPoint = parent.Center;
                Vector2 direction = endPoint - startPoint;

                float angle = MathF.Atan2(direction.Y, direction.X);
                float roadDirectionAt45Angle = angle + Helper.Deg2Rad(45.0f);

                points[pointsAdded++] = new Vector2(defaultSize * MathF.Cos(roadDirectionAt45Angle), defaultSize * MathF.Sin(roadDirectionAt45Angle));
                points[pointsAdded++] = new Vector2(defaultSize * MathF.Cos(-roadDirectionAt45Angle), defaultSize * MathF.Sin(-roadDirectionAt45Angle));
            }

            return points;
        }

        public RoadIntersection GetParent()
        {
            return parent;
        }

        public void Dispose()
        {
            if (intPtr != IntPtr.Zero)
            {
                NativeHandle.Free(intPtr);
            }
        }
    }
}