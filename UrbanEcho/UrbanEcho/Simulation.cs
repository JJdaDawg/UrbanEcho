using Avalonia.Threading;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Avalonia;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UrbanEcho.ViewModels;
using static Mapsui.MapBuilder;
using Layer = Mapsui.Layers.Layer;

namespace UrbanEcho
{
    public static class Simulation
    {
        public static CancellationTokenSource Cts = new CancellationTokenSource();

        public static Task? SimTask;

        private static MapControl? MyMapControl;

        private static List<IFeature> roadNetworklist = new List<IFeature>();

        private static TileLayer? backgroundMBTile;

        private static RasterizingLayer? roadLayerFirstPass;
        private static RasterizingLayer? roadLayerSecondPass;
        private static Layer? intersectionLayer;
        private static bool isZoomedToLayer = false;

        private static ShapeFile? roadNetwork;

        private static bool loadedBackground = false;
        private static bool loadedRoad = false;
        private static bool loadedIntersection = false;

        private static bool haveGotFeatures = false;

        private static ProjectFile? currentProjectFile = new ProjectFile();

        private static MainViewModel? mainViewModel;

        public static void SetMapControl(MapControl mapControl, MainViewModel setMainViewModel)
        {
            MyMapControl = mapControl;

            mainViewModel = setMainViewModel;
        }

        public static void Run()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            //TODO: Remove this once we have UI for loading project
            currentProjectFile = ProjectFile.Open("Resources/ProjectFiles/myFile.Json");

            bool addText = false;

            double timeToSleep = 0;

            Stopwatch fpsTimer = Stopwatch.StartNew();

            while (Cts.IsCancellationRequested == false)
            {
                bool addLayer = IsLayerAdded();
                string timeToSend = $"last sleep time {timeToSleep.ToString()}";
                if (isZoomedToLayer == false || addLayer == true || addText == true)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (MyMapControl != null)
                        {
                            if (isZoomedToLayer == false)
                            {
                                ZoomToLayer(MyMapControl);
                            }

                            if (addLayer == true)
                            {
                                AddLayer(MyMapControl);
                            }
                        }

                        if (addText == true)
                        {
                            mainViewModel?.UpdateConsoleText($"{timeToSend}");
                            addText = false;
                        }
                    });
                }

                if (haveGotFeatures == false)
                {
                    GetRoadNetworkFeatures();
                }

                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    addText = true;
                    stopwatch.Restart();
                }

                //So Computer doesn't use 100% CPU and we update 60 times a second
                //16.77ms is 1/(60Hz) so if time since last scan is less than that sleep for bit
                timeToSleep = 16.6667f - (double)(fpsTimer.ElapsedTicks) * 1000 / Stopwatch.Frequency;

                if (timeToSleep > 1)
                {
                    Thread.Sleep((int)timeToSleep);
                }
                fpsTimer.Restart();
            }
        }

        private static void GetRoadNetworkFeatures()
        {
            if (roadNetwork != null)
            {
                roadNetworklist = Helper.GetFeatures(roadNetwork).ToList();
            }
            if (roadNetworklist.Count > 0)
            {
                if (roadNetworklist[0] is BaseFeature f)
                {
                    object? o = f["LANES"];
                    if (o != null)
                    {
                        string? test = o.ToString();
                        haveGotFeatures = true;
                    }
                }
            }
        }

        private static bool IsLayerAdded()
        {
            bool addLayer = false;
            if (currentProjectFile != null)
            {
                if (loadedBackground == false)
                {
                    //TileLayer backgroundMBTile = CreateMbTilesLayer(Path.GetFullPath(Path.Combine("Resources\\Rasters", "LandCover19.mbtiles")), "regular");

                    //backgroundMBTile = CreateLayers.CreateMbTilesLayer(Path.GetFullPath(Path.Combine("Resources\\Rasters", "LandCover19.mbtiles")), "background");

                    //currentProjectFile.BackgroundLayerPath = Path.GetFullPath(Path.Combine("Resources\\Rasters", "Aerial2.mbtiles"));

                    backgroundMBTile = CreateLayers.CreateMbTilesLayer(currentProjectFile.BackgroundLayerPath, "background"); ;

                    loadedBackground = true;
                    addLayer = true;
                }
                if (loadedRoad == false)
                {
                    //currentProjectFile.RoadLayerPath = Path.Combine("Resources\\ShapeFiles\\Road_Network", "Road_Network.shp");

                    roadNetwork = new ShapeFile(currentProjectFile.RoadLayerPath);
                    roadLayerFirstPass = new RasterizingLayer(CreateLayers.CreateRoadLayer(roadNetwork, "Road Outline", true, false));
                    roadLayerSecondPass = new RasterizingLayer(CreateLayers.CreateRoadLayer(roadNetwork, "Roads", false, true));

                    loadedRoad = true;
                    addLayer = true;
                }
                if (loadedIntersection == false)
                {
                    //currentProjectFile.IntersectionLayerPath = Path.Combine("Resources\\ShapeFiles\\intersections_kitchener", "intersections_kitchener.shp");

                    ShapeFile intersections = new ShapeFile(currentProjectFile.IntersectionLayerPath);
                    intersectionLayer = CreateLayers.CreateIntersectionsLayer(intersections, "Intersections");

                    loadedIntersection = true;
                    addLayer = true;
                }
            }
            else
            {
                //TODO: Add errors for null project
            }

            return addLayer;
        }

        private static void ZoomToLayer(MapControl mapControl)
        {
            if (backgroundMBTile != null)
            {
                TileLayer layer = backgroundMBTile;
                if (layer.Extent != null)
                {
                    MRect extent = layer.Extent;
                    MRect panBounds = extent;

                    if (mapControl != null)
                    {
                        panBounds?.Multiply(5.0f);
                        //https://github.com/Mapsui/Mapsui/blob/main/Samples/Mapsui.Samples.Common/Maps/Navigation/KeepWithinExtentSample.cs

                        if (panBounds != null)
                        {
                            mapControl.Map.BackColor = Color.White;

                            mapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(extent.MinX + (extent.MaxX - extent.MinX) / 2,
                                extent.MinY + (extent.MaxY - extent.MinY) / 2), 15.0);
                            mapControl.Map.Navigator.OverridePanBounds = panBounds;
                            mapControl.Map.Navigator.OverrideZoomBounds = new MMinMax(0.1, 50);
                        }
                    }

                    isZoomedToLayer = true;
                }
            }
        }

        private static void AddLayer(MapControl mapControl)
        {
            mapControl.Map.Layers.Clear();

            if (backgroundMBTile != null)
            {
                mapControl.Map?.Layers.Add(backgroundMBTile);
            }
            if (roadLayerFirstPass != null)
            {
                mapControl.Map?.Layers.Add(roadLayerFirstPass);
            }
            if (roadLayerSecondPass != null)
            {
                mapControl.Map?.Layers.Add(roadLayerSecondPass);
            }
            if (intersectionLayer != null)
            {
                mapControl.Map?.Layers.Add(intersectionLayer);
            }
        }
    }
}