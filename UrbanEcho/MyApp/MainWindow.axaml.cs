using Avalonia.Controls;

using BruTile;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using System.IO;

namespace MyApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        //https://mapsui.com/v5/samples/#/DataFormats/ShapefileWithLabels

        string trafficVolumesPath = Path.Combine("Resources\\ShapeFiles\\Traffic_Volumes", "Traffic_Volumes.shp");
        ShapeFile trafficVolumes = new ShapeFile(trafficVolumesPath);
        //MRect? panBounds = trafficVolumes.GetExtent();

        RasterizingLayer layer = new RasterizingLayer(CreateLayer(trafficVolumes));

        MRect? panBounds = layer.Extent;

        //https://github.com/Mapsui/Mapsui/blob/main/Samples/Mapsui.Samples.Common/Maps/Navigation/KeepWithinExtentSample.cs

        if (panBounds != null)
        {
            //Viewport viewport = new Viewport(extent.Centroid.X, extent.Centroid.Y, extent.Width, extent.Height);
            MyMapControl.Map.Navigator.Limiter = new Mapsui.Limiting.ViewportLimiterKeepWithinExtent();

            MyMapControl.Map.Navigator.OverridePanBounds = panBounds;

            MyMapControl.Map?.Layers.Add(layer);
        }
    }

    private static Layer CreateLayer(IProvider source)
    {
        Layer layer = new Layer();
        layer.Name = "New Layer";
        layer.DataSource = source;

        //https://github.com/Mapsui/Mapsui/blob/42b59e9dad1fd9512f0114f8c8a3fd3f5666d330/Samples/Mapsui.Samples.Common/Maps/CustomStyleSample.cs#L16-L51

        VectorStyle style = new VectorStyle();

        Pen pen = new Pen(Color.Black, 2);
        style.Line = pen;
        style.Opacity = 1.0f;

        layer.Style = style;

        return layer;
    }
}