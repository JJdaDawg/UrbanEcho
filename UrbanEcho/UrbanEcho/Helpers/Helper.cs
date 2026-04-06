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
using UrbanEcho.Graph;
using UrbanEcho.Physics;

namespace UrbanEcho.Helpers
{
    /// <summary>
    /// Helper class contains helper functions used throughout program.
    /// </summary>
    public static class Helper
    {
        public const float DefaultLaneWidth = 3.75f;//in meters
        public const int NumberOfVehicleGroups = 1; //spread out the updates so we can have better fps
        public const float ExtraPavementFactor = 1.25f; //Sets how the width of roads is shown

        /// <summary>
        /// Gets a a list of features from a <see cref="IProvider"/>
        /// </summary>
        /// <returns>Returns the list of features <see cref="IFeature"/> </returns>
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
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Unable to get features {ex.ToString()}"));

                return null;
            }
        }

        /// <summary>
        /// Given a map coordinate returns a <see cref="Vector2"/> box2d world coordinate (Box2d can only use float)
        /// </summary>
        /// <returns>Returns the position <see cref="Vector2"/> </returns>
        public static Vector2 Convert2Box2dWorldPosition(double x, double y)
        {
            double worldPosX = x - World.Offset.X;
            double worldPosY = y - World.Offset.Y;

            return new Vector2((float)worldPosX, (float)worldPosY);
        }

        /// <summary>
        /// Given a map coordinate as a <see cref="Point"/> returns a <see cref="Vector2"/> box2d world coordinate (Box2d can only use float)
        /// </summary>
        /// <returns>Returns the position <see cref="Vector2"/> </returns>
        public static Vector2 Convert2Box2dWorldPosition(Point point)
        {
            double worldPosX = point.X - World.Offset.X;
            double worldPosY = point.Y - World.Offset.Y;

            return new Vector2((float)worldPosX, (float)worldPosY);
        }

        /// <summary>
        /// Gets a a list of features from a <see cref="IProvider"/>
        /// </summary>
        /// <returns>Returns the list of features <see cref="IFeature"/> </returns>
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

        /// <summary>
        /// Tries to get the double value from a feature field <see cref="IFeature"/>
        /// returns a default value if unable to get the value
        /// </summary>
        /// <returns>Returns value as a <see cref="double"/> </returns>
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

        /// <summary>
        /// Tries to get the floating point value from a feature field <see cref="IFeature"/>
        /// returns a default value if unable to get the value
        /// </summary>
        /// <returns>Returns value as a <see cref="float"/> </returns>
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

        /// <summary>
        /// Tries to get the int value from a feature field <see cref="IFeature"/>
        /// returns a default value if unable to get the value
        /// </summary>
        /// <returns>Returns value as a <see cref="int"/> </returns>
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

        /// <summary>
        /// Tries to get the string value from a feature field <see cref="IFeature"/>
        /// returns a default value if unable to get the value
        /// </summary>
        /// <returns>Returns value as a <see cref="string"/> </returns>
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

        /// <summary>
        /// Creates a box2d <see cref="b2Polygon"/> used for body shapes using a  <see cref="Vector2"/> array
        /// </summary>
        /// <returns>Returns the polygon created <see cref="b2Polygon"/> </returns>
        public static b2Polygon CreatePolygon(Vector2[] corners)
        {
            if (corners.Length is < 3 or > 8) throw new Exception($"Corner count ({corners.Length}) must be within [3,8].");

            return B2Api.b2MakePolygon(B2Api.b2ComputeHull(corners, corners.Length), 0);
        }

        /// <summary>
        /// Takes a value as radians and returns degrees
        /// </summary>
        /// <returns>Returns the value as degrees <see cref="float"/> </returns>
        public static float Rad2Deg(float r)
        {
            return r * (180.0f / MathF.PI);
        }

        /// <summary>
        /// Takes a value as degrees and returns radians
        /// </summary>
        /// <returns>Returns the value as radians <see cref="float"/> </returns>
        public static float Deg2Rad(float d)
        {
            return d * (MathF.PI / 180.0f);
        }

        /// <summary>
        /// Takes a value as meters per second and converts it to kilometers per hour
        /// </summary>
        /// <returns>Returns the value as kilometers per hour <see cref="float"/> </returns>
        public static float MS2Kmh(float s)
        {
            return s * 3.6f;
        }

        /// <summary>
        /// Takes a value as kilometers per hour  and converts it to meters per second
        /// </summary>
        /// <returns>Returns the value as meters per second <see cref="float"/> </returns>
        public static float Kmh2Ms(float s)
        {
            return s / 3.6f;
        }

        /// <summary>
        /// Takes a <see cref="RoadType"/> value and returns a priority value, the higher value returned
        /// the more priority the road type has.
        /// </summary>
        /// <returns>Returns the priority value <see cref="int"/> </returns>
        public static int GetPriority(RoadType value)
        {
            int returnValue = 0;
            if (value == RoadType.Unknown)
            {
                returnValue = 0;
            }
            else if (value == RoadType.AlleywayLane)
            {
                returnValue = 1;
            }
            else if (value == RoadType.Private)
            {
                returnValue = 2;
            }
            else if (value == RoadType.CulDeSac)
            {
                returnValue = 3;
            }
            else if (value == RoadType.LocalStreet)
            {
                returnValue = 4;
            }
            else if (value == RoadType.Roundabout)
            {
                returnValue = 5;
            }
            else if (value == RoadType.Ramp)
            {
                returnValue = 6;
            }
            else if (value == RoadType.Collector)
            {
                returnValue = 7;
            }
            else if (value == RoadType.Arterial)
            {
                returnValue = 8;
            }
            else if (value == RoadType.Expressway)
            {
                returnValue = 9;
            }
            else if (value == RoadType.Freeway)
            {
                returnValue = 10;
            }

            return returnValue;
        }

        /// <summary>
        /// Takes a <see cref="string"/> value representing the road type and returns a priority value, the higher value returned
        /// the more priority the road type has.
        /// </summary>
        /// <returns>Returns the priority value <see cref="int"/> </returns>
        public static int GetPriority(string value)
        {
            int returnValue = 0;
            if (value == "NULL")
            {
                returnValue = 0;
            }
            else if (value == "Alleyway / Lane")
            {
                returnValue = 1;
            }
            else if (value == "Private")
            {
                returnValue = 2;
            }
            else if (value == "Cul - de - Sac")
            {
                returnValue = 3;
            }
            else if (value == "Local Street")
            {
                returnValue = 4;
            }
            else if (value == "Roundabout")
            {
                returnValue = 5;
            }
            else if (value == "Ramp")
            {
                returnValue = 6;
            }
            else if (value == "Collector")
            {
                returnValue = 7;
            }
            else if (value == "Arterial")
            {
                returnValue = 8;
            }
            else if (value == "Expressway / Highway")
            {
                returnValue = 9;
            }
            else if (value == "Freeway")
            {
                returnValue = 10;
            }

            return returnValue;
        }
    }
}