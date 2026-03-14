using Avalonia.Controls;
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
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Services;
using UrbanEcho.Sim;
using UrbanEcho.UI;
using UrbanEcho.ViewModels;
using Layer = Mapsui.Layers.Layer;

namespace UrbanEcho;

public partial class MainWindow : AppWindow, IPanelService
{
    private GridLength _lastConsoleHeight = new GridLength(200);
    private GridLength _lastRightPanelWidth = new GridLength(300);
    private GridLength _lastPropertiesHeight = new GridLength(1, GridUnitType.Star);
    private GridLength _lastProjectExplorerHeight = new GridLength(1, GridUnitType.Star);
    private bool _projectExplorerOpen = true;
    private bool _propertiesOpen = true;

    private MainViewModel vm;

    public static MainWindow Instance { get; private set; } = null!;

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
        var fileDialogService = new FileDialogService(this);
        var mapFeatureService = new MapFeatureService();
        var vehicleService = new VehicleService();
        vm = new MainViewModel(this, fileDialogService, mapFeatureService, vehicleService);
        DataContext = vm;

        SetupMap.Init(vm.Map.MyMap);

        SimManager.Instance.SimTask = Task.Factory.StartNew(new Action(SimManager.Instance.Run), SimManager.Instance.Cts.Token);
        UIUpdate.UITask = Task.Factory.StartNew(new Action(UIUpdate.Run), UIUpdate.Cts.Token);
    }

    public MainViewModel GetMainViewModel()
    {
        return vm;
    }

    public Map GetMap()
    {
        return vm.Map.MyMap;
    }

    public void ToggleConsole(bool open)
    {
        var splitterRow = LeftGrid.RowDefinitions[1];
        var consoleRow = LeftGrid.RowDefinitions[2];
        if (open)
        {
            splitterRow.Height = new GridLength(5);
            consoleRow.Height = _lastConsoleHeight;
            ConsoleSplitter.IsVisible = true;
        }
        else
        {
            _lastConsoleHeight = consoleRow.Height;
            splitterRow.Height = new GridLength(0);
            consoleRow.Height = new GridLength(0);
            ConsoleSplitter.IsVisible = false;
        }
    }

    public void ToggleRightPanel(bool open)
    {
        var splitterCol = MainContentGrid.ColumnDefinitions[1];
        var col = MainContentGrid.ColumnDefinitions[2];
        if (open)
        {
            splitterCol.Width = new GridLength(4);
            col.Width = _lastRightPanelWidth;
            RightPanelSplitter.IsVisible = true;
        }
        else
        {
            _lastRightPanelWidth = col.Width;
            splitterCol.Width = new GridLength(0);
            col.Width = new GridLength(0);
            RightPanelSplitter.IsVisible = false;
        }
    }

    public void ToggleProperties(bool open)
    {
        var splitterRow = RightGrid.RowDefinitions[1];
        var row = RightGrid.RowDefinitions[2];
        if (open)
        {
            if (_projectExplorerOpen) splitterRow.Height = new GridLength(4);
            row.Height = _lastPropertiesHeight;
            PropertiesSplitter.IsVisible = _projectExplorerOpen;
            ToggleRightPanel(true);
        }
        else
        {
            _lastPropertiesHeight = row.Height;
            splitterRow.Height = new GridLength(0);
            row.Height = new GridLength(0);
            PropertiesSplitter.IsVisible = false;
            if (!_projectExplorerOpen) ToggleRightPanel(false);  // Both closed, collapse right column
        }
        _propertiesOpen = open;
    }

    public void ToggleProjectExplorer(bool open)
    {
        var row = RightGrid.RowDefinitions[0];
        var splitterRow = RightGrid.RowDefinitions[1];
        if (open)
        {
            row.Height = _lastProjectExplorerHeight;
            if (_propertiesOpen) splitterRow.Height = new GridLength(4);
            PropertiesSplitter.IsVisible = _propertiesOpen;
            ToggleRightPanel(true);
        }
        else
        {
            _lastProjectExplorerHeight = row.Height;
            row.Height = new GridLength(0);
            splitterRow.Height = new GridLength(0);
            PropertiesSplitter.IsVisible = false;
            if (!_propertiesOpen) ToggleRightPanel(false);  // Both closed, collapse right column
        }
        _projectExplorerOpen = open;
    }
}