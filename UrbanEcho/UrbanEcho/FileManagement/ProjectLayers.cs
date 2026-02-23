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
using Mapsui.Tiling.Layers;
using Mapsui.UI;
using Mapsui.UI.Avalonia;
using NetTopologySuite.Geometries;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Helpers;
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

        //private static MemoryProvider? vehicleProvider;
        private static RasterizingLayer? graphLayer;

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

        public static bool IsRasterVisible { get; set; } = true;
        public static bool IsIntersectionsVisible { get; set; } = true;

        private static List<IFeature> RoadFeatures = new List<IFeature>();

        public static List<IFeature> VehicleFeatures = new List<IFeature>();

        public static List<IFeature> GraphLayerFeatures = new List<IFeature>();

        public static MPoint CenterOfMap = new MPoint();

        public static void LoadProject(string path)
        {
            ProjectFile? openProject = ProjectFile.Open(path);

            if (openProject != null)
            {
                currentProjectFile = openProject;
                backgroundRequiresLoading = true;
                roadRequiresLoading = true;
                intersectionRequiresLoading = true;

                backgroundLoaded = false;
                roadLoaded = false;
                intersectionLoaded = false;
                vehicleLoaded = false;

                Load(currentProjectFile);
            }
        }

        public static void LoadBackgroundFile(string path)
        {
            if (currentProjectFile != null)
            {
                currentProjectFile.BackgroundLayerPath = path;
                backgroundRequiresLoading = true;
                backgroundLoaded = false;
                Load(currentProjectFile);
            }
        }

        public static void LoadRoadFile(string path)
        {
            if (currentProjectFile != null)
            {
                currentProjectFile.RoadLayerPath = path;
                roadRequiresLoading = true;
                roadLoaded = false;

                Load(currentProjectFile);
            }
        }

        public static void LoadIntersectionsFile(string path)
        {
            if (currentProjectFile != null)
            {
                currentProjectFile.IntersectionLayerPath = path;
                intersectionRequiresLoading = true;
                intersectionLoaded = false;

                Load(currentProjectFile);
            }
        }

        public static bool LayersNeedReAdd()
        {
            return backgroundRequiresLoading || roadRequiresLoading || intersectionRequiresLoading;
        }

        public static bool Load(ProjectFile currentProjectFile)
        {
            bool addLayer = false;
            if (currentProjectFile != null)
            {
                if (backgroundRequiresLoading == true)
                {
                    backgroundMBTile = CreateMbTilesLayer(currentProjectFile.BackgroundLayerPath, "background");

                    if (backgroundMBTile != null)
                    {
                        addLayer = true;
                        backgroundLoaded = true;
                        backgroundRequiresLoading = false;
                    }
                }
                if (roadRequiresLoading == true)
                {
                    try
                    {
                        ShapeFile roadNetwork = new ShapeFile(currentProjectFile.RoadLayerPath);
                        EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Load Road Shape File"));
                        Layer roadLayerFirst = CreateRoadLayer(roadNetwork, "Road Outline", true, false);
                        roadLayerFirstPass = new RasterizingLayer(roadLayerFirst);
                        roadLayerSecondPass = new RasterizingLayer(CreateRoadLayer(roadNetwork, "Roads", false, true));

                        RoadFeatures = Helpers.Helper.GetRoadNetworkFeatures(roadLayerFirst.DataSource);
                        Sim.Sim.roadGraph = UrbanTrafficSim.Core.IO.RoadGraphLoader.LoadFromFeatures(Helpers.Helper.GetFeatures(roadLayerFirst.DataSource));
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
                if (intersectionRequiresLoading == true)
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

                if (vehicleRequiresLoading && intersectionLoaded && roadLoaded)
                {
                    MemoryLayer tempGraphLayer = CreateGraphLayer();

                    //TODO: if we are going to load new road network we should probably destroy box
                    ///2d world and dispose any handles created in the
                    ///IntersectionBody file. Then create a new world and make new shapes again

                    MRect? extent = roadLayerFirstPass?.Extent;
                    if (extent != null)
                    {
                        CenterOfMap = new MPoint(extent.MinX + (extent.MaxX - extent.MinX) / 2,
                                    extent.MinY + (extent.MaxY - extent.MinY) / 2);
                    }
                    vehicleLayer = CreateVehicleLayer();
                    tempGraphLayer.Features = GraphLayerFeatures;
                    graphLayer = new RasterizingLayer(tempGraphLayer);
                    vehicleRequiresLoading = false;
                    EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Initialize Graph"));
                    Sim.Sim.InitializeGraph();
                }
            }
            else
            {
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.Sim.GetMainViewModel(), $"Failed To add layers no project"));
            }

            return addLayer;
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

        public static Layer? CreateRoadLayer(IProvider source, string name, bool doOutline, bool showAADT)
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

                //https://github.com/Mapsui/Mapsui/blob/42b59e9dad1fd9512f0114f8c8a3fd3f5666d330/Samples/Mapsui.Samples.Common/Maps/CustomStyleSample.cs#L16-L51

                RoadStyle style = new RoadStyle();
                if (style.Line != null)
                {
                    style.Line.PenStrokeCap = PenStrokeCap.Square;
                    style.Line.StrokeJoin = StrokeJoin.Bevel;
                    style.Line.StrokeMiterLimit = 10.0f;
                }

                if (style.Outline != null)
                {
                    style.Outline.PenStrokeCap = PenStrokeCap.Square;
                    style.Outline.StrokeJoin = StrokeJoin.Bevel;
                    style.Outline.StrokeMiterLimit = 10.0f;
                }

                style.UseOutline = doOutline;
                style.ShowAADT = showAADT;

                style.Opacity = 1.0f;
                style.Line = new Pen();
                style.Line.Color = Color.LightGrey;
                style.Outline = new Pen();
                style.Outline.Color = Color.GhostWhite;

                layer.Style = style;
                layer.Opacity = 1.0f;
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
                World.Init(CenterOfMap.X, CenterOfMap.Y);
            }

            try
            {
                layer = new MemoryLayer("Vehicles");
                /* Spawning at each road feature
                for (int i = 0; RoadFeatures.Count > i; i++)
                {
                    if (RoadFeatures[i] is GeometryFeature gf)
                    {
                        if (gf.Geometry is NetTopologySuite.Geometries.LineString l)
                        {
                            if (l.Coordinates.Length > 0)
                            {
                                MPoint mPoint = new MPoint(l.Coordinates[0].X, l.Coordinates[0].Y);
                                PointFeature pf = new PointFeature(mPoint);
                                pf["VehicleNumber"] = i;
                                pf["VehicleType"] = "RedCar";
                                pf["Hidden"] = false;
                                pf["Angle"] = 0.0f;
                                VehicleFeatures.Add(pf);

                                UrbanEcho.Sim.Sim.Vehicles.Add(new Vehicle(pf));
                            }
                        }
                    }
                }*/

                int vehiclesAdded = 0;
                // Spawning at each road graph From Edge point
                for (int i = 0; i < Sim.Sim.roadGraph?.Edges.Count; i++)
                {
                    if (Sim.Sim.roadGraph.Nodes.TryGetValue(Sim.Sim.roadGraph.Edges[i].From, out RoadNode? roadNodeFrom))
                    {
                        if (Sim.Sim.roadGraph.Nodes.TryGetValue(Sim.Sim.roadGraph.Edges[i].To, out RoadNode? roadNodeTo))
                        {
                            if (roadNodeFrom != null && roadNodeTo != null)
                            {
                                MPoint mPoint = new MPoint(roadNodeFrom.X, roadNodeFrom.Y);
                                PointFeature pf = new PointFeature(mPoint);
                                pf["VehicleNumber"] = i;
                                pf["VehicleType"] = "RedCar";
                                pf["Hidden"] = false;
                                pf["Angle"] = 0.0f;
                                //Vehicle groups used so we don't raycast and update velocities every frame (was slowing down fps)
                                Vehicle vehicle = new Vehicle(pf, roadNodeFrom, roadNodeTo, Sim.Sim.roadGraph?.Edges[i], "RegularCar", vehiclesAdded % Helper.NumberOfVehicleGroups);
                                vehiclesAdded++;
                                if (vehicle.IsCreated)
                                {
                                    UrbanEcho.Sim.Sim.Vehicles.Add(vehicle);
                                    VehicleFeatures.Add(pf);
                                }
                                else
                                {
                                    PointFeature pfFailed = new PointFeature(mPoint);

                                    GraphLayerFeatures.Add(pfFailed);
                                }
                            }
                        }
                    }
                }
                /* Spawning at each road feature
                for (int i = 0; RoadFeatures.Count > i; i++)
                {
                    if (RoadFeatures[i] is GeometryFeature gf)
                    {
                        if (gf.Geometry is NetTopologySuite.Geometries.LineString l)
                        {
                            if (l.Coordinates.Length > 0)
                            {
                                MPoint mPoint = new MPoint(l.Coordinates[0].X, l.Coordinates[0].Y);
                                PointFeature pf = new PointFeature(mPoint);
                                pf["VehicleNumber"] = i;
                                pf["VehicleType"] = "RedCar";
                                pf["Hidden"] = false;
                                pf["Angle"] = 0.0f;
                                VehicleFeatures.Add(pf);

                                UrbanEcho.Sim.Sim.Vehicles.Add(new Vehicle(pf));
                            }
                        }
                    }
                }*/

                layer.Features = VehicleFeatures;

                //vehicleProvider = new MemoryProvider(VehicleFeatures);

                //layer.DataSource = vehicleProvider;// .Features = (IEnumerable<IFeature>)VehicleFeatures.Select(v => (IFeature)v.Clone()).ToList();

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

        public static MemoryLayer? CreateGraphLayer()
        {
            MemoryLayer? layer = null;

            try
            {
                layer = new MemoryLayer("Graph");
                /*
                foreach (KeyValuePair<int, RoadNode> kvp in Sim.Sim.roadGraph.Nodes)
                {
                    MPoint mPoint = new MPoint(kvp.Value.X, kvp.Value.Y);
                    PointFeature pf = new PointFeature(mPoint);
                    pf["Node"] = kvp.Value.Id;

                    GraphLayerFeatures.Add(pf);
                }*/

                for (int i = 0; i < Sim.Sim.roadGraph.Edges.Count; i++)
                {
                    int fromNodeIndex = Sim.Sim.roadGraph.Edges[i].From;
                    int toNodeIndex = Sim.Sim.roadGraph.Edges[i].To;

                    if (Sim.Sim.roadGraph.Nodes.TryGetValue(fromNodeIndex, out RoadNode? fromNodeValue))
                    {
                        if (Sim.Sim.roadGraph.Nodes.TryGetValue(toNodeIndex, out RoadNode? toNodeValue))
                        {
                            GeometryFeature feature = new GeometryFeature();
                            Coordinate[] coordinates = new Coordinate[2];

                            coordinates[0] = new Coordinate(fromNodeValue.X, fromNodeValue.Y);
                            coordinates[1] = new Coordinate(toNodeValue.X, toNodeValue.Y);

                            feature.Geometry = new LineString(coordinates);

                            GraphLayerFeatures.Add(feature);
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

        //Only call from UI
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
            if (IsIntersectionsVisible && intersectionLayer != null)
            {
                myMap?.Layers.Add(intersectionLayer);
            }

            if (vehicleLayer != null)
            {
                myMap?.Layers.Add(vehicleLayer);
            }
            /*
            if (graphLayer != null)
            {
                myMap?.Layers.Add(graphLayer);
            }*/
        }

        public static void UpdateVehicleLayer(bool fullClone, Map? map)
        {
            if (vehicleLayer != null && map != null)
            {
                if (fullClone)
                {
                    MRect extent = map.Navigator.Viewport.ToExtent();

                    List<IFeature> copyOfVehiclesFeatures = new List<IFeature>();

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
        }
    }
}