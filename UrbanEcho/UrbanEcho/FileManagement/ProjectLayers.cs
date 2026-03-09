using Avalonia.Media;
using BruTile;
using BruTile.MbTiles;
using BruTile.Wms;
using FluentAvalonia.Core;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using Mapsui.Tiling.Layers;
using Mapsui.UI;
using Mapsui.UI.Avalonia;
using NetTopologySuite.Geometries;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Graph;
using UrbanEcho.Helpers;
using UrbanEcho.Models;
using UrbanEcho.Physics;
using UrbanEcho.Sim;
using UrbanEcho.Styles;
using UrbanEcho.ViewModels;
using Color = Mapsui.Styles.Color;
using Exception = System.Exception;
using Layer = Mapsui.Layers.Layer;
using Pen = Mapsui.Styles.Pen;

namespace UrbanEcho.FileManagement
{
    public static class ProjectLayers
    {
        private static TileLayer? backgroundMBTile;

        private static RasterizingLayer? roadLayerFirstPass;
        private static RasterizingLayer? roadLayerSecondPass;
        private static Layer? intersectionLayer;
        private static MemoryLayer? vehicleLayer;
        private static MemoryLayer? roadSelectionLayer;

        private static RasterizingLayer? debugLayer;

        private static MemoryLayer? censusOverlayLayer;

        private static bool backgroundRequiresLoading = false;
        private static bool roadRequiresLoading = false;
        private static bool intersectionRequiresLoading = false;
        private static bool vehicleRequiresLoading = true;

        private static bool backgroundLoaded = false;
        private static bool roadLoaded = false;
        private static bool intersectionLoaded = false;
        private static bool vehicleLoaded = false;

        private static bool isZoomedToLayer = false;

        private static ProjectFile? currentProjectFile = new ProjectFile();
        public static bool IsVolumeVisible { get; set; } = true;
        public static bool IsTrafficSpeedVisible { get; set; } = true;
        public static bool IsRasterVisible { get; set; } = true;
        public static bool IsIntersectionsVisible { get; set; } = true;
        public static bool IsCensusOverlayVisible { get; set; } = false;

        //private static List<IFeature> RoadFeatures = new List<IFeature>();

        public static List<IFeature> VehicleFeatures = new List<IFeature>();

        public static List<IFeature> DebugLayerFeatures = new List<IFeature>();

        public static MPoint CenterOfMap = new MPoint();

        public static Layer? IntersectionLayer => intersectionLayer;
        public static MemoryLayer? VehicleLayer => vehicleLayer;

        public static void LoadProject(string path)
        {
            ProjectFile? openProject = ProjectFile.Open(path);

            if (openProject != null)
            {
                //Clear queue incase UI is currently updating vehicle layer
                EventQueueForUI.Instance.Clear();

                currentProjectFile = openProject;

                resetLayers();

                if (Load(currentProjectFile))
                {
                    if (Sim.Sim.MyMap != null)
                    {
                        EventQueueForUI.Instance.Add(new AddLayersEvent(Sim.Sim.MyMap));
                        EventQueueForUI.Instance.Add(new ZoomEvent(Sim.Sim.MyMap));
                    }
                }
                else
                {
                    if (Sim.Sim.MyMap != null)
                    {
                        ClearLayers(Sim.Sim.MyMap);
                    }
                }

                EventQueueForUI.Instance.Add(new SetProjectEvent(currentProjectFile));
            }
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
                backgroundLoaded = false;
                if (Load(currentProjectFile))
                {
                    if (Sim.Sim.MyMap != null)
                    {
                        EventQueueForUI.Instance.Add(new AddLayersEvent(Sim.Sim.MyMap));
                        EventQueueForUI.Instance.Add(new ZoomEvent(Sim.Sim.MyMap));
                    }
                }

                EventQueueForUI.Instance.Add(new SetProjectEvent(currentProjectFile));
            }
        }

        public static MRect? TryGetBackgroundExtent()
        {
            MRect? returnValue = null;
            if (backgroundLoaded)
            {
                if (backgroundMBTile != null)
                {
                    returnValue = backgroundMBTile.Extent;
                }
            }
            return returnValue;
        }

