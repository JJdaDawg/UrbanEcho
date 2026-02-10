using Avalonia.Threading;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;
using UrbanEcho.Helpers;
using UrbanEcho.ViewModels;
using static Mapsui.MapBuilder;
using Layer = Mapsui.Layers.Layer;

namespace UrbanEcho.Sim
{
    public static class Simulation
    {
        public static CancellationTokenSource Cts = new CancellationTokenSource();

        public static Task? SimTask;

        private static MapControl? MyMapControl;

        private static MainViewModel? mainViewModel;

        public static void SetMapControl(MapControl mapControl, MainViewModel setMainViewModel)
        {
            MyMapControl = mapControl;

            mainViewModel = setMainViewModel;
        }

        public static void Run()
        {
            //TODO: Remove this once we have UI for loading project
            //currentProjectFile = ProjectFile.Open("Resources/ProjectFiles/myFile.Json");
            LoadFileEvent loadProjectEvent = new LoadFileEvent(SimEnumTypes.FileType.ProjectFile, "Resources/ProjectFiles/myFile.Json");
            loadProjectEvent.Run();

            bool addLayer = ProjectLayers.LayersNeedReAdd();
            bool isZoomedToLayer = ProjectLayers.IsZoomedToLayer();

            FrameTimer frameTimer = new FrameTimer(true);

            bool addText = false;

            while (Cts.IsCancellationRequested == false)
            {
                if ((isZoomedToLayer == false) || (addLayer == true) || (addText == true))
                {
                    string timeToShow = frameTimer.TimeToShow();
                    bool localAddText = addText;
                    bool localIsZoomedToLayer = isZoomedToLayer;
                    bool localAddLayer = addLayer;

                    Dispatcher.UIThread.Post(() =>
                    {
                        if (MyMapControl != null)
                        {
                            if (localIsZoomedToLayer == false)
                            {
                                ProjectLayers.ZoomToLayer(MyMapControl);
                            }

                            if (localAddLayer == true)
                            {
                                ProjectLayers.AddLayers(MyMapControl);
                                ProjectLayers.ResetRequiresLoading();
                            }
                        }

                        if (localAddText == true)
                        {
                            mainViewModel?.UpdateConsoleText($"{timeToShow}");
                        }
                    });

                    if (isZoomedToLayer == false)
                    {
                        isZoomedToLayer = true;
                    }

                    if (addLayer == true)
                    {
                        addLayer = false;
                    }
                }

                addText = frameTimer.ShouldShowText();
                frameTimer.ResetShowText();
                int timeToSleep = frameTimer.GetTimeToSleep();

                if (timeToSleep > 0)
                {
                    Thread.Sleep(timeToSleep);
                }
            }
        }
    }
}