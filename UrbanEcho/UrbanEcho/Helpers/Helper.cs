using Box2dNet.Interop;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Providers;
using NetTopologySuite.Geometries;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Physics;

namespace UrbanEcho.Helpers
{
    //was internal function in clipping class
    //https://github.com/Mapsui/Mapsui/blob/main/Mapsui.Rendering.Skia/Functions/ClippingFunctions.cs

    public static class Helper
    {
        //1.0f / MathF.Cos(43.4511f*(MathF.PI / 180.0f))
        public const float MapCorrection = 1.0f;// 1.37748f; try without since the correction only applies to east to west

        public const float DefaultLaneWidth = 3.75f * MapCorrection;//in meters
        public const int NumberOfVehicleGroups = 1; //spread out the updates so we can have better fps
        public const float ExtraPavementFactor = 1.25f;

        public static Point MakePrecisePoint(
        Point p,
        PrecisionModel precision)
        {
            return new Point(precision.MakePrecise(p.X), precision.MakePrecise(p.Y));
        }

        /// <summary>
        /// Convert a list of Mapsui points in world coordinates to SKPoint in screen coordinates
        /// </summary>
        /// <param name="viewport">The Viewport that is used for the conversions.</param>
        /// <param name="points">List of points in Mapsui world coordinates</param>
        /// <returns>List of screen coordinates in SKPoint</returns>
        public static List<SKPoint> WorldToScreen(Viewport viewport, IEnumerable<Coordinate>? points)
        {
            var result = new List<SKPoint>();
            if (points == null)
                return result;

            foreach (var point in points)
            {
                var (screenX, screenY) = viewport.WorldToScreenXY(point.X, point.Y);
                result.Add(new SKPoint((float)screenX, (float)screenY));
            }

            return result;
        }

        private static IEnumerable<IFeature>? TryGetFeatures(IProvider source)
        {
            try
            {
                MRect rect = new MRect(double.MinValue, double.MinValue, double.MaxValue, double.MaxValue);
                FetchInfo fetch = new FetchInfo(new MSection(rect, 10000));

                Task<IEnumerable<IFeature>> features = source.GetFeaturesAsync(fetch);
                return features.Result;
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Unable to get features {ex.ToString()}"));

                return null;
            }
        }

        public static Vector2 Convert2Box2dWorldPosition(double x, double y)
        {
            double worldPosX = x - World.Offset.X;
            double worldPosY = y - World.Offset.Y;

            return new Vector2((float)worldPosX, (float)worldPosY);
        }

        public static Vector2 Convert2Box2dWorldPosition(MPoint mPoint)
        {
            double worldPosX = mPoint.X - World.Offset.X;
            double worldPosY = mPoint.Y - World.Offset.Y;

            return new Vector2((float)worldPosX, (float)worldPosY);
        }

        public static Vector2 Convert2Box2dWorldPosition(Point point)
        {
            double worldPosX = point.X - World.Offset.X;
            double worldPosY = point.Y - World.Offset.Y;

            return new Vector2((float)worldPosX, (float)worldPosY);
        }

        public static List<IFeature> GetFeatures(IProvider source)
        {
            List<IFeature> featureList = new List<IFeature>();

            if (source != null)
            {
                IEnumerable<IFeature>? features = Helper.TryGetFeatures(source);
                if (features != null)
                {
                    featureList = features.ToList();
                }
            }

            return featureList;
        }

        public static double TryGetFeatureKVPToDouble(IFeature feature, string key, double defaultValue)
        {
            double value = defaultValue;

            if (feature.Fields.Contains(key))
            {
                if (double.TryParse(feature[key]?.ToString(), out double valueOut))
                {
                    value = valueOut;
                }
            }

            return value;
        }

        public static float TryGetFeatureKVPToFloat(IFeature feature, string key, float defaultValue)
        {
            float value = defaultValue;

            if (feature.Fields.Contains(key))
            {
                if (float.TryParse(feature[key]?.ToString(), out float valueOut))
                {
                    value = valueOut;
                }
            }

            return value;
        }

        public static int TryGetFeatureKVPToInt(IFeature feature, string key, int defaultValue)
        {
            int value = defaultValue;

            if (feature.Fields.Contains(key))
            {
                if (Int32.TryParse(feature[key]?.ToString(), out int valueOut))
                {
                    value = valueOut;
                }
            }

            return value;
        }

        public static string TryGetFeatureKVPToString(IFeature feature, string key, string defaultValue)
        {
            string value = defaultValue;

            if (feature.Fields.Contains(key))
            {
                if (feature[key] != null)
                {
                    string? keyValue = feature[key]?.ToString();
                    if (keyValue != null)
                    {
                        value = keyValue;
                    }
                }
            }

            return value;
        }

        public static b2Polygon CreatePolygon(Vector2[] corners)
        {
            if (corners.Length is < 3 or > 8) throw new Exception($"Corner count ({corners.Length}) must be within [3,8].");

            return B2Api.b2MakePolygon(B2Api.b2ComputeHull(corners, corners.Length), 0);
        }

        public static float Rad2Deg(float r)
        {
            return r * (180.0f / MathF.PI);
        }

        public static float Deg2Rad(float d)
        {
            return d * (MathF.PI / 180.0f);
        }

        public static Vector2 MS2Kmh(Vector2 v)
        {
            return v * 3.6f;
        }

        public static float MS2Kmh(float s)
        {
            return s * 3.6f;
        }

        public static Vector2 Kmh2Ms(Vector2 v)
        {
            return v / 3.6f;
        }

        public static float Kmh2Ms(float s)
        {
            return s / 3.6f;
        }

        public static float DoMapCorrection(float value)
        {
            return value * MapCorrection;
        }

        public static double DoMapCorrection(double value)
        {
            return value * MapCorrection;
        }
    }
}