        public static void LoadRoadFile(string path)
        {
            if (currentProjectFile != null)
            {
                currentProjectFile.RoadLayerPath = path;
                roadRequiresLoading = true;
                roadLoaded = false;

                if (Load(currentProjectFile))
                {
                    if (Sim.Sim.MyMap != null)
                    {
                        EventQueueForUI.Instance.Add(new AddLayersEvent(Sim.Sim.MyMap));
                        EventQueueForUI.Instance.Add(new ZoomEvent(Sim.Sim.MyMap));
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

                if (Load(currentProjectFile))
                {
                    if (Sim.Sim.MyMap != null)
                    {
                        EventQueueForUI.Instance.Add(new AddLayersEvent(Sim.Sim.MyMap));
                        EventQueueForUI.Instance.Add(new ZoomEvent(Sim.Sim.MyMap));
                    }
                }

                EventQueueForUI.Instance.Add(new SetProjectEvent(currentProjectFile));
            }
        }

        public static bool LayersNeedReAdd()
        {
            return backgroundRequiresLoading || roadRequiresLoading || intersectionRequiresLoading;
        }

        private static void resetLayers()
        {
            backgroundRequiresLoading = true;
            roadRequiresLoading = true;
            intersectionRequiresLoading = true;
            vehicleRequiresLoading = true;

            backgroundLoaded = false;
            roadLoaded = false;
            intersectionLoaded = false;
            vehicleLoaded = false;
            backgroundMBTile = null;
            roadLayerFirstPass = null;
            roadLayerSecondPass = null;
            intersectionLayer = null;
            vehicleLayer = null;
            roadSelectionLayer = null;
            debugLayer = null;
            censusOverlayLayer = null;

            Sim.Sim.Clear(); //Clear all the existing lists
            //RoadFeatures = new List<IFeature>();
            VehicleFeatures = new List<IFeature>();
            DebugLayerFeatures = new List<IFeature>();

            Map? map = Sim.Sim.MyMap;
            if (map != null)
            {
                map.Refresh();
            }
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
                        backgroundMBTile = CreateMbTilesLayer(currentProjectFile.BackgroundLayerPath, "background");

                        if (backgroundMBTile != null)
                        {
                            addLayer = true;
                            backgroundLoaded = true;
                            backgroundRequiresLoading = false;
                        }
                    }
                    else
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Project file did not contain a background layer"));
                    }
                }
                if (roadRequiresLoading == true)
                {
                    if (currentProjectFile.RoadLayerPath != "")
                    {
                        try
                        {
                            ShapeFile roadNetwork = new ShapeFile(currentProjectFile.RoadLayerPath);
                            EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Load Road Shape File"));
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
                                    if (roadLayer.DataSource != null)
                                    {
                                        List<IFeature> RoadFeatures = Helpers.Helper.GetFeatures(roadLayer.DataSource);
                                        Sim.Sim.RoadFeatures = new Dictionary<string, IFeature>();
                                        foreach (IFeature feature in RoadFeatures)
                                        {
                                            string key = Helper.TryGetFeatureKVPToString(feature, "OBJECTID", "");

                                            if (!string.IsNullOrEmpty(key))
                                            {
                                                IFeature newFeature = feature.Copy();
                                                newFeature["VehicleCount"] = 0;
                                                newFeature["FromToSpeed"] = 0.0;
                                                newFeature["ToFromSpeed"] = 0.0;
                                                newFeature["Speed"] = 0.0;
                                                Sim.Sim.RoadFeatures.TryAdd(key, newFeature);
                                            }
                                        }

                                        Sim.Sim.RoadGraph = UrbanTrafficSim.Core.IO.RoadGraphLoader.LoadFromFeatures(Helpers.Helper.GetFeatures(roadLayer.DataSource));
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Failed to load Road Layer {ex.ToString()}"));
                        }

                        if (roadLayerFirstPass != null && roadLayerSecondPass != null)
                        {
                            addLayer = true;
                            roadLoaded = true;
                            roadRequiresLoading = false;
                        }
                    }
                    else
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Project file did not contain a road layer"));
                    }
                }
                if (intersectionRequiresLoading == true)
                {
                    if (currentProjectFile.IntersectionLayerPath != "")
                    {
                        try
                        {
                            ShapeFile intersections = new ShapeFile(currentProjectFile.IntersectionLayerPath);
                            EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Loaded Intersections Shape File"));
                            intersectionLayer = CreateIntersectionsLayer(intersections, "Intersections");
                        }
                        catch (Exception ex)
                        {
                            EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Failed to load Intersection Layer {ex.ToString()}"));
                        }

                        if (intersectionLayer != null)
                        {
                            addLayer = true;
                            intersectionLoaded = true;
                            intersectionRequiresLoading = false;
                        }
                    }
                    else
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Project file did not contain a intersection layer"));
                    }
                }

                if (vehicleRequiresLoading && intersectionLoaded && roadLoaded)
                {
                    //MemoryLayer tempDebugLayer = CreateDebugLayer();//use this layer for testing
                    //tempDebugLayer.Features = DebugLayerFeatures;
                    //debugLayer = new RasterizingLayer(tempDebugLayer);
                    ////TODO: if we are going to load new road network we should probably destroy box
                    ///2d world and dispose any handles created in the
                    ///IntersectionBody file. Then create a new world and make new shapes again

                    vehicleRequiresLoading = false;
                    EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Initialize Graph"));
                    Sim.Sim.InitializeGraph();

                    if (Sim.Sim.RoadGraph != null)
                    {
                        TrafficVolumeLoader.AssignToGraph(Sim.Sim.RoadGraph);
                    }

                    try
                    {
                        // Load census data for realistic spawn distribution (before vehicle creation)
                        if (!string.IsNullOrEmpty(currentProjectFile?.CensusLayerPath))
                        {
                            Sim.Sim.InitializeCensusSpawning(currentProjectFile.CensusLayerPath);
                        }
                        else
                        {
                            string defaultCensusPath = "Resources/ShapeFiles/Census_2021_Work_Commuting/GIS_DATA_CENSUS_2021_WORK_COMMUTING.shp";
                            if (System.IO.File.Exists(defaultCensusPath))
                            {
                                Sim.Sim.InitializeCensusSpawning(defaultCensusPath);
                                if (currentProjectFile != null)
                                {
                                    currentProjectFile.CensusLayerPath = defaultCensusPath;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Failed to load a census layer {ex.Message}"));
                    }

                    vehicleLayer = CreateVehicleLayer();

                    // Build census zone overlay if census data was loaded
                    if (Sim.Sim.CensusSpawn != null && Sim.Sim.CensusSpawn.IsLoaded)
                    {
                        censusOverlayLayer = CreateCensusOverlayLayer(Sim.Sim.CensusSpawn.Zones);
                    }
                }
            }
            else
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Failed To add layers project could not be referenced"));
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

        //https://github.com/BruTile/BruTile
        public static TileLayer? CreateMbTilesLayer(string path, string name)
        {
            TileLayer? mbTilesLayer = null;
            try
            {
                MbTilesTileSource mbTilesTileSource = new MbTilesTileSource(new SQLiteConnectionString(path, true));
                mbTilesLayer = new TileLayer(mbTilesTileSource) { Name = name };
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Loaded MBTiles Background File"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Failed to create tile layer {ex.ToString()}"));
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
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Created Background Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Failed to load Background Layer {ex.ToString()}"));
            }

            return layer;
        }

        public static Layer? CreateIntersectionsLayer(IProvider source, string name)
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

                layer.Opacity = 1.0f;

                layer.MaxVisible = 3.5f;

                layer.DataSource = projectingProvider;

                if (World.Created == false)
                {
                    MRect? extent = layer?.Extent;
                    if (extent != null)
                    {
                        CenterOfMap = new MPoint(extent.MinX + (extent.MaxX - extent.MinX) / 2,
                                    extent.MinY + (extent.MaxY - extent.MinY) / 2);
                    }
                    World.Init(CenterOfMap.X, CenterOfMap.Y);
                }

                if (World.Created == false)
                {
                    return null;
                }

                IntersectionStyles intersectionsStyle = new IntersectionStyles();

                layer.Style = intersectionsStyle.CreateThemeStyle();
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Created Intersections Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Unable to create Intersections Layer {ex.ToString()}"));
            }

            return layer;
        }

        public static bool CreateRoadIntersections()
        {
            if (intersectionLayer is null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Intersections layer was not added before trying to add intersection bodies"));
                return false;
            }

            IProvider? intersectionDatasource = intersectionLayer.DataSource;
            if (intersectionDatasource is null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Datasource used for adding intersection bodies was null"));
                return false;
            }
            List<IFeature> features = Helpers.Helper.GetFeatures(intersectionDatasource);

            if (Sim.Sim.RoadGraph is null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Intersections can not be added before road graph"));
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
                            RoadIntersection? r = RoadIntersection.Create(name, feature, Sim.Sim.RoadGraph);

                            if (r is not null)
                            {
                                Sim.Sim.RoadIntersections.Add(r);
                            }
                        }
                    }
                }
            }
            return true;
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

                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Created Road Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Failed to create road layer {ex.ToString()}"));
            }
            return layer;
        }

        public static MemoryLayer? CreateVehicleLayer()
        {
            MemoryLayer? layer = null;

            if (World.Created == false)
            {
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
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Unable To Create Vehicle Layer if box2d world was not initialized"));
                return null;
            }

            try
            {
                layer = new MemoryLayer("Vehicles");
                /*
                int vehiclesAdded = 0;
                Random random = new Random();

                // Build a weighted spawn list so high-AADT edges get more vehicles (fallback)
                var weightedEdgeIndices = Sim.Sim.RoadGraph != null
                    ? TrafficVolumeLoader.BuildWeightedEdgeSpawnList(Sim.Sim.RoadGraph)
                    : new List<int>();

                //// Scale vehicle count to observed AADT rather than edge count.
                //// Each simulated vehicle represents ~scaleFactor worth of daily traffic.
                //double totalAADT = Sim.Sim.RoadGraph?.Edges
                //    .Where(e => e.Metadata.TrafficVolume > 100) // exclude defaulted edges
                //    .Sum(e => e.Metadata.TrafficVolume) ?? 0;

                //const double scaleFactor = 0.00004;
                //int spawnCount = Math.Clamp((int)(totalAADT * scaleFactor), 50, 600);
                int spawnCount = Sim.Sim.RoadGraph?.Edges.Count / 2 ?? 0;

                for (int v = 0; v < spawnCount; v++)
                {
                    // Use census spawn manager if available, otherwise fall back to AADT
                    int spawnNodeId;
                    if (Sim.Sim.CensusSpawn != null && Sim.Sim.CensusSpawn.IsLoaded)
                    {
                        spawnNodeId = Sim.Sim.CensusSpawn.PickWeightedSpawnNode();
                    }
                    else
                    {
                        int edgeIdx = weightedEdgeIndices.Count > 0
                            ? weightedEdgeIndices[random.Next(weightedEdgeIndices.Count)]
                            : v;
                        spawnNodeId = Sim.Sim.RoadGraph!.Edges[edgeIdx].From;
                    }

                    // Find an outgoing edge from the chosen spawn node
                    var outgoing = Sim.Sim.RoadGraph!.GetOutgoingEdges(spawnNodeId);
                    if (outgoing.Count == 0) continue;

                    var edge = outgoing[random.Next(outgoing.Count)];

                    if (Sim.Sim.RoadGraph.Nodes.TryGetValue(edge.From, out RoadNode? roadNodeFrom))
                    {
                        if (Sim.Sim.RoadGraph.Nodes.TryGetValue(edge.To, out RoadNode? roadNodeTo))
                        {
                            if (roadNodeFrom != null && roadNodeTo != null)
                            {
                                double randomValue = Random.Shared.NextDouble();
                                double truckRatio = 0.1f;
                                bool isTruck = false;
                                if (randomValue <= truckRatio)
                                {
                                    isTruck = true;
                                }

                                MPoint mPoint = new MPoint(roadNodeFrom.X, roadNodeFrom.Y);
                                PointFeature pf = new PointFeature(mPoint);

                                pf["VehicleNumber"] = vehiclesAdded;
                                pf["Hidden"] = "true";
                                pf["Angle"] = 0.0f;
                                string type = "RegularCar";
                                if (!isTruck)
                                {
                                    pf["VehicleType"] = "Car" + random.Next(0, VehicleStyles.NumberOFCarColors);
                                }
                                else
                                {
                                    pf["VehicleType"] = "Truck" + random.Next(0, VehicleStyles.NumberOFTruckColors);
                                    type = "TransportTruck";
                                }
                                //Vehicle groups used so we don't raycast and update velocities every frame (was slowing down fps)
                                //currently vehicle groups just set as 1 so vehicle groups is bypassed
                                Vehicle vehicle = new Vehicle(pf, edge, type, vehiclesAdded % Helper.NumberOfVehicleGroups, Sim.Sim.RoadGraph);

                                if (vehicle.IsCreated)
                                {
                                    vehiclesAdded++;
                                    UrbanEcho.Sim.Sim.Vehicles.Add(vehicle);
                                    VehicleFeatures.Add(pf);
                                }
                                else
                                {
                                    PointFeature pfFailed = new PointFeature(mPoint);

                                    DebugLayerFeatures.Add(pfFailed);
                                }
                            }
                        }
                    }
                }*/

                layer.Features = VehicleFeatures;

                layer.Opacity = 1.0f;

                layer.MaxVisible = 1.75f;

                VehicleStyles vehiclesStyle = new VehicleStyles();

                layer.Style = vehiclesStyle.CreateThemeStyle();

                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Created Vehicles Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Failed to create Vehicle Layer {ex.ToString()}"));
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

                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(),
                    $"[Census] Created overlay layer with {features.Count} zone polygons"));

                return layer;
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(),
                    $"[Census] Failed to create overlay layer: {ex}"));
                return null;
            }
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
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), "Started adding intersection bodies"));

                if (ProjectLayers.CreateRoadIntersections())
                {
                    Sim.Sim.SetIntersectionBodiesCreated();
                }
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), "Done adding intersection bodies"));

                foreach (RoadIntersection r in Sim.Sim.RoadIntersections)
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

                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Created Graph Node Layer"));
            }
            catch (Exception ex)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Failed to create Graph Node Layer {ex.ToString()}"));
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

            MRect? extent = map.Extent;
            if (extent != null)
            {
                MRect panBounds = extent;

                if (map != null)
                {
                    panBounds?.Multiply(5.0f);
                    //https://github.com/Mapsui/Mapsui/blob/main/Samples/Mapsui.Samples.Common/Maps/Navigation/KeepWithinExtentSample.cs

                    if (panBounds != null)
                    {
                        map.Navigator.OverridePanBounds = panBounds;
                        map.Navigator.OverrideZoomBounds = new MMinMax(0.01, 50);

                        map.Navigator.CenterOnAndZoomTo(new MPoint(extent.MinX + (extent.MaxX - extent.MinX) / 2,
                            extent.MinY + (extent.MaxY - extent.MinY) / 2), 15.0);
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
        /* old version of ZoomToLayer
        public static void ZoomToLayer(Map map)
        {
            if (map == null)
            {
                return;
            }

            if (backgroundMBTile != null)
            {
                TileLayer layer = backgroundMBTile;
                if (layer.Extent != null)
                {
                    MRect extent = layer.Extent;
                    MRect panBounds = extent;

                    if (map != null)
                    {
                        panBounds?.Multiply(5.0f);
                        //https://github.com/Mapsui/Mapsui/blob/main/Samples/Mapsui.Samples.Common/Maps/Navigation/KeepWithinExtentSample.cs

                        if (panBounds != null)
                        {
                            map.Navigator.OverridePanBounds = panBounds;
                            map.Navigator.OverrideZoomBounds = new MMinMax(0.01, 50);

                            map.Navigator.CenterOnAndZoomTo(new MPoint(extent.MinX + (extent.MaxX - extent.MinX) / 2,
                                extent.MinY + (extent.MaxY - extent.MinY) / 2), 15.0);
                        }
                    }

                    isZoomedToLayer = true;
                }
            }
            else
            {
                SetDefaultZoomLimit(map);
            }
        }*/

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

                    myMap.Navigator.OverridePanBounds = panBounds;
                    myMap.Navigator.OverrideZoomBounds = new MMinMax(0.1, 50);

                    myMap.Navigator.CenterOnAndZoomTo(new MPoint(extent.MinX + (extent.MaxX - extent.MinX) / 2,
                        extent.MinY + (extent.MaxY - extent.MinY) / 2), 15.0);
                }
            }
        }

        //Only call from UI
        public static void AddLayers(Map myMap)
        {
            myMap.Layers.Clear();

            if (IsRasterVisible && backgroundMBTile != null)
            {
                myMap?.Layers.Add(backgroundMBTile);
            }
            if (roadLayerFirstPass != null)
            {
                myMap?.Layers.Add(roadLayerFirstPass);
            }
            if (roadLayerSecondPass != null)
            {
                myMap?.Layers.Add(roadLayerSecondPass);
            }

            if (roadSelectionLayer == null)
                roadSelectionLayer = CreateRoadSelectionLayer();
            myMap?.Layers.Add(roadSelectionLayer);
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
            /*
            if (debugLayer != null)
            {
                myMap?.Layers.Add(debugLayer);
            }*/

            Map? map = Sim.Sim.MyMap;
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
                    lock (Sim.Sim.LockChangeVehicleFeatureList)
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
            resetLayers();
            EventQueueForUI.Instance.Add(new SetProjectEvent(currentProjectFile));
        }
    }
}