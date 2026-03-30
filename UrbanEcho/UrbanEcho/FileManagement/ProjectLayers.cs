using BruTile.MbTiles;
using DocumentFormat.OpenXml.Bibliography;
using FluentAvalonia.Core;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using Mapsui.Tiling.Layers;
using Microsoft.EntityFrameworkCore.Diagnostics;

//using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using OsmSharp;
using OsmSharp.Streams;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using UrbanEcho.Events.UI;
using UrbanEcho.Graph;
using UrbanEcho.Helpers;
using UrbanEcho.Models;
using UrbanEcho.Physics;
using UrbanEcho.Sim;
using UrbanEcho.Styles;
using Color = Mapsui.Styles.Color;
using Exception = System.Exception;
using Layer = Mapsui.Layers.Layer;
using Pen = Mapsui.Styles.Pen;

namespace UrbanEcho.FileManagement
{
    public static class ProjectLayers
    {
        private static ILayer? backgroundLayer;

        private static RasterizingLayer? roadLayerFirstPass;
        private static RasterizingLayer? roadLayerSecondPass;
        private static RasterizingLayer? roadLabelLayer;
        private static ILayer? intersectionLayer;
        private static MemoryLayer? vehicleLayer;
        private static MemoryLayer? roadSelectionLayer;
        private static MemoryLayer? pathOverlayLayer;
        private static MemoryLayer? intersectionOverlayLayer;
        private static Avalonia.Threading.DispatcherTimer? pathBlinkTimer;

        private static RasterizingLayer? debugLayer;

        private static MemoryLayer? censusOverlayLayer;

        private static MemoryLayer? spawnerLayer;

        private static bool backgroundRequiresLoading = false;
        private static bool roadRequiresLoading = false;
        private static bool intersectionRequiresLoading = false;
        private static bool censusRequiresLoading = false;
        private static bool vehicleRequiresLoading = true;

        private static bool roadLoaded = false;
        private static bool intersectionLoaded = false;

        private static bool isZoomedToLayer = false;

        private static ProjectFile? currentProjectFile = new ProjectFile();
        public static bool IsVolumeVisible { get; set; } = true;
        public static bool IsTrafficSpeedVisible { get; set; } = true;
        public static bool IsRasterVisible { get; set; } = true;
        public static bool IsRoadVisible { get; set; } = true;

        public static bool IsIntersectionsVisible { get; set; } = true;
        public static bool IsCensusOverlayVisible { get; set; } = true;
        public static bool IsSpawnersVisible { get; set; } = true;

        public static List<IFeature> VehicleFeatures = new List<IFeature>();

        public static List<IFeature> DebugLayerFeatures = new List<IFeature>();

        public static MPoint CenterOfMap = new MPoint();

        public static ILayer? IntersectionLayer => intersectionLayer;
        public static MemoryLayer? VehicleLayer => vehicleLayer;

        public static MemoryLayer? PinLayer;
        public static List<IFeature> PinLayerFeatures = new List<IFeature>();

        public static MemoryLayer? SpawnerLayer => spawnerLayer;
        public static List<IFeature> SpawnerFeatures = new List<IFeature>();

        public static void LoadProject(string path)
        {
            ProjectFile? openProject = ProjectFile.Open(path);

            if (openProject != null)
            {
                //Clear queue incase UI is currently updating vehicle layer
                EventQueueForUI.Instance.Clear();

                currentProjectFile = openProject;
                SimManager.Instance.SetProjectNameChanged();
                resetLayers();

                if (Load(currentProjectFile))
                {
                    if (MainWindow.Instance.GetMap() != null)
                    {
                        EventQueueForUI.Instance.Add(new AddLayersEvent(MainWindow.Instance.GetMap()));
                        EventQueueForUI.Instance.Add(new ZoomEvent(MainWindow.Instance.GetMap()));
                    }
                }
                else
                {
                    if (MainWindow.Instance.GetMap() != null)
                    {
                        ClearLayers(MainWindow.Instance.GetMap());
                    }
                }

                EventQueueForUI.Instance.Add(new SetProjectEvent(currentProjectFile));
            }
        }

        public static bool GetIsRoadAndIntersectionLoaded()
        {
            return roadLoaded && intersectionLoaded;
        }

        public static ProjectFile? GetProject()
        {
            return currentProjectFile;
        }

        public static void LoadBackgroundFile(string path)
        {
            if (currentProjectFile != null)
            {
                currentProjectFile.BackgroundLayerPath = path;
                backgroundRequiresLoading = true;
                backgroundLayer = null;
                if (Load(currentProjectFile))
                {
                    if (MainWindow.Instance.GetMap() != null)
                    {
                        EventQueueForUI.Instance.Add(new AddLayersEvent(MainWindow.Instance.GetMap()));
                        EventQueueForUI.Instance.Add(new ZoomEvent(MainWindow.Instance.GetMap()));
                    }
                }

                EventQueueForUI.Instance.Add(new SetProjectEvent(currentProjectFile));
            }
        }

        public static MRect? TryGetRoadLayerExtent()
        {
            MRect? returnValue = null;
            if (roadLoaded)
            {
                if (roadLayerFirstPass != null)
                {
                    returnValue = roadLayerFirstPass.Extent;
                }
            }
            return returnValue;
        }

