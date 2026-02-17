using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Chrome;
using BruTile;
using BruTile.MbTiles;
using BruTile.Wms;
using FluentAvalonia.UI.Windowing;
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
using Mapsui.Utilities;
using Mapsui.Widgets;
using Mapsui.Widgets.InfoWidgets;
using NetTopologySuite.Geometries;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Sim;
using UrbanEcho.ViewModels;
using Layer = Mapsui.Layers.Layer;

namespace UrbanEcho;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();

        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
        MainViewModel vm = new MainViewModel();
        DataContext = vm;
        SetupMap.Init(vm.Map.MyMap);
        Sim.Sim.SetMainViewModel(vm);
        Sim.Sim.SimTask = Task.Factory.StartNew(new Action(Sim.Sim.Run), Sim.Sim.Cts.Token);
    }
}