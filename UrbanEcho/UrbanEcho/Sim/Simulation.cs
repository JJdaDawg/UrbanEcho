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
using UrbanEcho.Events.UI;
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
            LoadFileEvent loadProjectEvent = new LoadFileEvent(SimEnumTypes.FileType.ProjectFile, "Resources/ProjectFiles/myFile.Json");
            loadProjectEvent.Run();

            bool addLayer = ProjectLayers.LayersNeedReAdd();

            EventQueueForUI.Instance.Add(new AddLayersEvent(MyMapControl));
            EventQueueForUI.Instance.Add(new ZoomEvent(MyMapControl));

            FrameTimer frameTimer = new FrameTimer(true);

            while (Cts.IsCancellationRequested == false)
            {
                if (!EventQueueForUI.Instance.IsEmpty())
                {
                    if (!EventQueueForUI.Instance.IsEmpty())
                        Dispatcher.UIThread.Post(() =>
                        {
                            while (!EventQueueForUI.Instance.IsEmpty())
                            {
                                EventQueueForUI.Instance.Read()?.Run();
                            }
                        });
                }

                simulationLoop();

                if (frameTimer.ShouldShowText())
                {
                    EventQueueForUI.Instance.Add(new ShowMessageConsoleWindowEvent(mainViewModel, frameTimer.TimeToShow()));
                    frameTimer.ResetShowText();
                }

                int timeToSleep = frameTimer.GetTimeToSleep();

                if (timeToSleep > 0)
                {
                    Thread.Sleep(timeToSleep);
                }
            }
        }

        private static void simulationLoop()
        {
            //simulate doing stuff
            Thread.SpinWait(1000000);
        }
    }
}