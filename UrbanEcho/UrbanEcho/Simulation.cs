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

        private static bool mapControlSet = false;

        public static void SetMapControl(MapControl mapControl)
        {
            MyMapControl = mapControl;
            if (MyMapControl != null)
            {
                mapControlSet = true;
            }
        }

        public static void Run()
        {
            //TODO: add a error for this, without this set we can't run task
            if (mapControlSet == false) return;

            //TODO: Remove this once we have UI for loading project
            currentProjectFile = ProjectFile.Open("Resources/ProjectFiles/myFile.Json");

            while (Cts.IsCancellationRequested == false)
            {
                bool addLayer = IsLayerAdded();

                if (isZoomedToLayer == false || addLayer == true)
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
                    });
                }

                if (haveGotFeatures == false)
                {
                    GetRoadNetworkFeatures();
                }

                //So Computer doesn't use 100% CPU
                //TODO: Add correct timing so we know how long to sleep given time spent and FPS we want
                Thread.Sleep(100);
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