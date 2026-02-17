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
using UrbanEcho.FileManagement;
using UrbanEcho.ViewModels;
using static Mapsui.MapBuilder;
using Layer = Mapsui.Layers.Layer;
using Box2dNet.Interop;

namespace UrbanEcho.Sim
{
    public static class Sim
    {
        public static CancellationTokenSource Cts = new CancellationTokenSource();

        public static Task? SimTask;

        private static Map? MyMap;

        private static MainViewModel? mainViewModel;

        public static List<Vehicle> Vehicles = new List<Vehicle>();
        public static float SimTime = 0;

        public static void SetMainViewModel(MainViewModel setMainViewModel)
        {
            mainViewModel = setMainViewModel;

            MyMap = setMainViewModel.Map.MyMap;
        }

        public static MainViewModel? GetMainViewModel()
        {
            return mainViewModel;
        }

        public static void Run()
        {
            Stopwatch totalRunTime = new Stopwatch();
            totalRunTime.Start();

            if (mainViewModel == null)
            {
                return;
            }
            //TODO: Remove this once we have UI for loading project
            LoadFileEvent loadProjectEvent = new LoadFileEvent(FileTypes.FileType.ProjectFile, "Resources/ProjectFiles/myFile.Json", mainViewModel.Map.MyMap);
            EventQueueForSim.Instance.Add(loadProjectEvent); //will usually happen from UI

            FrameTimer frameTimer = new FrameTimer(false);

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
                readQueue();

                if (frameTimer.ShouldShowText())
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(mainViewModel, frameTimer.TimeToShow()));
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
            //Thread.SpinWait(100000);
            if (World.Created)
            {
                B2Api.b2World_Step(World.WorldId, 1 / 60f, 4);

                Sim.SimTime += 1 / 60f;

                foreach (Vehicle v in Vehicles)
                {
                    v.Update();
                }

                ProjectLayers.SetVehicleLayerDataChanged();
            }
        }

        private static void readQueue()
        {
            while (!EventQueueForSim.Instance.IsEmpty())
            {
                EventQueueForSim.Instance.Read()?.Run();
            }
        }
    }
}