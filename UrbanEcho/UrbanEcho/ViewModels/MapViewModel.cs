using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mapsui;
using Mapsui.Layers;
using System.Collections.Generic;
using System.Linq;
using UrbanEcho.FileManagement;
using UrbanEcho.Messages;
using UrbanEcho.Models;
using UrbanEcho.Models.UI;
using UrbanEcho.Services;
using UrbanEcho.Sim;

public partial class MapViewModel : ObservableObject
{
    private readonly IMapFeatureService _mapFeatureService;

    [ObservableProperty] private Map myMap = new Map();
    [ObservableProperty] private bool isRasterVisible = true;
    [ObservableProperty] private bool isIntersectionsVisible = true;

    public MapViewModel(IMapFeatureService mapFeatureService)
    {
        _mapFeatureService = mapFeatureService;
        MyMap.Tapped += OnMapTapped;
    }

    private void OnMapTapped(object? sender, MapEventArgs e)
    {
        var layers = new List<ILayer>();
        if (ProjectLayers.IntersectionLayer is not null) layers.Add(ProjectLayers.IntersectionLayer);
        if (ProjectLayers.VehicleLayer is not null) layers.Add(ProjectLayers.VehicleLayer);

        var mapInfo = e.GetMapInfo(layers);
        if (mapInfo?.Feature is null) return;

        if (mapInfo.Layer == ProjectLayers.IntersectionLayer) HandleIntersectionClick(mapInfo.Feature);
        else if (mapInfo.Layer == ProjectLayers.VehicleLayer) HandleVehicleClick(mapInfo.Feature);
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

    [RelayCommand] private void ToggleRaster() => IsRasterVisible = !IsRasterVisible;
    [RelayCommand] private void ToggleIntersectionDetails() => IsIntersectionsVisible = !IsIntersectionsVisible;
}