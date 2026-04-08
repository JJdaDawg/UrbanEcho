using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.UI.Avalonia;
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
using UrbanEcho.Physics;
using UrbanEcho.Services;
using UrbanEcho.Sim;

public partial class MapViewModel : ObservableObject
{
    private readonly IMapFeatureService _mapFeatureService;

    private SelectionLayer _activeLayer = SelectionLayer.None;

    private VehicleReadOnly? _trackedVehicle;
    private VehicleReadOnly? _pendingDestinationVehicle;
    private SpawnPoint? _pendingMoveSpawner;

    [ObservableProperty] private Map myMap = new Map();
    [ObservableProperty] private bool isVolumeVisible = true;
    [ObservableProperty] private bool isTrafficSpeedVisible = true;
    [ObservableProperty] private bool isRasterVisible = true;
    [ObservableProperty] private bool isIntersectionsVisible = true;
    [ObservableProperty] private bool isCensusOverlayVisible = true;
    [ObservableProperty] private bool isRoadVisible = true;

    private int giveTimeForZoomingOut;
    private int skipFollowScans;
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
            _pendingMoveSpawner = null;
            ProjectLayers.SetRoadSelection(null, MyMap);
            ProjectLayers.SetPathOverlay(null, MyMap);
            ProjectLayers.SetIntersectionOverlay(null, MyMap);
        });
        WeakReferenceMessenger.Default.Register<MapFeatureSelectedMessage>(this, (r, m) =>
        {
            if (m.Type != MapFeatureType.Road) { ProjectLayers.SetRoadSelection(null, MyMap); }

            if (m.Type != MapFeatureType.Vehicle)
            {
                _trackedVehicle = null;
                ProjectLayers.SetPathOverlay(null, MyMap);

                if (ProjectLayers.PinLayer != null) { ProjectLayers.PinLayer.Enabled = false; }
            }
            else
            {
                _trackedVehicle = null;
                ProjectLayers.SetPathOverlay(null, MyMap);

                if (ProjectLayers.PinLayer != null) { ProjectLayers.PinLayer.Enabled = false; }
            }
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
        WeakReferenceMessenger.Default.Register<ShowIntersectionOverlayMessage>(this, (r, m) =>
        {
            var features = m.Intersection.GetConnectedRoadFeatures();
            ProjectLayers.SetIntersectionOverlay(features, MyMap);
        });
        WeakReferenceMessenger.Default.Register<DeleteSpawnerMessage>(this, (r, m) =>
        {
            ProjectLayers.RemoveSpawnPoint(m.SpawnPoint);
            WeakReferenceMessenger.Default.Send(new MapFeatureDeselectedMessage());
            WeakReferenceMessenger.Default.Send(new LogMessage($"Spawner deleted", LogSource.Map));
        });
        WeakReferenceMessenger.Default.Register<MoveSpawnerMessage>(this, (r, m) =>
        {
            _pendingMoveSpawner = m.SpawnPoint;
            WeakReferenceMessenger.Default.Send(new ShowToastMessage("Click a node on the map to move the spawner"));
        });
        WeakReferenceMessenger.Default.Register<CancelMoveSpawnerMessage>(this, (r, m) =>
        {
            _pendingMoveSpawner = null;
            WeakReferenceMessenger.Default.Send(new HideToastMessage());
        });
        WeakReferenceMessenger.Default.Register<AutoPlaceSpawnersFromExtentMessage>(this, (r, m) =>
            HandleAutoPlaceFromExtent(m));
        WeakReferenceMessenger.Default.Register<AutoPlaceSpawnersFromOsmResidentialMessage>(this, (r, m) =>
            HandleAutoPlaceFromOsmResidential(m));

        var trackingTimer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16 * 5)
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

        if (_pendingMoveSpawner is not null)
        {
            var worldPos = e.WorldPosition;
            if (worldPos is not null) { HandleSpawnerMove(_pendingMoveSpawner, worldPos); }
            return;
        }

        var layers = new List<ILayer>();

        if (_activeLayer == SelectionLayer.Road)
        {
            var worldPos = e.WorldPosition;
            if (worldPos is not null) HandleRoadClick(worldPos);
            return;
        }

        if (_activeLayer == SelectionLayer.Spawner)
        {
            var worldPos = e.WorldPosition;
            if (worldPos is not null) HandleSpawnerClick(worldPos);
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
        WeakReferenceMessenger.Default.Send(new ShowIntersectionOverlayMessage(intersection));
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
        if (SimManager.Instance.RoadGraph is null) return null;

        var clickPoint = new Point(worldPos.X, worldPos.Y);
        RoadEdge? best = null;
        double bestDist = RoadSelectionThreshold;

        foreach (var edge in SimManager.Instance.RoadGraph.Edges)
        {
            if (edge.Feature is GeometryFeature gf && gf.Geometry is LineString ls)
            {
                double d = ls.Distance(clickPoint);
                if (d < bestDist) { bestDist = d; best = edge; }
            }
        }

        return best;
    }

    private const double SpawnerSelectionThreshold = 500.0;

    private void HandleSpawnerClick(MPoint worldPos)
    {
        // First, try to select an existing spawn point near the click
        SpawnPoint? nearest = null;
        double bestDist = SpawnerSelectionThreshold;
        foreach (var sp in SimManager.Instance.SpawnPoints)
        {
            double dx = sp.X - worldPos.X;
            double dy = sp.Y - worldPos.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < bestDist)
            {
                bestDist = dist;
                nearest = sp;
            }
        }

        if (nearest is not null)
        {
            WeakReferenceMessenger.Default.Send(new MapFeatureSelectedMessage(MapFeatureType.Spawner, nearest));
            return;
        }

        // No nearby spawner found — add a new one at this location
        var node = FindNearestSpawnableNode(worldPos);
        if (node is null)
        {
            WeakReferenceMessenger.Default.Send(new LogMessage("No road graph node with outgoing edges found near click", LogSource.Map));
            return;
        }

        var spawnPoint = new SpawnPoint
        {
            X = node.X,
            Y = node.Y,
            NearestNodeId = node.Id,
            VehiclesPerMinute = 5
        };
        ProjectLayers.AddSpawnPoint(spawnPoint);
        WeakReferenceMessenger.Default.Send(new MapFeatureSelectedMessage(MapFeatureType.Spawner, spawnPoint));
        WeakReferenceMessenger.Default.Send(new LogMessage($"Spawner added at node {node.Id}", LogSource.Map));
    }

    private void HandleSpawnerMove(SpawnPoint spawnPoint, MPoint worldPos)
    {
        _pendingMoveSpawner = null;
        var node = FindNearestSpawnableNode(worldPos);
        if (node is null)
        {
            WeakReferenceMessenger.Default.Send(new HideToastMessage());
            return;
        }
        ProjectLayers.MoveSpawnPoint(spawnPoint, node.X, node.Y, node.Id);
        WeakReferenceMessenger.Default.Send(new HideToastMessage());
        WeakReferenceMessenger.Default.Send(new SpawnerMovedMessage());
        WeakReferenceMessenger.Default.Send(new MapFeatureSelectedMessage(MapFeatureType.Spawner, spawnPoint));
        WeakReferenceMessenger.Default.Send(new LogMessage($"Spawner moved to node {node.Id}", LogSource.Map));
    }

    private RoadNode? FindNearestSpawnableNode(MPoint worldPos)
    {
        if (SimManager.Instance.RoadGraph is null) return null;
        double bestDist = double.MaxValue;
        RoadNode? bestNode = null;
        foreach (var kvp in SimManager.Instance.RoadGraph.Nodes)
        {
            if (SimManager.Instance.RoadGraph.GetOutgoingEdges(kvp.Key).Count == 0)
                continue;
            double dx = kvp.Value.X - worldPos.X;
            double dy = kvp.Value.Y - worldPos.Y;
            double dist = dx * dx + dy * dy;
            if (dist < bestDist) { bestDist = dist; bestNode = kvp.Value; }
        }
        return bestNode;
    }

    partial void OnIsVolumeVisibleChanged(bool value)
    {
        ProjectLayers.IsVolumeVisible = value;
        MyMap.Refresh();
        WeakReferenceMessenger.Default.Send(new LogMessage("Volume visibility toggled", LogSource.Map));
    }

    private void HandleDestinationPick(VehicleReadOnly vehicle, MPoint worldPos)
    {
        _pendingDestinationVehicle = null;
        int? nearestNode = FindNearestNode(worldPos);
        if (nearestNode is null) return;
        EventQueueForSim.Instance.Add(new SetDestinationEvent(vehicle, nearestNode.Value));

        WeakReferenceMessenger.Default.Send(new DestinationPickedMessage());
        WeakReferenceMessenger.Default.Send(new LogMessage($"Destination set for vehicle {vehicle.Id()}", LogSource.Map));
    }

    private int? FindNearestNode(MPoint worldPos)
    {
        if (SimManager.Instance.RoadGraph is null) { return null; }
        double bestDist = double.MaxValue;
        int? bestNode = null;
        foreach (var kvp in SimManager.Instance.RoadGraph.Nodes)
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
        if (_trackedVehicle is null || (!(SimManager.Instance.RunSimulation || SimManager.Instance.Paused || World.Created == false)))//Make sure this routine is only caused during simulation,
                                                                                                                                      //or it can keep layers busy and prevent report running
        {
            if (ProjectLayers.PinLayer != null)
            {
                if (ProjectLayers.PinLayer.Enabled)
                {
                    ProjectLayers.PinLayer.Enabled = false;
                }
            }
            return;
        }
        else
        {
            Viewport vp = MyMap.Navigator.Viewport;
            Vector2 pos = Vector2.Zero;
            if (_trackedVehicle != null)
            {
                pos = _trackedVehicle.PinPos();

                if (float.IsNaN(pos.X) || float.IsNaN(pos.Y))
                {
                    return;
                }
            }
            else
            {
                return;
            }

            if (ProjectLayers.PinLayer != null)
            {
                if (!ProjectLayers.PinLayer.Enabled)
                {
                    ProjectLayers.PinLayer.Enabled = true;
                }
                if (ProjectLayers.PinLayerFeatures.Count > 0)
                {
                    List<IFeature> pinFeatures = new List<IFeature>();
                    MPoint mPoint = new MPoint(pos.X + World.Offset.X, pos.Y + World.Offset.Y);
                    if (ProjectLayers.PinLayerFeatures[0] is PointFeature pf)
                    {
                        pf.Point.X = mPoint.X;
                        pf.Point.Y = mPoint.Y;
                        PointFeature newPoint = (PointFeature)pf.Clone();
                        pinFeatures.Add(newPoint);
                        ProjectLayers.PinLayer.Features = pinFeatures;
                        ProjectLayers.PinLayer.DataHasChanged();
                    }
                }
            }
            skipFollowScans++;
            if (skipFollowScans >= 10)
            {
                skipFollowScans = 0;
                Vector2 currentPosViewportCenter = UrbanEcho.Helpers.Helper.Convert2Box2dWorldPosition(vp.CenterX, vp.CenterY);
                if (giveTimeForZoomingOut == 0)//If zero then not in middle of flyover
                {
                    if (Vector2.Distance(currentPosViewportCenter, pos) >= 2000.0f)//If center of viewport 2km away from tracked vehicle do flyover
                    {
                        currentResolution = vp.Resolution;
                        if (currentResolution <= 0.1f)
                        {
                            currentResolution = 0.1f;
                        }
                        MyMap.Navigator.ZoomTo(15, 16 * 100, Mapsui.Animations.Easing.CubicOut);
                        //If vehicle moved to new position from where tracking is do flyover and skip trying to center on vehicle
                        giveTimeForZoomingOut = 1;
                    }
                    else
                    {
                        if (World.Created)
                        {
                            MyMap.Navigator.CenterOn(new MPoint(pos.X + World.Offset.X, pos.Y + World.Offset.Y), 16 * 50, Mapsui.Animations.Easing.Linear);
                        }
                    }
                }
                else
                {//If vehicle moved to new position from where tracking is do flyover and skip trying to center on vehicle
                    giveTimeForZoomingOut++;
                    if (giveTimeForZoomingOut == 2)
                    {
                        MyMap.Navigator.CenterOn(new MPoint(pos.X + World.Offset.X, pos.Y + World.Offset.Y), 16 * 50, Mapsui.Animations.Easing.CubicInOut);
                    }
                    if (giveTimeForZoomingOut == 3)
                    {
                        MyMap.Navigator.ZoomTo(currentResolution, 16 * 50, Mapsui.Animations.Easing.CubicOut);
                    }
                    if (giveTimeForZoomingOut >= 4)
                    {
                        giveTimeForZoomingOut = 0;
                    }
                }
            }
            // MyMap.Refresh();//Refresh so map is redrawn else some items don't render in after map moves
        }
    }

    [RelayCommand] private void ToggleRaster() => IsRasterVisible = !IsRasterVisible;

    [RelayCommand] private void ToggleVolume() => IsVolumeVisible = !IsVolumeVisible;

    [RelayCommand] private void ToggleRoad() => IsRoadVisible = !IsRoadVisible;

    [RelayCommand] private void ToggleTrafficSpeed() => IsTrafficSpeedVisible = !IsTrafficSpeedVisible;

    [RelayCommand] private void ToggleIntersectionDetails() => IsIntersectionsVisible = !IsIntersectionsVisible;

    [RelayCommand] private void ToggleCensusOverlay() => IsCensusOverlayVisible = !IsCensusOverlayVisible;

    private void HandleAutoPlaceFromExtent(AutoPlaceSpawnersFromExtentMessage m)
    {
        var graph = SimManager.Instance.RoadGraph;
        if (graph == null)
        {
            WeakReferenceMessenger.Default.Send(new LogMessage("Road graph not loaded", LogSource.Map));
            return;
        }

        var coords = graph.Nodes.Values
            .Select(n => new Coordinate(n.X, n.Y))
            .ToArray();

        if (coords.Length < 3)
        {
            WeakReferenceMessenger.Default.Send(new LogMessage("Not enough road nodes to compute boundary", LogSource.Map));
            return;
        }

        var factory = new GeometryFactory();
        var hull = factory.CreateMultiPointFromCoords(coords).ConvexHull();
        if (hull == null || hull.IsEmpty)
        {
            WeakReferenceMessenger.Default.Send(new LogMessage("Could not compute road network boundary", LogSource.Map));
            return;
        }

        var gateNodes = UrbanEcho.Graph.PolygonSpawnerHelper.GetBoundaryNodes(hull, graph, m.Tolerance, m.MaxGates);
        if (gateNodes.Count == 0)
        {
            WeakReferenceMessenger.Default.Send(new LogMessage("No road nodes found near network boundary — try increasing tolerance", LogSource.Map));
            return;
        }

        var spawnPoints = gateNodes.Select(node => new SpawnPoint
        {
            X = node.X,
            Y = node.Y,
            NearestNodeId = node.Id,
            VehiclesPerMinute = m.VehiclesPerMinute
        }).ToList();

        EventQueueForSim.Instance.Add(new AutoPlaceSpawnersEvent(spawnPoints));
        WeakReferenceMessenger.Default.Send(new LogMessage(
            $"Auto-placed {gateNodes.Count} gateway spawner(s) at network boundary", LogSource.Map));
    }

    private void HandleAutoPlaceFromOsmResidential(AutoPlaceSpawnersFromOsmResidentialMessage m)
    {
        var graph = SimManager.Instance.RoadGraph;
        if (graph == null)
        {
            WeakReferenceMessenger.Default.Send(new LogMessage("Road graph not loaded", LogSource.Map));
            return;
        }

        var polygons = UrbanEcho.Graph.PolygonSpawnerHelper.GetOsmResidentialPolygons(m.OsmPath);
        if (polygons.Count == 0)
        {
            WeakReferenceMessenger.Default.Send(new LogMessage("No residential areas (landuse=residential) found in OSM file", LogSource.Map));
            return;
        }

        var spawnPoints = new List<SpawnPoint>();
        foreach (var (name, polygon) in polygons)
        {
            var gateNodes = UrbanEcho.Graph.PolygonSpawnerHelper.GetBoundaryNodes(
                polygon, graph, toleranceMercator: 400.0, maxGates: m.MaxGatesPerArea);

            foreach (var node in gateNodes)
            {
                spawnPoints.Add(new SpawnPoint
                {
                    X = node.X,
                    Y = node.Y,
                    NearestNodeId = node.Id,
                    VehiclesPerMinute = m.VehiclesPerMinute
                });
            }
        }

        if (spawnPoints.Count == 0)
        {
            WeakReferenceMessenger.Default.Send(new LogMessage(
                $"Found {polygons.Count} residential area(s) but no road nodes near their boundaries", LogSource.Map));
            return;
        }

        EventQueueForSim.Instance.Add(new AutoPlaceSpawnersEvent(spawnPoints));
        WeakReferenceMessenger.Default.Send(new LogMessage(
            $"Auto-placed {spawnPoints.Count} spawner(s) from {polygons.Count} residential area(s)", LogSource.Map));
    }
}