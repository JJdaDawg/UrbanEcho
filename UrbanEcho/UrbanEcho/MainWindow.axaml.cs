using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using BruTile;
using BruTile.MbTiles;
using BruTile.Wms;
using Mapsui;
using Mapsui.Extensions.Provider;
using Mapsui.Layers;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Providers;
using Mapsui.Rendering.Skia;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using Mapsui.UI;
using Mapsui.UI.Avalonia;
using NetTopologySuite.Geometries;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UrbanEcho.Sim;
using UrbanEcho.ViewModels;
using Layer = Mapsui.Layers.Layer;

namespace UrbanEcho;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainViewModel vm = new MainViewModel();
        DataContext = vm;
        SetupMap.Init(MyMapControl);
        Simulation.SetMapControl(MyMapControl, vm);
        Simulation.SimTask = Task.Factory.StartNew(new Action(Simulation.Run), Simulation.Cts.Token);
    }
}