using BruTile.MbTiles;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanEcho
{
    public static class CreateLayers
    {
        //https://github.com/BruTile/BruTile
        public static TileLayer CreateMbTilesLayer(string path, string name)
        {
            MbTilesTileSource mbTilesTileSource = new MbTilesTileSource(new SQLiteConnectionString(path, true));
            TileLayer mbTilesLayer = new TileLayer(mbTilesTileSource) { Name = name };
            //TODO: Figure out how to check if this failed and show error

            return mbTilesLayer;
        }

        //not currently used (for geotiff files)
        public static ILayer CreateBackLayer(IProvider source, string name)
        {
            source.CRS = "EPSG:4326";

            ProjectingProvider projectingProvider = new ProjectingProvider(source)
            {
                CRS = "EPSG:3857"
            };

            Layer layer = new Layer(name);
            layer.DataSource = projectingProvider;
            //TODO: Figure out how to check if this failed and show error

            return layer;
        }

        public static Layer CreateIntersectionsLayer(IProvider source, string name)
        {
            source.CRS = "EPSG:4326";

            ProjectingProvider projectingProvider = new ProjectingProvider(source)
            {
                CRS = "EPSG:3857"
            };

            Layer layer = new Layer(name);

            layer.Opacity = 1.0f;

            layer.MaxVisible = 2;

            layer.DataSource = projectingProvider;
            //TODO: Figure out how to check if this failed and show error

            IntersectionStyles intersectionsStyle = new IntersectionStyles();

            layer.Style = intersectionsStyle.CreateThemeStyle();

            return layer;
        }

        public static Layer CreateRoadLayer(IProvider source, string name, bool doOutline, bool showAADT)
        {
            source.CRS = "EPSG:4326";

            ProjectingProvider projectingProvider = new ProjectingProvider(source)
            {
                CRS = "EPSG:3857"
            };

            Layer layer = new Layer(name);
            layer.DataSource = projectingProvider;
            //TODO: Figure out how to check if this failed and show error

            //https://github.com/Mapsui/Mapsui/blob/42b59e9dad1fd9512f0114f8c8a3fd3f5666d330/Samples/Mapsui.Samples.Common/Maps/CustomStyleSample.cs#L16-L51

            RoadStyle style = new RoadStyle();
            if (style.Line != null)
            {
                style.Line.PenStrokeCap = PenStrokeCap.Square;
                style.Line.StrokeJoin = StrokeJoin.Bevel;
                style.Line.StrokeMiterLimit = 10.0f;
            }

            if (style.Outline != null)
            {
                style.Outline.PenStrokeCap = PenStrokeCap.Square;
                style.Outline.StrokeJoin = StrokeJoin.Bevel;
                style.Outline.StrokeMiterLimit = 10.0f;
            }

            style.UseOutline = doOutline;
            style.ShowAADT = showAADT;

            style.Opacity = 1.0f;
            style.Line = new Pen();
            style.Line.Color = Color.LightGrey;
            style.Outline = new Pen();
            style.Outline.Color = Color.GhostWhite;

            layer.Style = style;
            layer.Opacity = 1.0f;

            return layer;
        }
    }
}