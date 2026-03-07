using Box2dNet;
using Box2dNet.Interop;
using BruTile.Wms;
using Mapsui;
using Mapsui.Layers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using UrbanEcho.Events.Sim;
using UrbanEcho.Events.UI;
using UrbanEcho.FileManagement;
using UrbanEcho.Graph;
using UrbanEcho.Helpers;
using UrbanEcho.Models;
using UrbanEcho.Physics;
using UrbanEcho.Reporting;
using UrbanEcho.Styles;
using UrbanEcho.ViewModels;
using static UrbanEcho.FileManagement.FileTypes;

namespace UrbanEcho.Sim
{
    public enum SimControlType
    {
        Stop = 0,
        Pause = 1,
        Start = 2,
        SpeedUp = 3,
        SpeedDown = 4
    }

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
        public static long TaskUpdates = 0;

        public static int GroupToUpdate = 0;

        public static AStarPathfinder? pathfinder;
        public static List<int>? nodes;

        private static bool intersectionBodiesCreated = false;

        public static Dictionary<int, double> NodePenalties = new Dictionary<int, double>();

        public static bool Flasher;

        public static SimClock Clock = new SimClock(startHourOfDay: 6, simMinutesPerRealSecond: 1f);

        private static readonly Random spawnRng = new Random();

        public static object LockChangeVehicleFeatureList = new object();

        private static int maxSimSpeed = 4;
        private static int simSpeed = 1;

        private static int startingNumberOfVehicles = 4000;
        private static int maxVehicles = 5000;

        private static float baseStepSize = 1 / 60.0f;

        public static bool RunSimulation = false;
        public static bool Paused = false;

        public static int SimSpeed
        {
            get
            {
                return simSpeed;
            }
            set
            {
                simSpeed = Math.Clamp(value, 1, maxSimSpeed);
            }
        }

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
            bool startedSimulation = false;

            while (Cts.IsCancellationRequested == false)
            {
                TaskUpdates++;
                frameTimer.Update();

                if (World.Created)
                {
                    if (!(intersectionBodiesCreated))
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(mainViewModel, "Started adding intersection bodies"));

                        if (ProjectLayers.CreateRoadIntersections())
                        {
                            SetIntersectionBodiesCreated();
                        }
                        EventQueueForUI.Instance.Add(new LogToConsole(mainViewModel, "Done adding intersection bodies"));
                    }

