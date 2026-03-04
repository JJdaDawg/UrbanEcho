using Avalonia.Controls.Shapes;
using FluentIcons.Avalonia;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UrbanEcho.Styles
{
    public class RoadStyles
    {
        private bool useOutline;
        private bool useAADT;

        public RoadStyles(bool useOutline, bool useAADT)
        {
            this.useOutline = useOutline;
            this.useAADT = useAADT;
        }

        public void SetUseAADT(bool useAADT)
        {
            this.useAADT = useAADT;
        }

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

            if (Sim.Sim.MyMap.Navigator.Viewport.Resolution > 0.01f)
            {
                resolutionFactor = (float)(1.0f / Sim.Sim.MyMap.Navigator.Viewport.Resolution);
            }

            double pavementWidth = 0;

            /*
            pavementWidth = Helpers.Helper.TryGetFeatureKVPToDouble(gf, "PAVEMENT_W", 1);

            if (pavementWidth < 4)
            {
                int lanes = Helpers.Helper.TryGetFeatureKVPToInt(gf, "LANES", 2);

                pavementWidth = lanes * Helpers.Helper.DefaultLaneWidth;
            }*/
            //Try with just using number of lanes instead
            int lanes = Helpers.Helper.TryGetFeatureKVPToInt(gf, "LANES", 2);
            pavementWidth = lanes * Helpers.Helper.DefaultLaneWidth * Helpers.Helper.ExtraPavementFactor;

            style.Line.Width = pavementWidth * resolutionFactor;
            style.Outline.Width = style.Line.Width * 0.1f;

            if (useOutline == false)
            {
                style.Outline.Width = 0;
            }

            //TODO: make AADT not hardcoded
            if (gf.Fields.Contains("AADT") && useAADT)
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

                    style.Line.Color = cb.GetColor(normalizedValue);
                }
                else
                {
                    style.Line.Color = new Color(148, 148, 148);
                }
            }
            else
            {
                style.Line.Color = new Color(148, 148, 148);
            }

            return style;
        }

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