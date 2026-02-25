using Box2dNet.Interop;
using Mapsui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;
using UrbanEcho.Events.UI;
using UrbanEcho.FileManagement;
using UrbanEcho.Graph;
using UrbanEcho.Helpers;
using UrbanEcho.Physics;
using UrbanEcho.ViewModels;
using static UrbanEcho.FileManagement.FileTypes;

namespace UrbanEcho.Sim
{
    public static class Sim
    {
        public static CancellationTokenSource Cts = new CancellationTokenSource();

        public static Task? SimTask;

        public static Map? MyMap;

        private static MainViewModel? mainViewModel;

        public static List<Vehicle> Vehicles = new List<Vehicle>();

        public static List<RoadIntersection> RoadIntersections = new List<RoadIntersection>();

        public static RoadGraph? RoadGraph;

        public static float SimTime = 0;

        public static long SimFrames = 0;

        public static int GroupToUpdate = 0;

        public static AStarPathfinder? pathfinder;
        public static List<int>? nodes;

        private static bool vehiclePathsLoaded = false;
        private static bool intersectionBodiesCreated = false;

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
            LoadFileEvent loadProjectEvent = new LoadFileEvent(FileType.ProjectFile, "Resources/ProjectFiles/myFile.Json", mainViewModel.Map.MyMap);
            EventQueueForSim.Instance.Add(loadProjectEvent); //will usually happen from UI

            FrameTimer frameTimer = new FrameTimer(false);

            while (Cts.IsCancellationRequested == false)
            {/*Moved to UI update class
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
                }*/
                frameTimer.Update();
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
                if (!(intersectionBodiesCreated))
                {
                    intersectionBodiesCreated = ProjectLayers.CreateRoadIntersections();
                    EventQueueForUI.Instance.Add(new LogToConsole(mainViewModel, "Done adding intersection bodies"));
                }
                B2Api.b2World_Step(World.WorldId, 1 / 60.0f, 1);

                Sim.SimTime += 1 / 60.0f;

                Sim.SimFrames++;

                Sim.GroupToUpdate = (Sim.GroupToUpdate + 1) % Helper.NumberOfVehicleGroups;
                bool aPathNotLoaded = false;
                foreach (Vehicle v in Vehicles)
                {
                    if (!v.PathSet)
                    {
                        aPathNotLoaded = true;
                        InitializeVehiclePath(v);
                    }

                    v.Update();
                }

                if (!aPathNotLoaded)
                {
                    if (!vehiclePathsLoaded)
                    {
                        vehiclePathsLoaded = true;
                        EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), "All vehicle paths loaded"));
                    }
                }
            }

            //Only update vehicle layer if ui queue is empty
            if (EventQueueForUI.Instance.IsEmpty())
            {
                ProjectLayers.UpdateVehicleLayer(true, MyMap);
            }
        }

        private static void readQueue()
        {
            while (!EventQueueForSim.Instance.IsEmpty())
            {
                EventQueueForSim.Instance.Read()?.Run();
            }
        }

        public static void InitializeGraph()
        {
            if (RoadGraph == null || RoadGraph.Nodes.Count < 2)
                return;

            pathfinder = new AStarPathfinder(RoadGraph);
            nodes = RoadGraph.Nodes.Keys.ToList();
        }

        public static void InitializeVehiclePath(Vehicle v)
        {
            int? startNodeId = v.NodeToId;
            if (startNodeId == null || nodes == null || pathfinder == null || RoadGraph == null) return;

            int goalNode;
            do
            {
                goalNode = nodes[Random.Shared.Next(nodes.Count)];
            } while (goalNode == startNodeId.Value && nodes.Count > 1);

            var path = pathfinder.FindPath(startNodeId.Value, goalNode).ToList();

            if (path.Count > 1)
            {
                v.SetPath(RoadGraph, path);
                v.PathSet = true;
            }
            else
            {
                v.ResetVehicleToNewPos();
            }
        }

        public static void Free()
        {
            Sim.Cts.Cancel();
            try
            {
                if (Sim.SimTask != null)
                {
                    Sim.SimTask.Wait();

                    foreach (RoadIntersection r in Sim.RoadIntersections)
                    {
                        if (r.Body != null)
                        {
                            r.Body.Dispose();
                        }
                    }

                    B2Api.b2DestroyWorld(World.WorldId);//Destroy world
                }
            }
            finally
            {
                Sim.Cts.Dispose();
            }
        }
    }
}