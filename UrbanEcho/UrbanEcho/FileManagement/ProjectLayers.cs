using BruTile.MbTiles;

using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Avalonia;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Styles;

namespace UrbanEcho.FileManagement
{
    public static class ProjectLayers
    {
        private static TileLayer? backgroundMBTile;

        private static RasterizingLayer? roadLayerFirstPass;
        private static RasterizingLayer? roadLayerSecondPass;
        private static Layer? intersectionLayer;

        private static bool backgroundRequiresLoading = false;
        private static bool roadRequiresLoading = false;
        private static bool intersectionRequiresLoading = false;

        private static bool isZoomedToLayer = false;

        private static ProjectFile? currentProjectFile = new ProjectFile();

        public static bool IsRasterVisible { get; set; } = true;

        public static void LoadProject(string path)
        {
            ProjectFile? openProject = ProjectFile.Open(path);

            if (openProject != null)
            {
                currentProjectFile = openProject;
                backgroundRequiresLoading = true;
                roadRequiresLoading = true;
                intersectionRequiresLoading = true;
                Load(currentProjectFile);
            }
        }

        public static void LoadBackgroundFile(string path)
        {
            if (currentProjectFile != null)
            {
                currentProjectFile.BackgroundLayerPath = path;
                backgroundRequiresLoading = true;
                Load(currentProjectFile);
            }
        }

        public static void LoadRoadFile(string path)
        {
            if (currentProjectFile != null)
            {
                currentProjectFile.RoadLayerPath = path;
                roadRequiresLoading = true;
                Load(currentProjectFile);
            }
        }

        public static void LoadIntersectionsFile(string path)
        {
            if (currentProjectFile != null)
            {
                currentProjectFile.IntersectionLayerPath = path;
                intersectionRequiresLoading = true;
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
                    backgroundMBTile = CreateMbTilesLayer(currentProjectFile.BackgroundLayerPath, "background"); ;

                    if (backgroundMBTile != null)
                    {
                        addLayer = true;
                    }
                }
                if (roadRequiresLoading == true)
                {
                    try
                    {
                        ShapeFile roadNetwork = new ShapeFile(currentProjectFile.RoadLayerPath);
                        roadLayerFirstPass = new RasterizingLayer(CreateRoadLayer(roadNetwork, "Road Outline", true, false));
                        roadLayerSecondPass = new RasterizingLayer(CreateRoadLayer(roadNetwork, "Roads", false, true));
                    }
                    catch (Exception ex)
                    {
                        //TODO: Add error message
                    }

                    if (roadLayerFirstPass != null && roadLayerSecondPass != null)
                    {
                        addLayer = true;
                    }
                }
                if (intersectionRequiresLoading == true)
                {
                    try
                    {
                        ShapeFile intersections = new ShapeFile(currentProjectFile.IntersectionLayerPath);
                        intersectionLayer = CreateIntersectionsLayer(intersections, "Intersections");
                    }
                    catch (Exception ex)
                    {
                        //TODO: Add error message
                    }

                    if (intersectionLayer != null)
                    {
                        addLayer = true;
                    }
                }
                else
                {
                    //TODO: Add errors for null project
                }
            }

            return addLayer;
        }

        public static void ResetRequiresLoading()
        {
            backgroundRequiresLoading = false;
            roadRequiresLoading = false;
            intersectionRequiresLoading = false;
        }

        //https://github.com/BruTile/BruTile
        public static TileLayer? CreateMbTilesLayer(string path, string name)
        {
            TileLayer? mbTilesLayer = null;
            try
            {
                MbTilesTileSource mbTilesTileSource = new MbTilesTileSource(new SQLiteConnectionString(path, true));
                mbTilesLayer = new TileLayer(mbTilesTileSource) { Name = name };
            }
            catch (Exception ex)
            {
            }

            //TODO: Figure out how to check if this failed and show error

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
            }
            catch (Exception ex)
            {
            }
            //TODO: Figure out how to check if this failed and show error

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
                //TODO: Figure out how to check if this failed and show error

                IntersectionStyles intersectionsStyle = new IntersectionStyles();

                layer.Style = intersectionsStyle.CreateThemeStyle();
            }
            catch (Exception ex) { }

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
                //TODO: Figure out how to check if this failed and show error

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
            }
            catch (Exception ex) { }
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
                            map.Navigator.OverrideZoomBounds = new MMinMax(0.1, 50);

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
            if (intersectionLayer != null)
            {
                myMap?.Layers.Add(intersectionLayer);
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