        public static void LoadRoadFile(string path)
        {
            resetLayers();
            if (currentProjectFile != null)
            {
                currentProjectFile.RoadLayerPath = path;
                roadRequiresLoading = true;
                censusRequiresLoading = true;
                roadLoaded = false;
                roadLabelLayer = null;
                roadLayerFirstPass = null;
                roadLayerSecondPass = null;

                if (Path.GetExtension(currentProjectFile.RoadLayerPath) == ".osm")//if osm file also load the intersection file
                {
                    currentProjectFile.IntersectionLayerPath = path;
                    intersectionRequiresLoading = true;
                    intersectionLoaded = false;
                    intersectionLayer = null;
                }

                if (Load(currentProjectFile))
                {
                    if (MainWindow.Instance.GetMap() != null)
                    {
                        EventQueueForUI.Instance.Add(new AddLayersEvent(MainWindow.Instance.GetMap()));
                        EventQueueForUI.Instance.Add(new ZoomEvent(MainWindow.Instance.GetMap()));
                    }
                }

                EventQueueForUI.Instance.Add(new SetProjectEvent(currentProjectFile));
            }
        }

        public static void LoadIntersectionsFile(string path)
        {
            if (currentProjectFile != null)
            {
                currentProjectFile.IntersectionLayerPath = path;
                intersectionRequiresLoading = true;
                intersectionLoaded = false;
                intersectionLayer = null;

                if (Load(currentProjectFile))
                {
                    if (MainWindow.Instance.GetMap() != null)
                    {
                        EventQueueForUI.Instance.Add(new AddLayersEvent(MainWindow.Instance.GetMap()));
                        EventQueueForUI.Instance.Add(new ZoomEvent(MainWindow.Instance.GetMap()));
                    }
                }

                EventQueueForUI.Instance.Add(new SetProjectEvent(currentProjectFile));
            }
        }

