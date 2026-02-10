using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Providers;
using NetTopologySuite.Geometries;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho.Helpers
{
    //was internal function in clipping class
    //https://github.com/Mapsui/Mapsui/blob/main/Mapsui.Rendering.Skia/Functions/ClippingFunctions.cs

    public class Helper
    {
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

        public static IEnumerable<IFeature> GetFeatures(IProvider source)
        {
            MRect rect = new MRect(double.MinValue, double.MinValue, double.MaxValue, double.MaxValue);
            FetchInfo fetch = new FetchInfo(new MSection(rect, 10000));

            Task<IEnumerable<IFeature>> features = source.GetFeaturesAsync(fetch);
            return features.Result;
        }

        public static List<IFeature> GetRoadNetworkFeatures(IProvider source)
        {
            List<IFeature> roadNetworklist = new List<IFeature>();

            if (source != null)
            {
                roadNetworklist = Helper.GetFeatures(source).ToList();
            }

            /* Example of how to read from the list
            if (roadNetworklist.Count > 0)
            {
                if (roadNetworklist[0] is BaseFeature f)
                {
                    object? o = f["LANES"];
                    if (o != null)
                    {
                        string? test = o.ToString();
                    }
                }
            }*/

            return roadNetworklist;
        }
    }
}