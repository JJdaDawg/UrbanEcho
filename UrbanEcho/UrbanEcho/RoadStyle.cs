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

namespace UrbanEcho
{
    public class RoadStyle : IStyle
    {
        public double MinVisible { get; set; } = 0;
        public double MaxVisible { get; set; } = double.MaxValue;
        public bool Enabled { get; set; } = true;
        public float Opacity { get; set; } = 1.0f;

        public bool UseOutline = false;

        public bool ShowAADT = false;

        public Pen? Line { get; set; }

        public Pen? Outline { get; set; }

        public RoadStyle()
        {
            Outline = new Pen();

            Outline.Color = Color.Gray;
            Outline.Width = 1.0;
            Outline.PenStyle = PenStyle.Solid;

            Line = new Pen();

            Line.Color = Color.Black;
            Line.Width = 1.0;
            Line.PenStyle = PenStyle.Solid;
        }
    }
}