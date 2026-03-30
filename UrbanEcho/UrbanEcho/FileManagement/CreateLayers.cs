using BruTile.MbTiles;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using Mapsui.Tiling.Layers;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.Graph;
using UrbanEcho.Models;
using UrbanEcho.Physics;
using UrbanEcho.Sim;
using UrbanEcho.Styles;
using NetTopologySuite.Geometries;

namespace UrbanEcho.FileManagement
{
    public static class CreateLayers
    {
        public static MemoryLayer CreateRoadSelectionLayer()
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

        public static MemoryLayer CreatePathOverlayLayer()
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

        public static MemoryLayer CreateIntersectionOverlayLayer()
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

        public static bool CreateRoadIntersections(ILayer? intersectionLayer)
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

        public static MemoryLayer? CreateVehicleLayer(RasterizingLayer? roadLayerFirstPass, List<IFeature> VehicleFeatures)
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
                    MPoint CenterOfMap = new MPoint(extent.MinX + (extent.MaxX - extent.MinX) / 2,
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

        public static MemoryLayer? CreatePinLayer(List<IFeature> PinLayerFeatures)
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

        public static MemoryLayer? CreateSpawnerLayer(List<IFeature> SpawnerFeatures)
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
    }
}