using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mapsui;
using Mapsui.Layers;
using System;
using System.Collections.Generic;
using System.Linq;
using UrbanEcho.Events.UI;
using UrbanEcho.FileManagement;
using UrbanEcho.Messages;
using UrbanEcho.Models;
using UrbanEcho.Models.UI;
using UrbanEcho.Physics;
using UrbanEcho.Services;
using UrbanEcho.Sim;

public partial class MapViewModel : ObservableObject
{
    private readonly IMapFeatureService _mapFeatureService;

    private SelectionLayer _activeLayer = SelectionLayer.None;

    private Vehicle? _trackedVehicle;

    [ObservableProperty] private Map myMap = new Map();
    [ObservableProperty] private bool isRasterVisible = true;
    [ObservableProperty] private bool isIntersectionsVisible = true;
    [ObservableProperty] private bool isCensusOverlayVisible = false;

    public MapViewModel(IMapFeatureService mapFeatureService)
    {
        _mapFeatureService = mapFeatureService;
        MyMap.Tapped += OnMapTapped;
        WeakReferenceMessenger.Default.Register<TrackVehicleMessage>(this, (r, m) => _trackedVehicle = m.Vehicle);
        WeakReferenceMessenger.Default.Register<ActiveLayerChangedMessage>(this, (r, m) => _activeLayer = m.ActiveLayer);
        WeakReferenceMessenger.Default.Register<MapFeatureDeselectedMessage>(this, (r, m) => _trackedVehicle = null);

        var trackingTimer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16 * 10)//Less calls allows us to zoom in and out while tracking easier
        };
        trackingTimer.Tick += (s, e) => UpdateTracking();
        trackingTimer.Start();
    }

    private void OnMapTapped(object? sender, MapEventArgs e)
    {
        var layers = new List<ILayer>();

        if (_activeLayer == SelectionLayer.Intersection && ProjectLayers.IntersectionLayer is not null)
        {
            layers.Add(ProjectLayers.IntersectionLayer);
        }
        else if (_activeLayer == SelectionLayer.Vehicle && ProjectLayers.VehicleLayer is not null)
        {
            layers.Add(ProjectLayers.VehicleLayer);
        }

        if (layers.Count == 0) { return; }

        var mapInfo = e.GetMapInfo(layers);
        if (mapInfo?.Feature is null) { return; }

        if (mapInfo.Layer == ProjectLayers.IntersectionLayer) { HandleIntersectionClick(mapInfo.Feature); }
        else if (mapInfo.Layer == ProjectLayers.VehicleLayer) { HandleVehicleClick(mapInfo.Feature); }
    }

    private void HandleIntersectionClick(IFeature feature)
    {
        var intersection = _mapFeatureService.MapIntersection(feature);
        if (intersection is null)
        {
            WeakReferenceMessenger.Default.Send(new MapFeatureDeselectedMessage());
            return;
        }
        WeakReferenceMessenger.Default.Send(new MapFeatureSelectedMessage(MapFeatureType.Signal, intersection));
    }

    private void HandleVehicleClick(IFeature feature)
    {
        var vehicle = _mapFeatureService.MapVehicle(feature);
        if (vehicle is null)
        {
            WeakReferenceMessenger.Default.Send(new MapFeatureDeselectedMessage());
            return;
        }
        WeakReferenceMessenger.Default.Send(new MapFeatureSelectedMessage(MapFeatureType.Vehicle, vehicle));
    }

    partial void OnIsRasterVisibleChanged(bool value)
    {
        ProjectLayers.IsRasterVisible = value;
        ProjectLayers.AddLayers(MyMap);
        WeakReferenceMessenger.Default.Send(new LogMessage("Raster background image toggled", LogSource.Map));
    }

    partial void OnIsIntersectionsVisibleChanged(bool value)
    {
        ProjectLayers.IsIntersectionsVisible = value;
        ProjectLayers.AddLayers(MyMap);
        WeakReferenceMessenger.Default.Send(new LogMessage("Intersection details toggled", LogSource.Map));
    }

    partial void OnIsCensusOverlayVisibleChanged(bool value)
    {
        ProjectLayers.IsCensusOverlayVisible = value;
        ProjectLayers.AddLayers(MyMap);
        WeakReferenceMessenger.Default.Send(new LogMessage("Census overlay toggled", LogSource.Map));
    }

    public void UpdateTracking()
    {
        if (_trackedVehicle is null) return;
        var pos = _trackedVehicle.Pos;
        MyMap.Navigator.CenterOn(new MPoint(pos.X + World.Offset.X, pos.Y + World.Offset.Y), 16 * 100, Mapsui.Animations.Easing.Linear);
        MyMap.Refresh();//Refresh so map is redrawn else some items don't render in after map moves
    }

    [RelayCommand] private void ToggleRaster() => IsRasterVisible = !IsRasterVisible;

    [RelayCommand] private void ToggleIntersectionDetails() => IsIntersectionsVisible = !IsIntersectionsVisible;

    [RelayCommand] private void ToggleCensusOverlay() => IsCensusOverlayVisible = !IsCensusOverlayVisible;
}