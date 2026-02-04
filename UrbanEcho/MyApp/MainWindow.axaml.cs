using Avalonia.Controls;
using BruTile;
using BruTile.MbTiles;
using Mapsui;
using Mapsui.Extensions.Provider;
using Mapsui.Layers;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Providers;
using Mapsui.Rendering.Skia;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using Mapsui.UI;
using Mapsui.UI.Avalonia;
using NetTopologySuite.Geometries;
using SQLite;

using System.Collections.Generic;
using System.IO;

namespace MyApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MyMapControl.Map.CRS = "EPSG:3857"; // The Map CRS needs to be set

        //https://mapsui.com/v5/samples/#/DataFormats/ShapefileWithLabels

        string roadNetworkPath = Path.Combine("Resources\\ShapeFiles\\Road_Network", "Road_Network.shp");
        ShapeFile roadNetwork = new ShapeFile(roadNetworkPath);

        string intersectionsPath = Path.Combine("Resources\\ShapeFiles\\intersections_kitchener", "intersections_kitchener.shp");
        ShapeFile intersections = new ShapeFile(intersectionsPath);

        //TileLayer openStreetMapLayer = Mapsui.Tiling.OpenStreetMap.CreateTileLayer();

        //for loading geotiff (too slow so using mbtiles)
        //string backgroundImagePath = Path.Combine("Resources\\Rasters", "LandCover.tif");
        //https://github.com/Mapsui/Mapsui/blob/main/Tests/Mapsui.Tests/GeoTiff/GeoTiffProviderTests.cs

        //follow this when exporting file
        //https://gis.stackexchange.com/questions/213785/saving-raster-as-tif-with-tfw-worldfile
        //
        //using GeoTiffProvider geoTiffProvider = new GeoTiffProvider(backgroundImagePath, null);

        //land cover shows as 7 different colors (for grass, trees, pavement, water etc)
        //TileLayer backgroundMBTile = CreateMbTilesLayer(Path.GetFullPath(Path.Combine("Resources\\Rasters", "LandCover19.mbtiles")), "regular");
        TileLayer backgroundMBTile = CreateMbTilesLayer(Path.GetFullPath(Path.Combine("Resources\\Rasters", "Aerial2.mbtiles")), "regular");
        RasterizingLayer layer = new RasterizingLayer(CreateRoadLayer(roadNetwork, "Road Outline", true, false));
        RasterizingLayer layer2 = new RasterizingLayer(CreateRoadLayer(roadNetwork, "Roads", false, true));
        RasterizingLayer layer3 = new RasterizingLayer(CreateIntersectionsLayer(intersections, "Intersections"));

        MRect? panBounds = layer.Extent;

        panBounds.Multiply(5.0f);
        //https://github.com/Mapsui/Mapsui/blob/main/Samples/Mapsui.Samples.Common/Maps/Navigation/KeepWithinExtentSample.cs

        if (panBounds != null)
        {
            MyMapControl.Map.BackColor = Color.White;

            MyMapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(layer.Extent.MinX + (layer.Extent.MaxX - layer.Extent.MinX) / 2,
                layer.Extent.MinY + (layer.Extent.MaxY - layer.Extent.MinY) / 2), 15.0);
            MyMapControl.Map.Navigator.OverridePanBounds = panBounds;
            MyMapControl.Map.Navigator.OverrideZoomBounds = new MMinMax(0.1, 50);
        }

        MapRenderer.RegisterStyleRenderer(typeof(RoadStyle), new RoadStyleRenderer());

        //https://mapsui.com/documentation/projections.html

        //openStreetMapLayer.Opacity = 0.9f;
        //MyMapControl.Map?.Layers.Add(openStreetMapLayer);
        //MyMapControl.Map?.Layers.Add(CreateBackLayer(geoTiffProvider, "land cover"));
        MyMapControl.Map?.Layers.Add(backgroundMBTile);
        MyMapControl.Map?.Layers.Add(layer);
        MyMapControl.Map?.Layers.Add(layer2);
        MyMapControl.Map?.Layers.Add(layer3);
    }

    //https://github.com/BruTile/BruTile
    public static TileLayer CreateMbTilesLayer(string path, string name)
    {
        MbTilesTileSource mbTilesTileSource = new MbTilesTileSource(new SQLiteConnectionString(path, true));
        TileLayer mbTilesLayer = new TileLayer(mbTilesTileSource) { Name = name };

        return mbTilesLayer;
    }

    //not currently used (for geotiff files)
    private static ILayer CreateBackLayer(IProvider source, string name)
    {
        source.CRS = "EPSG:4326";

        ProjectingProvider projectingProvider = new ProjectingProvider(source)
        {
            CRS = "EPSG:3857"
        };

        Layer layer = new Layer(name);
        layer.DataSource = projectingProvider;
        layer.Style = null;
        return layer;
    }

    private static Layer CreateIntersectionsLayer(IProvider source, string name)
    {
        source.CRS = "EPSG:4326";

        ProjectingProvider projectingProvider = new ProjectingProvider(source)
        {
            CRS = "EPSG:3857"
        };

        Layer layer = new Layer(name);

        layer.Opacity = 1.0f;

        if (layer.Style != null)
        {
            layer.Style.MaxVisible = 2;
        }

        layer.DataSource = projectingProvider;

        return layer;
    }

    private static Layer CreateRoadLayer(IProvider source, string name, bool doOutline, bool showAADT)
    {
        source.CRS = "EPSG:4326";

        ProjectingProvider projectingProvider = new ProjectingProvider(source)
        {
            CRS = "EPSG:3857"
        };

        Layer layer = new Layer(name);
        layer.DataSource = projectingProvider;

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