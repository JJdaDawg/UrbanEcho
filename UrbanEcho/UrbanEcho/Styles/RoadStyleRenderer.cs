using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Logging;
using Mapsui.Nts;
using Mapsui.Rendering;
using Mapsui.Rendering.Caching;
using Mapsui.Rendering.Skia;
using Mapsui.Rendering.Skia.Extensions;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using NetTopologySuite.Geometries;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Extensions;

//Modified VectorStyleRenderer for custom style
//https://github.com/Mapsui/Mapsui/blob/main/Mapsui.Rendering.Skia/SkiaStyles/VectorStyleRenderer.cs

//https://github.com/Mapsui/Mapsui/blob/01dcb06134a76b62d41e0b6d3a54151f9b57fc9e/Mapsui.Rendering.Skia/SkiaStyles/LineStringRenderer.cs
namespace UrbanEcho.Styles
{
    public class RoadStyleRenderer : ISkiaStyleRenderer
    {
        public bool Draw(SKCanvas canvas, Viewport viewport, ILayer layer, IFeature feature, IStyle style, RenderService renderService, long iteration)
        {
            if (style is RoadStyle roadStyle)
            {
                float opacity = (float)(layer.Opacity * style.Opacity);

                if (feature is GeometryFeature gf)
                {
                    Pen pen = new Pen();
                    Pen outlinePen = new Pen();
                    //TODO: make pavement not hardcoded
                    if (gf.Fields.Contains("PAVEMENT_W"))
                    {
                        if (double.TryParse(gf["PAVEMENT_W"]?.ToString(), out double pavementWidth))
                        {
                            if (viewport.Resolution > 0.01f)
                            {
                                if (pavementWidth < 4)
                                {
                                    pen.Width = 4 / viewport.Resolution;
                                    outlinePen.Width = pen.Width * 0.1f;
                                }
                                else
                                {
                                    pen.Width = pavementWidth / viewport.Resolution;
                                    outlinePen.Width = pen.Width * 0.1f;
                                }
                            }
                            else
                            {
                                pen.Width = 1;
                                outlinePen.Width = pen.Width * 0.1f;
                            }
                        }
                        else
                        {
                            pen.Width = 1;
                            outlinePen.Width = pen.Width * 0.1f;
                        }

                        if (roadStyle.UseOutline == false)
                        {
                            outlinePen.Width = 0;
                        }
                        if (roadStyle.Line != null)
                        {
                            roadStyle.Line.Width = pen.Width;
                        }
                        if (roadStyle.Outline != null)
                        {
                            roadStyle.Outline.Width = outlinePen.Width;
                        }
                    }
                    //TODO: make AADT not hardcoded
                    if (gf.Fields.Contains("AADT"))
                    {
                        if (double.TryParse(gf["AADT"]?.ToString(), out double aadtValue))
                        {
                            ColorBlend cb = ColorBlend.TwoColors(Color.LimeGreen, Color.Red);
                            double minAADTValue = 0;
                            double maxAADTValue = 50000;

                            double normalizedValue = 0;
                            if (maxAADTValue - minAADTValue > 0)
                            {
                                normalizedValue = (minAADTValue + aadtValue) / (maxAADTValue - minAADTValue);
                            }
                            Math.Clamp(normalizedValue, 0.0, 1.0);

                            roadStyle.Line.Color = cb.GetColor(normalizedValue);
                        }
                        else
                        {
                            roadStyle.Line.Color = Color.Grey;
                        }
                    }
                    else
                    {
                        roadStyle.Line.Color = Color.Grey;
                    }
                }

                void DrawGeometry(Geometry? geometry, int position = 0)
                {
                    switch (geometry)
                    {
                        case GeometryCollection collection:
                            {
                                for (var index = 0; index < collection.Count; index++)
                                {
                                    var child = collection[index];
                                    DrawGeometry(child, index);
                                }
                            }
                            break;

                        case LineString lineString:
                            DrawLineString(canvas, viewport, roadStyle, feature, lineString, opacity, renderService, position);
                            break;

                        case null: // A geometry may be null. It might be a mistake but logging in the renderer would flood the log.
                            break;

                        default:
                            throw new ArgumentException($"Unknown geometry type: {geometry?.GetType()}, Layer: {layer.Name}");
                    }
                }

                try
                {
                    switch (feature)
                    {
                        case GeometryFeature geometryFeature:
                            DrawGeometry(geometryFeature?.Geometry);
                            break;

                        default:
                            Logger.Log(LogLevel.Warning, $"{nameof(VectorStyleRenderer)} can not render feature of type '{feature.GetType()}', Layer: {layer.Name}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, ex.Message, ex);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static void DrawLineString(SKCanvas canvas, Viewport viewport, RoadStyle roadStyle,
        IFeature feature, LineString lineString, float opacity, RenderService renderService, int position)
        {
            LineString lineString2 = lineString;
            if (roadStyle == null)
            {
                return;
            }

            MRect item = viewport.ToExtent();
            double rotation = viewport.Rotation;
            float item2 = (float)(roadStyle.Line?.Width ?? 1.0);
            if (!roadStyle.Line.IsVisible())
            {
                return;
            }

            using (CacheTracker<SKPath> path = renderService.VectorCache.GetOrCreate((feature.Id, position, item, rotation, item2), ToPath))
            {
                Pen? outline = roadStyle.Outline;
                if (outline != null)
                {
                    if (outline.Width > 0.0)
                    {
                        double valueOrDefault = (roadStyle?.Outline?.Width + roadStyle?.Outline?.Width + roadStyle?.Line?.Width).GetValueOrDefault(1.0);
                        using CacheTracker<SKPaint> paint = renderService.VectorCache.GetOrCreate((roadStyle?.Outline, (float)valueOrDefault, opacity), (Func<(Pen?, float?, float), SKPaint>)CreateSkPaint);
                        canvas.DrawPath(path, paint);
                    }
                }

                using CacheTracker<SKPaint> paint2 = renderService.VectorCache.GetOrCreate<(Pen, float?, float), SKPaint>((roadStyle.Line, null, opacity), CreateSkPaint);
                canvas.DrawPath(path, paint2);
            }

            SKPath ToPath((long featureId, int position, MRect extent, double rotation, float lineWidth) valueTuple)
            {
                var result = lineString.ToSkiaPath(viewport, viewport.ToSkiaRect(), valueTuple.lineWidth);
                _ = result.Bounds;
                _ = result.TightBounds;
                return result;
            }
        }

        //Modified version of
        //https://github.com/Mapsui/Mapsui/blob/main/Mapsui.Experimental.Rendering.Skia/SkiaStyles/LineStringRenderer.cs
        private static SKPaint CreateSkPaint((Pen? pen, float? width, float opacity) valueTuple)
        {
            var pen = valueTuple.pen;
            var opacity = valueTuple.opacity;

            float lineWidth = valueTuple.width ?? 1;
            var lineColor = new Color();

            var strokeCap = PenStrokeCap.Round;
            var strokeJoin = StrokeJoin.Round;

            var strokeStyle = PenStyle.Solid;
            float[]? dashArray = null;
            float dashOffset = 0;

            if (pen != null)
            {
                lineWidth = valueTuple.width ?? (float)pen.Width;
                lineColor = pen.Color;

                strokeStyle = pen.PenStyle;
                dashArray = pen.DashArray;
                dashOffset = pen.DashOffset;
            }

            var paint = new SKPaint { IsAntialias = true };
            //https://skia.org/docs/user/api/skblendmode_overview/#Blend_Mode
            paint.BlendMode = SKBlendMode.Src;
            paint.IsStroke = true;
            paint.StrokeWidth = lineWidth;
            paint.Color = lineColor.ToSkia(opacity);
            paint.StrokeCap = strokeCap.ToSkia();
            paint.StrokeJoin = strokeJoin.ToSkia();

            paint.PathEffect = strokeStyle != PenStyle.Solid
                ? strokeStyle.ToSkia(lineWidth, dashArray, dashOffset)
                : null;
            return paint;
        }
    }
}