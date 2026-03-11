using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UrbanEcho.Events.Sim;
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
    private Vehicle? _pendingDestinationVehicle;

    [ObservableProperty] private Map myMap = new Map();
    [ObservableProperty] private bool isVolumeVisible = true;
    [ObservableProperty] private bool isTrafficSpeedVisible = true;
    [ObservableProperty] private bool isRasterVisible = true;
    [ObservableProperty] private bool isIntersectionsVisible = true;
    [ObservableProperty] private bool isCensusOverlayVisible = true;
    [ObservableProperty] private bool isRoadVisible = true;

    private int giveTimeForZoomingOut;
    private double currentResolution;

    public MapViewModel(IMapFeatureService mapFeatureService)
    {
        _mapFeatureService = mapFeatureService;
        MyMap.Tapped += OnMapTapped;
        WeakReferenceMessenger.Default.Register<TrackVehicleMessage>(this, (r, m) => _trackedVehicle = m.Vehicle);
        WeakReferenceMessenger.Default.Register<ActiveLayerChangedMessage>(this, (r, m) => _activeLayer = m.ActiveLayer);
        WeakReferenceMessenger.Default.Register<MapFeatureDeselectedMessage>(this, (r, m) =>
        {
            _trackedVehicle = null;
            ProjectLayers.SetRoadSelection(null, MyMap);
            ProjectLayers.SetPathOverlay(null, MyMap);
        });
        WeakReferenceMessenger.Default.Register<MapFeatureSelectedMessage>(this, (r, m) =>
        {
            if (m.Type != MapFeatureType.Road)
                ProjectLayers.SetRoadSelection(null, MyMap);
        });
        WeakReferenceMessenger.Default.Register<PickDestinationMessage>(this, (r, m) => _pendingDestinationVehicle = m.Vehicle);
        WeakReferenceMessenger.Default.Register<ShowVehiclePathMessage>(this, (r, m) =>
        {
            var features = m.Vehicle.GetRemainingPathFeatures();
            ProjectLayers.SetPathOverlay(features, MyMap);
        });
        WeakReferenceMessenger.Default.Register<HideVehiclePathMessage>(this, (r, m) =>
        {
            ProjectLayers.SetPathOverlay(null, MyMap);
        });

        var trackingTimer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16 * 10)//Less calls allows us to zoom in and out while tracking easier
        };
        trackingTimer.Tick += (s, e) => UpdateTracking();
        trackingTimer.Start();
    }

    private void OnMapTapped(object? sender, MapEventArgs e)
    {
        if (_pendingDestinationVehicle is not null)
        {
            var worldPos = e.WorldPosition;
            if (worldPos is not null) { HandleDestinationPick(_pendingDestinationVehicle, worldPos); }
            return;
        }

        var layers = new List<ILayer>();

        if (_activeLayer == SelectionLayer.Road)
        {
            var worldPos = e.WorldPosition;
            if (worldPos is not null) HandleRoadClick(worldPos);
            return;
        }

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

    private const double RoadSelectionThreshold = 200.0;

    private void HandleRoadClick(MPoint worldPos)
    {
        var edge = FindNearestEdge(worldPos);
        if (edge is null)
        {
            WeakReferenceMessenger.Default.Send(new MapFeatureDeselectedMessage());
            return;
        }
        WeakReferenceMessenger.Default.Send(new MapFeatureSelectedMessage(MapFeatureType.Road, edge));
        ProjectLayers.SetRoadSelection(edge.Feature, MyMap);
    }

    private RoadEdge? FindNearestEdge(MPoint worldPos)
    {
        if (Sim.RoadGraph is null) return null;

        var clickPoint = new Point(worldPos.X, worldPos.Y);
        RoadEdge? best = null;
        double bestDist = RoadSelectionThreshold;

        foreach (var edge in Sim.RoadGraph.Edges)
        {
            if (edge.Feature is GeometryFeature gf && gf.Geometry is LineString ls)
            {
                double d = ls.Distance(clickPoint);
                if (d < bestDist) { bestDist = d; best = edge; }
            }
        }

        return best;
    }

    partial void OnIsVolumeVisibleChanged(bool value)
    {
        ProjectLayers.IsVolumeVisible = value;
        MyMap.Refresh();
        WeakReferenceMessenger.Default.Send(new LogMessage("Volume visibility toggled", LogSource.Map));
    }

    private void HandleDestinationPick(Vehicle vehicle, MPoint worldPos)
    {
        _pendingDestinationVehicle = null;
        int? nearestNode = FindNearestNode(worldPos);
        if (nearestNode is null) return;
        EventQueueForSim.Instance.Add(new SetDestinationEvent(vehicle, nearestNode));
        vehicle.SetDestination(nearestNode.Value);
        WeakReferenceMessenger.Default.Send(new DestinationPickedMessage());
        WeakReferenceMessenger.Default.Send(new LogMessage($"Destination set for vehicle {vehicle.VehicleUI.Id}", LogSource.Map));
    }

    private int? FindNearestNode(MPoint worldPos)
    {
        if (Sim.RoadGraph is null) { return null; }
        double bestDist = double.MaxValue;
        int? bestNode = null;
        foreach (var kvp in Sim.RoadGraph.Nodes)
        {
            double dx = kvp.Value.X - worldPos.X;
            double dy = kvp.Value.Y - worldPos.Y;
            double dist = dx * dx + dy * dy;
            if (dist < bestDist) { bestDist = dist; bestNode = kvp.Key; }
        }
        return bestNode;
    }

    partial void OnIsTrafficSpeedVisibleChanged(bool value)
    {
        ProjectLayers.IsTrafficSpeedVisible = value;
        MyMap.Refresh();
        WeakReferenceMessenger.Default.Send(new LogMessage("Traffic speed visibility toggled", LogSource.Map));
    }

    partial void OnIsRasterVisibleChanged(bool value)
    {
        ProjectLayers.IsRasterVisible = value;
        ProjectLayers.AddLayers(MyMap);
        WeakReferenceMessenger.Default.Send(new LogMessage("Raster background image toggled", LogSource.Map));
    }

    partial void OnIsRoadVisibleChanged(bool value)
    {
        ProjectLayers.IsRoadVisible = value;
        ProjectLayers.AddLayers(MyMap);
        WeakReferenceMessenger.Default.Send(new LogMessage("Road Layer toggled", LogSource.Map));
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
        Vector2 currentPosViewportCenter = UrbanEcho.Helpers.Helper.Convert2Box2dWorldPosition(MyMap.Navigator.Viewport.CenterX, MyMap.Navigator.Viewport.CenterY);
        if (giveTimeForZoomingOut == 0)//If zero then not in middle of flyover
        {
            if (Vector2.Distance(currentPosViewportCenter, pos) >= 2000.0f)//If center of viewport 2km away from tracked vehicle do flyover
            {
                currentResolution = MyMap.Navigator.Viewport.Resolution;
                MyMap.Navigator.ZoomTo(15, 16 * 10 * 10, Mapsui.Animations.Easing.CubicOut);
                //If vehicle moved to new position from where tracking is do flyover and skip trying to center on vehicle
                giveTimeForZoomingOut = 1;
            }
            else
            {
                MyMap.Navigator.CenterOn(new MPoint(pos.X + World.Offset.X, pos.Y + World.Offset.Y), 16 * 10, Mapsui.Animations.Easing.Linear);
            }
        }
        else
        {//If vehicle moved to new position from where tracking is do flyover and skip trying to center on vehicle
            giveTimeForZoomingOut++;
            if (giveTimeForZoomingOut == 5)
            {
                MyMap.Navigator.CenterOn(new MPoint(pos.X + World.Offset.X, pos.Y + World.Offset.Y), 16 * 10 * 10, Mapsui.Animations.Easing.CubicInOut);
            }
            if (giveTimeForZoomingOut == 15)
            {
                MyMap.Navigator.ZoomTo(currentResolution, 16 * 10 * 15, Mapsui.Animations.Easing.CubicOut);
            }
            if (giveTimeForZoomingOut >= 30)
            {
                giveTimeForZoomingOut = 0;
            }
        }
        MyMap.Refresh();//Refresh so map is redrawn else some items don't render in after map moves
    }

    [RelayCommand] private void ToggleRaster() => IsRasterVisible = !IsRasterVisible;

    [RelayCommand] private void ToggleVolume() => IsVolumeVisible = !IsVolumeVisible;

    [RelayCommand] private void ToggleRoad() => IsRoadVisible = !IsRoadVisible;

    [RelayCommand] private void ToggleTrafficSpeed() => IsTrafficSpeedVisible = !IsTrafficSpeedVisible;

    [RelayCommand] private void ToggleIntersectionDetails() => IsIntersectionsVisible = !IsIntersectionsVisible;

    [RelayCommand] private void ToggleCensusOverlay() => IsCensusOverlayVisible = !IsCensusOverlayVisible;
}