        public static void LoadCensusFile(string path)
        {
            if (currentProjectFile == null || SimManager.Instance.RoadGraph == null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), "[Census] Cannot load census data: no project or road graph loaded"));
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(path))
                {
                    currentProjectFile.CensusLayerPath = path;
                    SimManager.Instance.InitializeCensusSpawning(path);

                    if (SimManager.Instance.CensusSpawn != null && SimManager.Instance.CensusSpawn.IsLoaded)
                    {
                        censusOverlayLayer = CreateCensusOverlayLayer(SimManager.Instance.CensusSpawn.Zones);
                        if (MainWindow.Instance.GetMap() != null)
                        {
                            EventQueueForUI.Instance.Add(new AddLayersEvent(MainWindow.Instance.GetMap()));
                        }
                        EventQueueForUI.Instance.Add(new CensusLoadedEvent());
                        EventQueueForUI.Instance.Add(new SetProjectEvent(currentProjectFile));
                    }
                }
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to load census data: {ex.Message}"));
            }
        }

        public static bool LayersNeedReAdd()
        {
            return backgroundRequiresLoading || roadRequiresLoading || intersectionRequiresLoading;
        }

        public static bool VehicleLayerReady()
        {
            if (vehicleLayer != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void resetLayers()
        {
            backgroundRequiresLoading = true;
            roadRequiresLoading = true;
            intersectionRequiresLoading = true;
            vehicleRequiresLoading = true;
            censusRequiresLoading = true;
            roadLoaded = false;
            intersectionLoaded = false;
            backgroundLayer = null;
            roadLayerFirstPass = null;
            roadLayerSecondPass = null;
            roadLabelLayer = null;
            intersectionLayer = null;
            vehicleLayer = null;
            PinLayer = null;
            roadSelectionLayer = null;
            pathOverlayLayer = null;
            intersectionOverlayLayer = null;
            if (pathBlinkTimer != null) { pathBlinkTimer.Stop(); pathBlinkTimer = null; }
            debugLayer = null;
            censusOverlayLayer = null;
            spawnerLayer = null;

            SimManager.Instance.Clear(); //Clear all the existing lists
            TrafficVolumeLoader.Reset();

            VehicleFeatures = new List<IFeature>();
            DebugLayerFeatures = new List<IFeature>();
            SpawnerFeatures = new List<IFeature>();

            MainWindow.Instance.GetMap().Refresh();
        }

        public static bool Load(ProjectFile currentProjectFile)
        {
            bool addLayer = false;
            if (currentProjectFile != null)
            {
                if (backgroundRequiresLoading == true)
                {
                    if (currentProjectFile.BackgroundLayerPath != "")
                    {
                        if (currentProjectFile.BackgroundLayerPath == "osm")
                        {
                            backgroundLayer = Mapsui.Tiling.OpenStreetMap.CreateTileLayer();
                            backgroundLayer.Name = "background";
                        }
                        else
                        {
                            backgroundLayer = CreateMbTilesLayer(currentProjectFile.BackgroundLayerPath, "background");
                        }
                        if (backgroundLayer != null)
                        {
                            addLayer = true;

                            backgroundRequiresLoading = false;
                        }
                    }
                    else
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Project file did not contain a background layer"));
                    }
                }
                if (roadRequiresLoading == true)
                {
                    if (currentProjectFile.RoadLayerPath != "")
                    {
                        try
                        {
                            if (Path.GetExtension(currentProjectFile.RoadLayerPath) == ".shp")
                            {
                                ShapeFile roadNetwork = new ShapeFile(currentProjectFile.RoadLayerPath);
                                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Load Road Shape File"));
                                if (roadNetwork != null)
                                {
                                    Layer? roadLayer = CreateRoadLayer(roadNetwork, "Road Outline", true);
                                    if (roadLayer != null)
                                    {
                                        roadLayerFirstPass = new RasterizingLayer(roadLayer);
                                        Layer? roadLayer2 = CreateRoadLayer(roadNetwork, "Roads", false);
                                        if (roadLayer2 != null)
                                        {
                                            roadLayerSecondPass = new RasterizingLayer(roadLayer2);
                                        }
                                        Layer? setRoadLabelLayer = CreateRoadLabelLayer(roadNetwork, "Road Labels");
                                        if (setRoadLabelLayer != null)
                                        {
                                            roadLabelLayer = new RasterizingLayer(setRoadLabelLayer);
                                        }

                                        if (roadLayer.DataSource != null)
                                        {
                                            SimManager.Instance.SetRoadFeatureStats(Helpers.Helper.GetFeatures(roadLayer.DataSource));

                                            SimManager.Instance.RoadGraph = UrbanTrafficSim.Core.IO.RoadGraphLoader.LoadFromFeatures(Helpers.Helper.GetFeatures(roadLayer.DataSource));
                                        }
                                    }
                                }
                            }
                            if (Path.GetExtension(currentProjectFile.RoadLayerPath) == ".osm")
                            {
                                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Load Road osm File"));

                                List<IFeature> featuresList = Helpers.OsmReadHelper.GetRoadFeatures(currentProjectFile.RoadLayerPath);
                                if (featuresList.Count > 0)
                                {
                                    MemoryLayer? roadLayer1 = CreateRoadLayerFromOSM(featuresList, "Road Outline", true);
                                    if (roadLayer1 != null)
                                    {
                                        roadLayerFirstPass = new RasterizingLayer(roadLayer1);
                                    }

                                    MemoryLayer? roadLayer2 = CreateRoadLayerFromOSM(featuresList, "Roads", false);
                                    if (roadLayer2 != null)
                                    {
                                        roadLayerSecondPass = new RasterizingLayer(roadLayer2);
                                    }

                                    MemoryLayer? setRoadLabelLayer = CreateRoadLabelLayerFromOSM(featuresList, "Road Labels");
                                    if (setRoadLabelLayer != null)
                                    {
                                        roadLabelLayer = new RasterizingLayer(setRoadLabelLayer);
                                    }

                                    if (featuresList.Count > 0)
                                    {
                                        SimManager.Instance.SetRoadFeatureStats(featuresList);
                                        SimManager.Instance.RoadGraph = UrbanTrafficSim.Core.IO.RoadGraphLoader.LoadFromFeatures(featuresList);
                                    }
                                }
                                else
                                {
                                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to load Road Layer file contained zero features"));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to load Road Layer {ex.ToString()}"));
                        }

                        if (roadLayerFirstPass != null && roadLayerSecondPass != null)
                        {
                            addLayer = true;
                            roadLoaded = true;
                            roadRequiresLoading = false;
                            vehicleRequiresLoading = true;
                            intersectionRequiresLoading = true;
                        }
                    }
                    else
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Project file did not contain a road layer"));
                    }
                }
                if (intersectionRequiresLoading == true)
                {
                    if (currentProjectFile.IntersectionLayerPath != "")
                    {
                        try
                        {
                            if (Path.GetExtension(currentProjectFile.RoadLayerPath) == ".shp")
                            {
                                ShapeFile intersections = new ShapeFile(currentProjectFile.IntersectionLayerPath);
                                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Loaded Intersections Shape File"));
                                intersectionLayer = CreateIntersectionsLayer(intersections, "Intersections");
                            }

                            if (Path.GetExtension(currentProjectFile.RoadLayerPath) == ".osm")
                            {
                                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Load Intersection osm File"));

                                //part of this code from here
                                //https://github.com/OsmSharp/core/blob/develop/samples/Sample.Filter/Program.cs

                                List<IFeature> featuresList = OsmReadHelper.GetIntersectionFeatures(currentProjectFile.RoadLayerPath);

                                intersectionLayer = CreateIntersectionsLayerFromOSM(featuresList, "Intersections");
                            }
                        }
                        catch (Exception ex)
                        {
                            EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to load Intersection Layer {ex.ToString()}"));
                        }
                    }

                    if (intersectionLayer != null)
                    {
                        addLayer = true;
                        intersectionLoaded = true;
                        intersectionRequiresLoading = false;
                        vehicleRequiresLoading = true;
                    }
                }
            }
            else
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Project file did not contain a intersection layer"));
            }

            if (vehicleRequiresLoading && intersectionLoaded && roadLoaded)
            {
                SimManager.Instance.ResetSim();
                World.Clear();//we need to init world again so center point is correct if reloading

                vehicleRequiresLoading = false;

                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Initialize Graph"));

                SimManager.Instance.InitializeGraph();

                if (censusRequiresLoading)
                {
                    if (currentProjectFile != null)
                    {
                        LoadCensusFile(currentProjectFile.CensusLayerPath);
                        if (censusOverlayLayer != null)
                        {
                            addLayer = true;
                            censusRequiresLoading = false;
                            vehicleRequiresLoading = true;
                        }
                    }
                }

                if (SimManager.Instance.RoadGraph != null)
                {
                    TrafficVolumeLoader.AssignToGraph(SimManager.Instance.RoadGraph);
                }

                vehicleLayer = CreateVehicleLayer();
                PinLayer = CreatePinLayer();
                spawnerLayer = CreateSpawnerLayer();
                if (false == true)//Change this to enable debug layer
                {
                    MemoryLayer tempDebugLayer = CreateDebugLayer();//use this layer for testing
                    tempDebugLayer.Features = DebugLayerFeatures;
                    debugLayer = new RasterizingLayer(tempDebugLayer);
                }
            }
            else
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Not all required layers loaded"));
            }

            return addLayer;
        }

        public static bool IsIntersectionsCreated()
        {
            return intersectionLoaded;
        }

        private static MemoryLayer CreateRoadSelectionLayer()
        {
            return new MemoryLayer("Road Selection")
            {
                Style = new VectorStyle
                {
                    Line = new Pen
                    {
                        Color = Color.FromArgb(220, 0, 200, 255),
                        Width = 6,
                        PenStrokeCap = PenStrokeCap.Round,
                        StrokeJoin = StrokeJoin.Round
                    }
                },
                Features = new List<IFeature>()
            };
        }

        public static void SetRoadSelection(IFeature? feature, Map? map)
        {
            if (roadSelectionLayer == null) return;

            roadSelectionLayer.Features = feature is not null
                ? new List<IFeature> { feature }
                : new List<IFeature>();

            map?.Refresh();
        }

        private static MemoryLayer CreatePathOverlayLayer()
        {
            return new MemoryLayer("Path Overlay")
            {
                Style = new VectorStyle
                {
                    Line = new Pen
                    {
                        Color = Color.FromArgb(200, 255, 180, 0),
                        Width = 4,
                        PenStyle = PenStyle.ShortDash,
                        PenStrokeCap = PenStrokeCap.Round,
                        StrokeJoin = StrokeJoin.Round
                    }
                },
                Features = new List<IFeature>()
            };
        }

        public static void SetPathOverlay(IReadOnlyList<IFeature>? features, Map? map)
        {
            if (pathOverlayLayer == null) return;

            if (features is null || features.Count == 0)
            {
                pathOverlayLayer.Features = new List<IFeature>();
                //StopPathBlink();
                map?.Refresh();
                return;
            }

            pathOverlayLayer.Features = new List<IFeature>(features);
            //StartPathBlink(map);
            map?.Refresh();
        }

        private static MemoryLayer CreateIntersectionOverlayLayer()
        {
            return new MemoryLayer("Intersection Overlay")
            {
                Features = new List<IFeature>(),
                Style = new ThemeStyle(f =>
                {
                    bool hasRightOfWay = f["RightOfWay"] is int v && v == 1;
                    return new VectorStyle
                    {
                        Line = new Pen
                        {
                            Color = hasRightOfWay ? Color.FromArgb(220, 50, 205, 50) : Color.FromArgb(220, 220, 30, 30),
                            Width = 6,
                            PenStrokeCap = PenStrokeCap.Round,
                            StrokeJoin = StrokeJoin.Round
                        }
                    };
                })
            };
        }

        public static void SetIntersectionOverlay(IReadOnlyList<IFeature>? roadFeatures, Map? map)
        {
            if (intersectionOverlayLayer == null) return;

            intersectionOverlayLayer.Features = roadFeatures != null && roadFeatures.Count > 0
                ? new List<IFeature>(roadFeatures)
                : new List<IFeature>();

            intersectionOverlayLayer.DataHasChanged();
            map?.Refresh();
        }

        private static void StartPathBlink(Map? map)
        {
            if (pathBlinkTimer != null) return;

            pathBlinkTimer = new Avalonia.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(600)
            };
            pathBlinkTimer.Tick += (s, e) =>
            {
                if (pathOverlayLayer == null) return;
                pathOverlayLayer.Enabled = !pathOverlayLayer.Enabled;
                map?.Refresh();
            };
            pathBlinkTimer.Start();
        }

        private static void StopPathBlink()
        {
            if (pathBlinkTimer != null)
            {
                pathBlinkTimer.Stop();
                pathBlinkTimer = null;
            }
            if (pathOverlayLayer != null)
                pathOverlayLayer.Enabled = true;
        }

        //https://github.com/BruTile/BruTile
        public static TileLayer? CreateMbTilesLayer(string path, string name)
        {
            TileLayer? mbTilesLayer = null;
            try
            {
                MbTilesTileSource mbTilesTileSource = new MbTilesTileSource(new SQLiteConnectionString(path, true));
                mbTilesLayer = new TileLayer(mbTilesTileSource) { Name = name };
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Loaded MBTiles Background File"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to create tile layer {ex.ToString()}"));
            }

            return mbTilesLayer;
        }

        //not currently used (for geotiff files)
        public static ILayer? CreateBackLayer(IProvider source, string name)
        {
            Layer? layer = null;
            try
            {
                source.CRS = "EPSG:4326";

                ProjectingProvider projectingProvider = new ProjectingProvider(source)
                {
                    CRS = "EPSG:3857"
                };

                layer = new Layer(name);
                layer.DataSource = projectingProvider;
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Created Background Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to load Background Layer {ex.ToString()}"));
            }

            return layer;
        }

        public static MemoryLayer? CreateIntersectionsLayerFromOSM(List<IFeature> features, string name)
        {
            MemoryLayer? layer = null;
            try
            {
                layer = new MemoryLayer(name);

                layer.Opacity = 1.0f;

                layer.MaxVisible = 3.5f;

                layer.Features = features;

                IntersectionStyles intersectionsStyle = new IntersectionStyles();
                if (layer != null)
                {
                    layer.Style = intersectionsStyle.CreateThemeStyle();
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Created Intersections Layer"));
                }
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Unable to create Intersections Layer {ex.ToString()}"));
            }
            if (layer == null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to create Intersections Layer"));
            }

            return layer;
        }

        public static MemoryLayer? CreateIntersectionsLayer(IProvider source, string name)
        {
            MemoryLayer? layer = null;
            try
            {
                source.CRS = "EPSG:4326";

                ProjectingProvider projectingProvider = new ProjectingProvider(source)
                {
                    CRS = "EPSG:3857"
                };

                layer = new MemoryLayer(name);

                layer.Opacity = 1.0f;

                layer.MaxVisible = 3.5f;

                List<IFeature> intersections = Helpers.Helper.GetFeatures(projectingProvider);

                layer.Features = intersections;

                IntersectionStyles intersectionsStyle = new IntersectionStyles();

                if (layer != null)
                {
                    layer.Style = intersectionsStyle.CreateThemeStyle();
                }
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Created Intersections Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Unable to create Intersections Layer {ex.ToString()}"));
            }
            if (layer == null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to create Intersections Layer"));
            }
            return layer;
        }

        public static bool CreateRoadIntersections()
        {
            List<IFeature> features = new List<IFeature>();
            if (intersectionLayer is null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Intersections layer was not added before trying to add intersection bodies"));
                return false;
            }
            if (intersectionLayer is Layer layer)
            {
                IProvider? intersectionDatasource = layer.DataSource;
                if (intersectionDatasource is null)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Datasource used for adding intersection bodies was null"));
                    return false;
                }
                features = Helpers.Helper.GetFeatures(intersectionDatasource);
            }
            else
            {
                if (intersectionLayer is MemoryLayer memoryLayer)
                {
                    features = memoryLayer.Features.ToList();
                }
            }
            if (SimManager.Instance.RoadGraph is null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Intersections can not be added before road graph"));
                return false;
            }

            for (int i = 0; i < features.Count; i++)
            {
                IFeature feature = features[i];

                if (feature != null)
                {
                    string? name = feature["Intersecti"]?.ToString();//default name
                    if (name == null)
                    {
                        name = Guid.NewGuid().ToString();
                    }

                    if (feature is GeometryFeature intersectGF)
                    {
                        if (intersectGF.Geometry is Point p)
                        {
                            RoadIntersection? r = RoadIntersection.Create(name, feature, SimManager.Instance.RoadGraph);

                            if (r is not null)
                            {
                                SimManager.Instance.RoadIntersections.Add(r);
                            }
                        }
                    }
                }
            }
            return true;
        }

        public static MemoryLayer? CreateRoadLayerFromOSM(List<IFeature> features, string name, bool doOutline)
        {
            MemoryLayer? layer = null;
            try
            {
                layer = new MemoryLayer(name);
                layer.Features = features;

                RoadStyles roadStyle = new RoadStyles(doOutline);

                layer.Style = roadStyle.CreateThemeStyle();

                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Created Road Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to create road layer {ex.ToString()}"));
            }
            return layer;
        }

        public static Layer? CreateRoadLayer(IProvider source, string name, bool doOutline)
        {
            Layer? layer = null;
            try
            {
                source.CRS = "EPSG:4326";

                ProjectingProvider projectingProvider = new ProjectingProvider(source)
                {
                    CRS = "EPSG:3857"
                };

                layer = new Layer(name);
                layer.DataSource = projectingProvider;

                RoadStyles roadStyle = new RoadStyles(doOutline);

                layer.Style = roadStyle.CreateThemeStyle();

                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Created Road Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to create road layer {ex.ToString()}"));
            }
            return layer;
        }

        public static MemoryLayer? CreateRoadLabelLayerFromOSM(List<IFeature> features, string name)
        {
            MemoryLayer? layer = null;
            try
            {
                layer = new MemoryLayer(name);
                layer.MinVisible = 0.2f;
                layer.MaxVisible = 1.5f;
                layer.Features = features;

                RoadLabelStyles labelStyle = new RoadLabelStyles();

                layer.Style = labelStyle.CreateThemeStyle();

                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Created Road Label Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to create road label layer {ex.ToString()}"));
            }
            return layer;
        }

        public static Layer? CreateRoadLabelLayer(IProvider source, string name)
        {
            Layer? layer = null;
            try
            {
                source.CRS = "EPSG:4326";

                ProjectingProvider projectingProvider = new ProjectingProvider(source)
                {
                    CRS = "EPSG:3857"
                };

                layer = new Layer(name);
                layer.MaxVisible = 1.5f;
                layer.DataSource = projectingProvider;

                RoadLabelStyles labelStyle = new RoadLabelStyles();

                layer.Style = labelStyle.CreateThemeStyle();

                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Created Road Label Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to create road label layer {ex.ToString()}"));
            }
            return layer;
        }

        public static MemoryLayer? CreateVehicleLayer()
        {
            MemoryLayer? layer = null;
            World.Clear();
            if (World.Created == false)
            {
                World.WasCreated = false;
                if (roadLayerFirstPass != null)
                    while (roadLayerFirstPass.Busy)
                    {
                        Thread.Sleep(1);
                    }
                MRect? extent = roadLayerFirstPass?.Extent;

                if (extent != null)
                {
                    CenterOfMap = new MPoint(extent.MinX + (extent.MaxX - extent.MinX) / 2,
                                extent.MinY + (extent.MaxY - extent.MinY) / 2);

                    World.Init(CenterOfMap.X, CenterOfMap.Y);
                }
            }

            if (World.Created == false)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Unable To Create Vehicle Layer if box2d world was not initialized"));
                return null;
            }

            try
            {
                layer = new MemoryLayer("Vehicles");

                layer.Features = VehicleFeatures;

                layer.Opacity = 1.0f;

                layer.MaxVisible = 1.75f;

                VehicleStyles vehiclesStyle = new VehicleStyles();

                layer.Style = vehiclesStyle.CreateThemeStyle();

                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Created Vehicles Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to create Vehicle Layer {ex.ToString()}"));
            }

            return layer;
        }

        /// <summary>
        /// Creates a semi-transparent overlay layer showing census dissemination
        /// areas color-coded by car commuter density (drivers per zone).
        /// Red = high density, blue = low density.
        /// </summary>
        public static MemoryLayer? CreateCensusOverlayLayer(IReadOnlyList<CensusZone> zones)
        {
            if (zones == null || zones.Count == 0)
                return null;

            try
            {
                var features = new List<IFeature>();

                int maxDrivers = 1;
                foreach (var z in zones)
                {
                    if (z.CarTruckVanDrivers > maxDrivers)
                        maxDrivers = z.CarTruckVanDrivers;
                }
                double maxIntensityValue = 0;
                foreach (var zone in zones)
                {
                    double zoneAreaToUse = zone.RatioOfArea;
                    if (zoneAreaToUse < 0.0001f)
                    {
                        zoneAreaToUse = 0.0001f;
                    }
                    double theValue = (zone.CarTruckVanDrivers / (double)(maxDrivers)) * 1.0f / zoneAreaToUse;

                    if (theValue > maxIntensityValue)
                    {
                        maxIntensityValue = theValue;
                    }
                }

                foreach (var zone in zones)
                {
                    var gf = new GeometryFeature();
                    gf.Geometry = zone.Boundary;
                    gf["Drivers"] = zone.CarTruckVanDrivers;
                    gf["Population"] = zone.Population;
                    gf["GeoCode"] = zone.GeoCode;

                    double zoneAreaToUse = zone.RatioOfArea;
                    if (zoneAreaToUse < 0.0001f)
                    {
                        zoneAreaToUse = 0.0001f;
                    }

                    // Normalized intensity 0.0 → 1.0
                    double intensity = ((zone.CarTruckVanDrivers / (double)(maxDrivers)) * 1.0f / zoneAreaToUse) / maxIntensityValue;

                    gf["Intensity"] = intensity;

                    features.Add(gf);
                }

                var layer = new MemoryLayer("Census Zones");
                layer.Features = features;
                layer.Opacity = 0.45f;
                layer.MinVisible = 3.0f;
                layer.MaxVisible = 15.0f;
                layer.Style = new ThemeStyle(f =>
                {
                    double intensity = 0.0;
                    if (f is GeometryFeature gf && gf["Intensity"] is double val)
                        intensity = val;

                    // Lerp from blue (low) → red (high)
                    int r = (int)(intensity * 255);
                    int g = 40;
                    int b = (int)((1.0 - intensity) * 255);

                    return new VectorStyle
                    {
                        Fill = new Mapsui.Styles.Brush(new Color(r, g, b, 120)),
                        Line = new Pen(new Color(80, 80, 80, 100), 0.5),
                        Outline = new Pen(new Color(80, 80, 80, 100), 0.5),
                    };
                });

                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(),
                    $"[Census] Created overlay layer with {features.Count} zone polygons"));

                return layer;
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(),
                    $"[Census] Failed to create overlay layer: {ex}"));
                return null;
            }
        }

        public static MemoryLayer? CreatePinLayer()
        {
            MemoryLayer? layer = null;

            try
            {
                layer = new MemoryLayer("PinLayer");
                MPoint mPoint = new MPoint(World.Offset.X, World.Offset.Y);
                PointFeature feature = new PointFeature(mPoint);

                PinLayerFeatures.Clear();
                PinLayerFeatures.Add(feature);

                layer.Features = PinLayerFeatures;

                layer.Opacity = 1.0f;

                PinStyles pinStyles = new PinStyles();
                layer.Style = pinStyles.CreateThemeStyle();

                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Created Pin Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to create Pin Layer {ex.ToString()}"));
            }

            return layer;
        }

        public static MemoryLayer? CreateSpawnerLayer()
        {
            MemoryLayer? layer = null;
            try
            {
                layer = new MemoryLayer("Spawners");
                layer.Features = SpawnerFeatures;
                layer.Opacity = 1.0f;
                SpawnerStyles spawnerStyles = new SpawnerStyles();
                layer.Style = spawnerStyles.CreateThemeStyle();
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Created Spawner Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to create Spawner Layer {ex.ToString()}"));
            }
            return layer;
        }

        public static void AddSpawnPoint(SpawnPoint spawnPoint)
        {
            SimManager.Instance.SpawnPoints.Add(spawnPoint);
            RebuildSpawnerFeatures();
        }

        public static void RemoveSpawnPoint(SpawnPoint spawnPoint)
        {
            SimManager.Instance.SpawnPoints.Remove(spawnPoint);
            RebuildSpawnerFeatures();
        }

        public static void MoveSpawnPoint(SpawnPoint spawnPoint, double x, double y, int nearestNodeId)
        {
            spawnPoint.X = x;
            spawnPoint.Y = y;
            spawnPoint.NearestNodeId = nearestNodeId;
            RebuildSpawnerFeatures();
        }

        public static void RebuildSpawnerFeatures()
        {
            SpawnerFeatures.Clear();
            foreach (SpawnPoint sp in SimManager.Instance.SpawnPoints)
            {
                MPoint mPoint = new MPoint(sp.X, sp.Y);
                GeometryFeature gf = new GeometryFeature(new NetTopologySuite.Geometries.Point(sp.X, sp.Y));
                gf["SpawnPointId"] = sp.Id;
                gf["VehiclesPerMinute"] = sp.VehiclesPerMinute;
                SpawnerFeatures.Add(gf);
            }

            if (spawnerLayer != null)
            {
                spawnerLayer.Features = SpawnerFeatures;
                spawnerLayer.DataHasChanged();
            }

            MainWindow.Instance.GetMap()?.Refresh();
        }

        public static SpawnPoint? FindSpawnPointByFeature(IFeature feature)
        {
            string? id = feature["SpawnPointId"]?.ToString();
            if (string.IsNullOrEmpty(id)) return null;
            return SimManager.Instance.SpawnPoints.Find(sp => sp.Id == id);
        }

        public static MemoryLayer? CreateDebugLayer()
        {
            MemoryLayer? layer = null;

            try
            {
                layer = new MemoryLayer("Debug");
                /*
                foreach (KeyValuePair<int, RoadNode> kvp in Sim.Sim.roadGraph.Nodes)
                {
                    MPoint mPoint = new MPoint(kvp.Value.X, kvp.Value.Y);
                    PointFeature pf = new PointFeature(mPoint);
                    pf["Node"] = kvp.Value.Id;

                    GraphLayerFeatures.Add(pf);
                }*/
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), "Started adding intersection bodies"));

                if (ProjectLayers.CreateRoadIntersections())
                {
                    SimManager.Instance.SetIntersectionBodiesCreated();
                }
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), "Done adding intersection bodies"));

                foreach (RoadIntersection r in SimManager.Instance.RoadIntersections)
                {
                    if (r.IsBodySet())
                    {
                        if (r.Body != null)
                        {
                            for (int i = 0; i < r.Body.polygon.count; i++)
                            {
                                Vector2 p0 = r.Body.polygon.vertices(i);
                                Vector2 p1 = r.Body.polygon.vertices((i + 1) % r.Body.polygon.count);
                                GeometryFeature feature = new GeometryFeature();
                                Coordinate[] coordinates = new Coordinate[2];

                                coordinates[0] = new Coordinate(World.Offset.X + (double)(p0.X + r.Center.X), World.Offset.Y + (double)(p0.Y + r.Center.Y));
                                coordinates[1] = new Coordinate(World.Offset.X + (double)(p1.X + r.Center.X), World.Offset.Y + (double)(p1.Y + r.Center.Y));

                                feature.Geometry = new LineString(coordinates);

                                DebugLayerFeatures.Add(feature);
                            }
                        }
                    }
                }

                /*show graph
                for (int i = 0; i < Sim.Sim.RoadGraph.Edges.Count; i++)
                {
                    int fromNodeIndex = Sim.Sim.RoadGraph.Edges[i].From;
                    int toNodeIndex = Sim.Sim.RoadGraph.Edges[i].To;

                    if (Sim.Sim.RoadGraph.Nodes.TryGetValue(fromNodeIndex, out RoadNode? fromNodeValue))
                    {
                        if (Sim.Sim.RoadGraph.Nodes.TryGetValue(toNodeIndex, out RoadNode? toNodeValue))
                        {
                            GeometryFeature feature = new GeometryFeature();
                            Coordinate[] coordinates = new Coordinate[2];

                            coordinates[0] = new Coordinate(fromNodeValue.X, fromNodeValue.Y);
                            coordinates[1] = new Coordinate(toNodeValue.X, toNodeValue.Y);

                            feature.Geometry = new LineString(coordinates);

                            DebugLayerFeatures.Add(feature);
                        }
                        else
                        {
                            EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Failed to get to Node"));
                        }
                    }
                    else
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Failed to get from Node"));
                    }
                }
                */
                layer.Opacity = 1.0f;

                VectorStyle orangeDotStyle = new VectorStyle { Line = new Pen { Color = Color.Pink, Width = 5 }, Outline = new Pen { Color = Color.Black, Width = 0.5 }, Fill = new Mapsui.Styles.Brush(Color.Orange) };

                layer.Style = orangeDotStyle;
                //layer.MaxVisible = 3.5f;

                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Created Graph Node Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to create Graph Node Layer {ex.ToString()}"));
            }

            return layer;
        }

        public static bool IsZoomedToLayer()
        {
            return isZoomedToLayer;
        }

        public static void ZoomToLayer(Map map)
        {
            if (map == null)
            {
                return;
            }

            //MRect? extent = map.Extent;

            MRect? extent = ProjectLayers.TryGetRoadLayerExtent();
            if (extent != null && double.IsNaN(extent.Centroid.X))
            {
                extent = ProjectLayers.TryGetRoadLayerExtent();//Fallback to reading background extent if failed to get map extents
            }

            if (extent != null && !double.IsNaN(extent.Centroid.X))//Only create the image if we could get the extents
            {
                MRect panBounds = extent;

                if (map != null)
                {
                    // panBounds?.Multiply(5.0f);
                    //https://github.com/Mapsui/Mapsui/blob/main/Samples/Mapsui.Samples.Common/Maps/Navigation/KeepWithinExtentSample.cs

                    if (panBounds != null)
                    {
                        //map.Navigator.OverridePanBounds = panBounds;
                        map.Navigator.OverrideZoomBounds = new MMinMax(0.01, 2500);
                        double centerX = extent.MinX + (extent.MaxX - extent.MinX) / 2;
                        double centerY = extent.MinY + (extent.MaxY - extent.MinY) / 2;

                        double resolution = Math.Max(extent.Width / 1024, extent.Height / 768);
                        Viewport viewport = new Viewport(centerX, centerY, resolution, 0, 1024, 768);

                        map.Navigator.CenterOnAndZoomTo(new MPoint(centerX,
                            centerY), resolution);

                        map.Refresh();
                    }
                }

                isZoomedToLayer = true;
            }
            else
            {
                SetDefaultZoomLimit(map);
            }
        }

        //Only call from UI
        //Add a default zoom limit, otherwise there is a crash if trying if scrolling
        //with middle mouse if never zoomed to a layer
        public static void SetDefaultZoomLimit(Map myMap)
        {
            if (myMap == null)
            {
                return;
            }

            MRect extent = new MRect(0, 0, 500, 500);
            MRect panBounds = extent;

            if (myMap != null)
            {
                panBounds?.Multiply(5.0f);
                //https://github.com/Mapsui/Mapsui/blob/main/Samples/Mapsui.Samples.Common/Maps/Navigation/KeepWithinExtentSample.cs

                if (panBounds != null)
                {
                    myMap.BackColor = Color.White;

                    //myMap.Navigator.OverridePanBounds = panBounds;
                    myMap.Navigator.OverrideZoomBounds = new MMinMax(0.1, 2500);

                    myMap.Navigator.CenterOnAndZoomTo(new MPoint(extent.MinX + (extent.MaxX - extent.MinX) / 2,
                        extent.MinY + (extent.MaxY - extent.MinY) / 2), 15.0);
                }
            }
        }

        //Only call from UI
        public static void AddLayers(Map myMap)
        {
            myMap.Layers.Clear();

            if (IsRasterVisible && backgroundLayer != null)
            {
                myMap?.Layers.Add(backgroundLayer);
            }
            if (IsRoadVisible)
            {
                if (roadLayerFirstPass != null)
                {
                    myMap?.Layers.Add(roadLayerFirstPass);
                }
                if (roadLayerSecondPass != null)
                {
                    myMap?.Layers.Add(roadLayerSecondPass);
                }

                if (roadLabelLayer != null)
                {
                    myMap?.Layers.Add(roadLabelLayer);
                }
            }

            if (roadSelectionLayer == null)
                roadSelectionLayer = CreateRoadSelectionLayer();
            myMap?.Layers.Add(roadSelectionLayer);

            if (pathOverlayLayer == null)
                pathOverlayLayer = CreatePathOverlayLayer();
            myMap?.Layers.Add(pathOverlayLayer);

            if (intersectionOverlayLayer == null)
                intersectionOverlayLayer = CreateIntersectionOverlayLayer();
            myMap?.Layers.Add(intersectionOverlayLayer);

            if (IsCensusOverlayVisible && censusOverlayLayer != null)
            {
                myMap?.Layers.Add(censusOverlayLayer);
            }
            if (IsIntersectionsVisible && intersectionLayer != null)
            {
                myMap?.Layers.Add(intersectionLayer);
            }

            if (vehicleLayer != null)
            {
                myMap?.Layers.Add(vehicleLayer);
            }

            if (IsSpawnersVisible && spawnerLayer != null)
            {
                myMap?.Layers.Add(spawnerLayer);
            }

            if (PinLayer != null)
            {
                myMap?.Layers.Add(PinLayer);
            }

            if (debugLayer != null)
            {
                myMap?.Layers.Add(debugLayer);
            }

            Map? map = MainWindow.Instance.GetMap();
            if (map != null)
            {
                map.Refresh();
            }
        }

        public static void UpdateVehicleLayer(bool fullClone, Map? map)
        {
            if (vehicleLayer != null && map != null)
            {
                if (fullClone)
                {
                    MRect extent = map.Navigator.Viewport.ToExtent();

                    List<IFeature> copyOfVehiclesFeatures = new List<IFeature>();
                    lock (SimManager.Instance.LockChangeVehicleFeatureList)
                    {
                        foreach (IFeature v in VehicleFeatures)
                        {
                            if (map.Navigator.Viewport.Resolution <= vehicleLayer.MaxVisible)
                            {
                                if (v is PointFeature pf)
                                {
                                    if (extent.Contains(pf.Point))
                                    {
                                        copyOfVehiclesFeatures.Add((IFeature)pf.Clone());
                                    }
                                }
                            }
                        }
                    }

                    EventQueueForUI.Instance.Add(new UpdatedVehicleMapEvent(copyOfVehiclesFeatures));
                }
                else
                {
                    EventQueueForUI.Instance.Add(new UpdatedVehicleMapEvent(VehicleFeatures));
                }
            }
        }

        //Only call from UI
        public static void SetVehicleLayerDataChanged(List<IFeature> copyOfVehiclesFeatures)
        {
            if (vehicleLayer != null)
            {
                vehicleLayer.Features = copyOfVehiclesFeatures;
                vehicleLayer.FeaturesWereModified();
                vehicleLayer.DataHasChanged();
            }
        }

        //Only call from UI
        public static void ClearLayers(Map myMap)
        {
            myMap.Layers.Clear();
        }

        public static void NewProject()
        {
            currentProjectFile = new ProjectFile();
            SimManager.Instance.SetProjectNameChanged();
            resetLayers();
            EventQueueForUI.Instance.Add(new SetProjectEvent(currentProjectFile));
        }
    }
}