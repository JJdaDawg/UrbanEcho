using Box2dNet;
using Box2dNet.Interop;
using BruTile.Wms;
using Mapsui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;
using UrbanEcho.Events.UI;
using UrbanEcho.FileManagement;
using UrbanEcho.Graph;
using UrbanEcho.Helpers;
using UrbanEcho.Models;
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

        public static CensusSpawnManager? CensusSpawn;

        private static float simTime = 0;

        public static long SimFrames = 0;

        public static int GroupToUpdate = 0;

        public static AStarPathfinder? pathfinder;
        public static List<int>? nodes;

        public static bool VehiclePathsLoaded = false;
        private static bool intersectionBodiesCreated = false;

        public static Dictionary<int, double> NodePenalties = new Dictionary<int, double>();

        public static bool Flasher;

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
            LoadFileEvent loadProjectEvent = new LoadFileEvent(FileType.ProjectFile, "Resources/ProjectFiles/myFile.uep", mainViewModel.Map.MyMap);
            EventQueueForSim.Instance.Add(loadProjectEvent); //will usually happen from UI

            //Start with a new project
            //NewProjectEvent newProjectEvent = new NewProjectEvent(mainViewModel.Map.MyMap);
            //EventQueueForSim.Instance.Add(newProjectEvent); //will usually happen from UI

            FrameTimer frameTimer = new FrameTimer(false, 60);

            while (Cts.IsCancellationRequested == false)
            {
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

        public static float GetSimTime()
        {
            return simTime;
        }

        private static void simulationLoop()
        {
            if (World.Created)
            {
                if (!(intersectionBodiesCreated))
                {
                    if (ProjectLayers.CreateRoadIntersections())
                    {
                        SetIntersectionBodiesCreated();
                    }
                    EventQueueForUI.Instance.Add(new LogToConsole(mainViewModel, "Done adding intersection bodies"));
                }

                B2Api.b2World_Step(World.WorldId, 1 / 60.0f, 1);

                Sim.simTime += 1 / 60.0f;

                Sim.SimFrames++;

                Sim.GroupToUpdate = (Sim.GroupToUpdate + 1) % Helper.NumberOfVehicleGroups;
                bool aPathNotLoaded = false;
                if (!VehiclePathsLoaded)
                {
                    foreach (Vehicle v in Vehicles)
                    {
                        if (!v.GraphSet)
                        {
                            aPathNotLoaded = true;
                            InitializeVehicle(v);
                        }
                    }
                }
                else
                {
                    foreach (Vehicle v in Vehicles)
                    {
                        v.Update();
                    }
                }

                if (!aPathNotLoaded)
                {
                    if (!VehiclePathsLoaded)
                    {
                        VehiclePathsLoaded = true;
                        EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), "All vehicle paths loaded"));
                    }
                }
            }

            foreach (RoadIntersection roadIntersection in RoadIntersections)
            {
                roadIntersection.UpdateTrafficRules();
            }

            //Only update vehicle layer if ui queue is empty and do it every few frames
            if (EventQueueForUI.Instance.IsEmpty() && Sim.SimFrames % 2 == 0)
            {
                ProjectLayers.UpdateVehicleLayer(true, MyMap);
            }

            //Update property panel
            updatePropertyPanel();
        }

        private static void updatePropertyPanel()
        {
            bool flasherLastValue = Flasher;
            //Panel updates once a second of the simulation
            if (Sim.SimFrames % 120 > 60)
            {
                Flasher = true;
            }
            else
            {
                Flasher = false;
            }

            if (flasherLastValue != Flasher)
            {
                UpdatePropertyPanelEvent updatePropertyPanelEvent = new UpdatePropertyPanelEvent();
                EventQueueForUI.Instance.Add(updatePropertyPanelEvent);
            }
        }

        private static void readQueue()
        {
            while (!EventQueueForSim.Instance.IsEmpty())
            {
                EventQueueForSim.Instance.Read()?.Run();
            }
        }

        public static void SetIntersectionBodiesCreated()
        {
            intersectionBodiesCreated = true;
            NodePenalties = BuildNodePenalties();
        }

        private static Dictionary<int, double> BuildNodePenalties()
        {
            var penalties = new Dictionary<int, double>();
            foreach (var intersection in RoadIntersections)
            {
                if (intersection.TheSignalType == RoadIntersection.SignalType.TwoWayStop)
                {
                    foreach (var etr in intersection.EdgesInto)
                        if (!etr.TrafficRule.IsNeverBlockingTraffic())
                            penalties[etr.RoadEdge.To] = Math.Max(penalties.GetValueOrDefault(etr.RoadEdge.To), 5.0);
                    continue;
                }

                double delay = intersection.TheSignalType switch
                {
                    RoadIntersection.SignalType.FullSignal => 30.0,
                    RoadIntersection.SignalType.AllWayStop => 8.0,
                    RoadIntersection.SignalType.Flasher => 2.0,
                    RoadIntersection.SignalType.PedestrianSignal => 4.0,
                    RoadIntersection.SignalType.StopLRTSignal => 20.0,
                    _ => 0.0
                };

                if (delay <= 0) continue;

                foreach (var etr in intersection.EdgesInto)
                    penalties[etr.RoadEdge.To] = Math.Max(penalties.GetValueOrDefault(etr.RoadEdge.To), delay);
            }
            return penalties;
        }

        public static void InitializeGraph()
        {
            if (RoadGraph == null || RoadGraph.Nodes.Count < 2)
                return;

            pathfinder = new AStarPathfinder(RoadGraph);
            nodes = RoadGraph.Nodes.Keys.ToList();
        }

        /// <summary>
        /// Load census data and create the census-aware spawn manager.
        /// Call after InitializeGraph().
        /// </summary>
        public static void InitializeCensusSpawning(string censusShapefilePath)
        {
            if (RoadGraph == null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(mainViewModel,
                    "[Census] Cannot load census data before road graph"));
                return;
            }

            var zones = CensusDataLoader.Load(censusShapefilePath, RoadGraph);
            CensusSpawn = new CensusSpawnManager(zones, RoadGraph);

            EventQueueForUI.Instance.Add(new LogToConsole(mainViewModel,
                $"[Census] Spawn manager ready: {(CensusSpawn.IsLoaded ? "OK" : "FALLBACK MODE")}"));
        }

        public static void InitializeVehicle(Vehicle v)
        {
            if (RoadGraph is not null)
            {
                v.SetGraph(RoadGraph);
            }
        }

        public static void Clear()
        {
            Vehicles = new List<Vehicle>();
            RoadIntersections = new List<RoadIntersection>();
            RoadGraph = null;
            CensusSpawn = null;
            simTime = 0;
            SimFrames = 0;
            GroupToUpdate = 0;
            pathfinder = null;
            nodes = null;
            VehiclePathsLoaded = false;
            intersectionBodiesCreated = false;
            NodePenalties = new Dictionary<int, double>();
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

                    foreach (Vehicle v in Sim.Vehicles)
                    {
                        if (v.Body != null)
                        {
                            v.Body.Dispose();
                        }
                    }
                    if (World.Created)
                    {
                        B2Api.b2DestroyWorld(World.WorldId);//Destroy world
                        World.Created = false;
                    }
                }
            }
            finally
            {
                Sim.Cts.Dispose();
            }
        }
    }
}