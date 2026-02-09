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

        public static void SetMapControl(MapControl mapControl)
        {
            MyMapControl = mapControl;
        }

        public static void Run()
        {
            MapControl? mapControl = MyMapControl;

            if (mapControl is null)
            {
                //If map is not on UI close task
                return;
            }

            while (Cts.IsCancellationRequested == false)
            {
                bool addLayer = IsLayerAdded();

                if (isZoomedToLayer == false || addLayer == true)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (isZoomedToLayer == false)
                        {
                            ZoomToLayer(mapControl);
                        }

                        if (addLayer == true)
                        {
                            AddLayer(mapControl);
                        }
                    });
                }

                if (haveGotFeatures == false)
                {
                    GetFeatures(mapControl);
                }

                //So Computer doesn't use 100% CPU
                Thread.Sleep(100);
            }
        }

        private static void GetFeatures(MapControl mapControl)
        {
            if (roadNetwork != null)
            {
                roadNetworklist = Helper.GetFeatures(roadNetwork, mapControl.Map).ToList();
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
            if (loadedBackground == false)
            {
                //TileLayer backgroundMBTile = CreateMbTilesLayer(Path.GetFullPath(Path.Combine("Resources\\Rasters", "LandCover19.mbtiles")), "regular");

                //backgroundMBTile = CreateLayers.CreateMbTilesLayer(Path.GetFullPath(Path.Combine("Resources\\Rasters", "LandCover19.mbtiles")), "background");
                backgroundMBTile = CreateLayers.CreateMbTilesLayer(Path.GetFullPath(Path.Combine("Resources\\Rasters", "Aerial2.mbtiles")), "background");

                loadedBackground = true;
                addLayer = true;
            }
            else if (loadedRoad == false)
            {
                string roadNetworkPath = Path.Combine("Resources\\ShapeFiles\\Road_Network", "Road_Network.shp");
                roadNetwork = new ShapeFile(roadNetworkPath);
                roadLayerFirstPass = new RasterizingLayer(CreateLayers.CreateRoadLayer(roadNetwork, "Road Outline", true, false));
                roadLayerSecondPass = new RasterizingLayer(CreateLayers.CreateRoadLayer(roadNetwork, "Roads", false, true));

                loadedRoad = true;
                addLayer = true;
            }
            else if (loadedIntersection == false)
            {
                string intersectionsPath = Path.Combine("Resources\\ShapeFiles\\intersections_kitchener", "intersections_kitchener.shp");
                ShapeFile intersections = new ShapeFile(intersectionsPath);
                intersectionLayer = CreateLayers.CreateIntersectionsLayer(intersections, "Intersections");

                loadedIntersection = true;
                addLayer = true;
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