                    if (RunSimulation)
                    {
                        if (!startedSimulation)
                        {
                            startedSimulation = true;

                            if (Vehicles.Count > 0)
                            {
                                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Loading vehicle paths"));
                                foreach (Vehicle v in Vehicles)
                                {
                                    v.ResetVehicleToNewPos();
                                }
                                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Done adding vehicle paths"));
                            }
                        }
                        simulationLoop();
                    }
                    if (!RunSimulation && !Paused && startedSimulation == true)
                    {
                        ResetStats();//Clear all the stats

                        lock (LockChangeVehicleFeatureList)//Make sure we dont change the list if being iterated
                        {
                            foreach (Vehicle vehicle in Vehicles)
                            {
                                if (vehicle.Body != null)
                                {
                                    vehicle.Body.Dispose(); //need to dispose to clean up IntPtr
                                }
                            }
                            Vehicles.Clear();
                            ProjectLayers.VehicleFeatures.Clear();
                        }
                        startedSimulation = false;
                    }
                }

                //Only update vehicle layer if ui queue is empty and do it every couple updates
                if (EventQueueForUI.Instance.IsEmpty() && TaskUpdates % 2 == 0)
                {
                    ProjectLayers.UpdateVehicleLayer(true, MyMap);
                }

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
            float stepSize = baseStepSize * SimSpeed;

            B2Api.b2World_Step(World.WorldId, stepSize, 1);

            Sim.simTime += stepSize;

            Sim.SimFrames++;

            if (Vehicles.Count == 0)
            {
                TrySpawnVehicle(startingNumberOfVehicles);
            }
            else
            {
                TrySpawnVehicle();
            }

            Sim.GroupToUpdate = (Sim.GroupToUpdate + 1) % Helper.NumberOfVehicleGroups;

            foreach (Vehicle v in Vehicles)
            {
                v.Update();
            }

            foreach (RoadIntersection roadIntersection in RoadIntersections)
            {
                roadIntersection.UpdateTrafficRules();
            }

            //Update property panel
            updatePropertyPanel();
        }

        private static void updatePropertyPanel()
        {
            bool flasherLastValue = Flasher;
            //Panel updates once a half second of the simulation
            if (Sim.SimFrames % 30 > 15)
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

        /// <summary>
        /// Checks the SimClock and, when the spawn interval has elapsed, creates
        /// one new vehicle using the census-weighted origin node (or a random
        /// node as fallback) and adds it to the simulation.
        /// </summary>
        private static void TrySpawnVehicle(int numberRequestingToSpawn = 1)
        {
            if (!Clock.ShouldSpawn(simTime))
                return;

            if (RoadGraph == null || !World.Created)
                return;
            if (Vehicles.Count >= maxVehicles)
            {
                return;
            }
            int spaceLeft = maxVehicles - Vehicles.Count;
            if (spaceLeft <= 0 || numberRequestingToSpawn <= 0)
            {
                return;
            }
            int numberToSpawn = numberRequestingToSpawn;
            if (numberRequestingToSpawn >= spaceLeft)
            {
                numberToSpawn = spaceLeft;
            }

            if (numberToSpawn > 1)
            { //Show message if more than 1 vehicle being spawned
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Loading vehicle paths"));
            }
            int vehiclesAddedThisSpawn = 0;
            for (int i = 0; i < numberToSpawn; i++)
            {
                int spawnNodeId;
                if (CensusSpawn != null && CensusSpawn.IsLoaded)
                {
                    spawnNodeId = CensusSpawn.PickWeightedSpawnNode();
                }
                else if (nodes != null && nodes.Count > 0)
                {
                    spawnNodeId = nodes[spawnRng.Next(nodes.Count)];
                }
                else
                {
                    continue;
                }

                var outgoing = RoadGraph.GetOutgoingEdges(spawnNodeId);
                if (outgoing.Count == 0)
                    continue;

                var edge = outgoing[spawnRng.Next(outgoing.Count)];

                if (!RoadGraph.Nodes.TryGetValue(edge.From, out RoadNode? fromNode) || fromNode == null)
                    continue;

                int vehicleId = Vehicles.Count;
                MPoint mPoint = new MPoint(fromNode.X, fromNode.Y);
                PointFeature pf = new PointFeature(mPoint);
                pf["VehicleNumber"] = vehicleId;
                pf["VehicleType"] = "Car" + spawnRng.Next(0, VehicleStyles.NumberOFCarColors);
                pf["Hidden"] = "true";
                pf["Angle"] = 0.0f;

                double randomValue = Random.Shared.NextDouble();
                double truckRatio = 0.1f;
                bool isTruck = false;
                if (randomValue <= truckRatio)
                {
                    isTruck = true;
                }

                string type = "RegularCar";
                if (!isTruck)
                {
                    pf["VehicleType"] = "Car" + spawnRng.Next(0, VehicleStyles.NumberOFCarColors);
                }
                else
                {
                    pf["VehicleType"] = "Truck" + spawnRng.Next(0, VehicleStyles.NumberOFTruckColors);
                    type = "TransportTruck";
                }

                Vehicle vehicle = new Vehicle(pf, edge, type, vehicleId % Helper.NumberOfVehicleGroups, Sim.RoadGraph);

                if (vehicle.IsCreated)
                {
                    vehiclesAddedThisSpawn++;
                    Vehicles.Add(vehicle);
                    lock (LockChangeVehicleFeatureList)
                    {
                        ProjectLayers.VehicleFeatures.Add(pf);
                    }
                    if (i == 0)
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(mainViewModel,
                            $"[Spawn {Clock.FormatTimeOfDay(simTime)}] Vehicle #{vehicleId} spawned at node {spawnNodeId} — total: {Vehicles.Count} - trying to spawn {numberToSpawn}"));
                    }
                }
            }
            if (numberToSpawn > 1)
            {//Show message if more than 1 vehicle being spawned
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Done adding vehicle paths {vehiclesAddedThisSpawn} vehicles added this spawn"));
            }
        }

        public static void ResetStats()
        {
            /*This part is just for showing on console highest vehicle incoming stat*/
            RoadIntersection? highestIncomingVehiclesIntersection = null;
            int highestIncomingVehiclesCount = 0;
            foreach (RoadIntersection roadIntersection in RoadIntersections)
            {
                IntersectionStats stats = roadIntersection.GetStats();
                int incoming = stats.NumberOfVehiclesEntered;
                if (incoming > highestIncomingVehiclesCount)
                {
                    highestIncomingVehiclesCount = incoming;
                    highestIncomingVehiclesIntersection = roadIntersection;
                }
            }
            if (highestIncomingVehiclesIntersection != null)
            {
                //Just to test
                EventQueueForUI.Instance.Add(new LogToConsole(Sim.GetMainViewModel(), $"Intersection {highestIncomingVehiclesIntersection.Name} had the most vehicles entered with {highestIncomingVehiclesCount} vehicles entered"));
            }

            //Clear stats at end of simulation
            foreach (RoadIntersection roadIntersection in RoadIntersections)
            {
                roadIntersection.ResetStats();
            }

            if (Sim.RoadGraph != null)
            {
                foreach (RoadEdge roadEdge in Sim.RoadGraph.Edges)
                {
                    roadEdge.ResetStats();
                }
            }
        }

        public static void Clear()
        {
            Paused = false;
            RunSimulation = false;
            foreach (Vehicle vehicle in Vehicles)
            {
                if (vehicle.Body != null)
                {
                    vehicle.Body.Dispose(); //need to dispose to clean up IntPtr and remove body from world
                }
            }
            Vehicles = new List<Vehicle>();
            foreach (RoadIntersection roadIntersection in RoadIntersections)
            {
                if (roadIntersection.Body != null)
                {
                    roadIntersection.Body.Dispose();//need to dispose to clean up IntPtr and remove body from world
                }
                roadIntersection.Dispose();//need to dispose to clean up event subscriptions
            }
            RoadIntersections = new List<RoadIntersection>();
            RoadGraph = null;
            CensusSpawn = null;
            simTime = 0;
            SimFrames = 0;
            GroupToUpdate = 0;
            pathfinder = null;
            nodes = null;
            intersectionBodiesCreated = false;
            NodePenalties = new Dictionary<int, double>();
            Clock.Reset();
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