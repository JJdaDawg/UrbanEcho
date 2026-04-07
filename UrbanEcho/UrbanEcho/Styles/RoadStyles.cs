using Avalonia.Controls.Shapes;
using FluentIcons.Avalonia;
using FluentIcons.Common.Internals;
using Mapsui;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using System;
using System.Collections.Generic;
using System.Linq;
using UrbanEcho.FileManagement;
using UrbanEcho.Helpers;
using UrbanEcho.Sim;
using UrbanEcho.UI;

namespace UrbanEcho.Styles
{
    /// <summary>
    /// Road Styles used for displaying a road
    /// </summary>
    public class RoadStyles
    {
        private bool useOutline;

        public RoadStyles(bool useOutline)
        {
            this.useOutline = useOutline;
        }

        /// <summary>
        /// Creates a Vector Style used for displaying the road
        /// </summary>
        /// <returns>Returns a <see cref="VectorStyle"/> </returns>
        private VectorStyle CreateVectorStyle(GeometryFeature gf)
        {
            VectorStyle style = new VectorStyle();

            style.Outline = new Pen();

            style.Outline.Color = Color.AntiqueWhite;
            style.Outline.Width = 1.0;
            style.Outline.PenStyle = PenStyle.Solid;

            style.Outline.PenStrokeCap = PenStrokeCap.Round;
            style.Outline.StrokeJoin = StrokeJoin.Round;

            style.Line = new Pen();

            style.Line.Color = Color.Black;
            style.Line.Width = 1.0;
            style.Line.PenStyle = PenStyle.Solid;

            style.Line.PenStrokeCap = PenStrokeCap.Round;
            style.Line.StrokeJoin = StrokeJoin.Round;

            float resolutionFactor = 100;

            if (MainWindow.Instance.GetMap().Navigator.Viewport.Resolution > 0.01f)
            {
                resolutionFactor = (float)(1.0f / MainWindow.Instance.GetMap().Navigator.Viewport.Resolution);
            }

            double pavementWidth = 0;

            int lanes = Helpers.Helper.TryGetFeatureKVPToInt(gf, "LANES", 2);
            pavementWidth = lanes * Helpers.Helper.DefaultLaneWidth * Helpers.Helper.ExtraPavementFactor;

            style.Line.Width = pavementWidth * resolutionFactor;
            style.Outline.Width = style.Line.Width * 0.1f;

            if (useOutline == false)
            {
                style.Outline.Width = 0;
            }

            int vehicleCount = 0;
            int isClosed = 0;

            string key = Helper.TryGetFeatureKVPToString(gf, "OBJECTID", "");
            if (!string.IsNullOrEmpty(key))
            {
                if (SimManager.Instance.RoadFeatures.TryGetValue(key, out IFeature? dictionaryFeature))
                {
                    if (dictionaryFeature != null)
                    {
                        vehicleCount = Helper.TryGetFeatureKVPToInt(dictionaryFeature, "VehicleCount", 0);

                        isClosed = Helper.TryGetFeatureKVPToInt(dictionaryFeature, "Closed", 0);

                        if (isClosed == 0)
                        {
                            if (vehicleCount > 0 && ProjectLayers.IsVolumeVisible)
                            {
                                ColorBlend cb = ColorBlend.TwoColors(Color.LimeGreen, Color.Red);
                                double minValue = 0;
                                double maxValue = SimManager.Instance.RoadWithMaxVolume;
                                double value = vehicleCount;

                                double normalizedValue = 0;
                                if (maxValue - minValue > 0)
                                {
                                    normalizedValue = (value - minValue) / (maxValue - minValue);
                                }
                                normalizedValue = Math.Clamp(normalizedValue, 0.0, 1.0);

                                style.Line.Color = cb.GetColor(normalizedValue);
                            }
                        }
                        else
                        {
                            style.Line.Color = Color.Black;
                        }
                    }
                }
            }

            if (isClosed == 0)
            {
                if (!ProjectLayers.IsVolumeVisible || vehicleCount == 0)
                {
                    style.Line.Color = new Color(148, 148, 148);
                }
            }
            else
            {
                style.Line.Color = Color.Black;
            }

            if (useOutline)
            {
                if (ProjectLayers.IsTrafficSpeedVisible)
                {
                    double speed = 0;
                    key = Helper.TryGetFeatureKVPToString(gf, "OBJECTID", "");
                    if (!string.IsNullOrEmpty(key))
                    {
                        if (SimManager.Instance.RoadFeatures.TryGetValue(key, out IFeature? dictionaryFeature))
                        {
                            if (dictionaryFeature != null)
                            {
                                speed = Helper.TryGetFeatureKVPToDouble(dictionaryFeature, "Speed", 0);

                                if (speed > 0)
                                {
                                    ColorBlend cb = ColorBlend.TwoColors(Color.FireBrick, Color.LimeGreen);
                                    double minValue = SimManager.Instance.MinForShowSpeed;
                                    double maxValue = SimManager.Instance.MaxForShowSpeed;
                                    double value = speed;

                                    double normalizedValue = 0;
                                    if (maxValue - minValue > 0)
                                    {
                                        normalizedValue = (value - minValue) / (maxValue - minValue);
                                    }
                                    normalizedValue = Math.Clamp(normalizedValue, 0.0, 1.0);

                                    style.Outline.Color = cb.GetColor(normalizedValue);
                                }
                            }
                        }
                    }

                    if (speed == 0)
                    {
                        style.Outline.Color = Color.AntiqueWhite;
                    }
                }
                else
                {
                    style.Outline.Color = Color.AntiqueWhite;
                }
            }

            return style;
        }

        /// <summary>
        /// Creates a theme style type of style that can be shown differently depending
        /// on features displayed
        /// </summary>
        /// <returns>Returns a <see cref="ThemeStyle"/> </returns>
        //https://github.com/Mapsui/Mapsui/blob/main/Samples/Mapsui.Samples.Common/Maps/Styles/ThemeStyleSample.cs
        public ThemeStyle CreateThemeStyle()
        {
            return new ThemeStyle(f =>
            {
                if (f is GeometryFeature geometryFeature)
                {
                    return CreateVectorStyle(geometryFeature);
                }
                else
                {
                    return null;
                }
            });
        }
    }
}