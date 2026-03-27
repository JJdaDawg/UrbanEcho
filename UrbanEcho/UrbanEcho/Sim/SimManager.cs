using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Box2dNet;
using Box2dNet.Interop;
using BruTile.Wms;
using FluentAvalonia.UI.Windowing;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

    public enum SpawnMode
    {
        Gates,
        Census
    }

    /// <summary>
    /// Controls how vehicles select their next destination node when building a path.
    /// </summary>
    public enum RoutingMode
    {
        /// <summary>Destinations weighted by AADT traffic volume (default).</summary>
        Aadt,

        /// <summary>Destinations chosen uniformly at random.</summary>
        Random,

        /// <summary>Destinations weighted by census zone employment (attraction model). Requires census data.</summary>
        CensusOD
    }

    public sealed class SimManager
    {
        private static SimManager? instance;

        public CancellationTokenSource Cts = new CancellationTokenSource();

        public Task? SimTask;

        public List<RoadIntersection> RoadIntersections = new List<RoadIntersection>();
        public Dictionary<string, IFeature> RoadFeatures = new Dictionary<string, IFeature>();

        public RoadGraph? RoadGraph;

        public CensusSpawnManager? CensusSpawn;

        public List<SpawnPoint> SpawnPoints = new List<SpawnPoint>();

        public SpawnMode SpawnMode { get; set; } = SpawnMode.Gates;

        public RoutingMode RoutingMode { get; set; } = RoutingMode.Aadt;

        public long TaskUpdates = 0;

        public AStarPathfinder? pathfinder;
        public List<int>? nodes;

        /// <summary>Node IDs that have at least one open, truck-allowed outgoing edge. Used as fallback spawn pool for trucks.</summary>
        public List<int> TruckEligibleNodes { get; private set; } = new List<int>();

        private bool intersectionBodiesCreated = false;

        public Dictionary<int, double> NodePenalties = new Dictionary<int, double>();

        public object LockChangeVehicleFeatureList = new object();

        private int maxSimSpeed = 4;
        private int simSpeed = 1;

        public readonly float BaseStepSize = 1 / 60.0f;

        public bool RunSimulation = false;
        public bool Paused = false;

        private const int startingVolume = 100;
        public int RoadWithMaxVolume = startingVolume; //Set to 100 at start used for displaying traffic volumes

        //And is reset at start of simulation
        public float MinForShowSpeed { get; private set; } = 20.0f;

        public float MaxForShowSpeed { get; private set; } = 90.0f;

        public int SimSpeed
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

        private Sim currentSim = new Sim();

        public static SimManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SimManager();
                }
                return instance;
            }
        }

        private bool simulationReady = false;
        private bool projectNameChanged = false;
        private bool lastSimulationReadyValue = false;

        private SimManager()
        {
        }

        public void Run()
        {
            Stopwatch totalRunTime = new Stopwatch();
            totalRunTime.Start();

            //TODO: Remove this once we have UI for loading project
            LoadFileEvent loadProjectEvent = new LoadFileEvent(FileType.ProjectFile, "Resources/ProjectFiles/myFile.uep", MainWindow.Instance.GetMap());
            //LoadFileEvent loadProjectEvent = new LoadFileEvent(FileType.ProjectFile, "Resources/OsmFiles/osmTest.uep", MainWindow.Instance.GetMap());

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

                if (World.WasCreated && !World.Created && ProjectLayers.VehicleLayerReady())
                {
                    World.Init();
                }

                if (World.Created)
                {
                    if (ProjectLayers.GetIsRoadAndIntersectionLoaded())
                    {
                        if (!(intersectionBodiesCreated) && RoadGraph != null)
                        {
                            EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), "Started adding intersection bodies"));

                            if (ProjectLayers.CreateRoadIntersections())
                            {
                                SetIntersectionBodiesCreated();
                            }
                            EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), "Done adding intersection bodies"));
                        }
                    }

                    simulationReady = true;

                    if (RunSimulation)
                    {
                        if (!startedSimulation)
                        {
                            startedSimulation = true;
                        }
                        if (!currentSim.IsDisposed())
                        {
                            if (World.Created)
                            {
                                currentSim.Step();
                            }
                        }
                        else
                        {
                            currentSim = new Sim();
                        }
                    }
                    if (!RunSimulation && !Paused && startedSimulation == true)
                    {
                        if (!currentSim.IsDisposed())
                        {
                            ResetSim();//Dispose so that all vehicles are cleared
                            currentSim.CreateReport();//Clear all the stats
                        }

                        currentSim = new Sim();//Get a new instance for current sim
                        startedSimulation = false;
                    }
                }
                else
                {
                    simulationReady = false;
                }

                //Only update vehicle layer if ui queue is empty and do it every couple updates
                if (EventQueueForUI.Instance.IsEmpty() && TaskUpdates % 2 == 0 && !currentSim.IsDisposed())
                {
                    ProjectLayers.UpdateVehicleLayer(true, MainWindow.Instance.GetMap());
                }

                readQueue();

                if (frameTimer.ShouldShowText())
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), frameTimer.TimeToShow()));
                    frameTimer.ResetShowText();
                }

                int timeToSleep = frameTimer.GetTimeToSleep();

                if (timeToSleep > 0)
                {
                    Thread.Sleep(timeToSleep);
                }

                if (FooterNeedsUpdate() && TaskUpdates % 30 == 0)
                {
                    UpdateFooter();
                }
            }
        }

        public bool FooterNeedsUpdate()
        {
            bool returnValue = (projectNameChanged || lastSimulationReadyValue != simulationReady || RunSimulation);
            return returnValue;
        }

        public void UpdateFooter()
        {
            lastSimulationReadyValue = simulationReady;
            projectNameChanged = false;
            string readyText = (simulationReady) ? "Ready" : "Not Ready - check that all required layers are added";
            string projectText = Path.GetFileNameWithoutExtension(ProjectLayers.GetProject()?.PathForThisFile ?? "");
            if (string.IsNullOrEmpty(projectText))
            {
                projectText = "Untitled Project";
            }
            else
            {
                projectText = projectText + " Project";
            }
            string simTimeText = GetSimTimeOfDay();
            int vehicleCount = RunSimulation ? GetActiveVehicleCount() : 0;
            EventQueueForUI.Instance.Add(new UpdateFooterEvent(readyText, projectText, simTimeText, vehicleCount));
        }

        public void SetProjectNameChanged()
        {
            projectNameChanged = true;
            simulationReady = false;
            if (FooterNeedsUpdate())
            {
                UpdateFooter();
            }
        }

        public float GetSimTime()
        {
            return currentSim.GetSimTime();
        }

        public int GetVehicleCount()
        {
            return currentSim.GetVehicleCount();
        }

        public int GetActiveVehicleCount()
        {
            return currentSim.GetActiveVehicleCount();
        }

        public int GetTargetVehicleCount()
        {
            return currentSim.GetTargetVehicleCount();
        }

        /// <summary>
        /// Returns true when the active vehicle count exceeds the demand target,
        /// signalling that a vehicle finishing its path should go dormant.
        /// </summary>
        public bool ShouldVehicleGoDormant()
        {
            return currentSim.GetActiveVehicleCount() > currentSim.GetTargetVehicleCount();
        }

        public string GetSimTimeOfDay()
        {
            return RunSimulation ? currentSim.GetSimTimeOfDay() : "--:--";
        }

        public List<VehicleReadOnly> GetVehicles()
        {
            return currentSim.GetVehicles();
        }

        public Vehicle? GetVehicle(VehicleReadOnly vehicleReadOnly)
        {
            return currentSim.GetVehicle(vehicleReadOnly);
        }

        public int GetGroupToUpdate()
        {
            return currentSim.GroupToUpdate;
        }

        /// <summary>
        /// Marks every edge that belongs to the same physical road as <paramref name="edge"/> as
        /// closed (covers both travel directions), then reroutes every vehicle whose remaining
        /// path includes that road.
        /// </summary>
        public void CloseRoad(RoadEdge edge)
        {
            if (RoadGraph is null) return;

            foreach (var e in RoadGraph.Edges)
            {
                if (e.Feature == edge.Feature)
                    e.Close();
            }

            lock (LockChangeVehicleFeatureList)
            {
                currentSim.CloseRoad(edge);
            }
        }

        /// <summary>
        /// Reopens every edge that belongs to the same physical road as <paramref name="edge"/>
        /// so A* can route through it again.
        /// </summary>
        public void OpenRoad(RoadEdge edge)
        {
            if (RoadGraph is null) return;

            foreach (var e in RoadGraph.Edges)
            {
                if (e.Feature == edge.Feature)
                    e.Open();
            }
        }

        /// <summary>
        /// Updates truck allowance on every edge sharing <paramref name="edge"/>'s feature and,
        /// when restricting trucks, reroutes any truck whose remaining path uses that road.
        /// </summary>
        public void SetTruckAllowance(RoadEdge edge, bool allow)
        {
            if (RoadGraph is null) return;

            foreach (var e in RoadGraph.Edges)
            {
                if (e.Feature == edge.Feature)
                    e.Metadata.TruckAllowance = allow;
            }

            if (!allow)
            {
                lock (LockChangeVehicleFeatureList)
                {
                    currentSim.SetTruckAllowance(edge);
                }
            }
        }

        /// <summary>
        /// Updates the speed limit on every edge sharing <paramref name="edge"/>'s feature and
        /// immediately applies the new limit to any vehicle currently travelling on that road.
        /// </summary>
        public void SetSpeedLimit(RoadEdge edge, double speedMs)
        {
            edge.Metadata.SpeedLimit = speedMs;

            float speedKmh = (float)(speedMs * 3.6);
            if (speedKmh < 30.0f) speedKmh = 30.0f;
            float corrected = Helper.DoMapCorrection(speedKmh);

            lock (LockChangeVehicleFeatureList)
            {
                currentSim.SetSpeedLimit(edge, corrected);
            }
        }

        private void readQueue()
        {
            while (!EventQueueForSim.Instance.IsEmpty())
            {
                EventQueueForSim.Instance.Read()?.Run();
            }
        }

        public void SetIntersectionBodiesCreated()
        {
            intersectionBodiesCreated = true;
            NodePenalties = BuildNodePenalties();
        }

        private Dictionary<int, double> BuildNodePenalties()
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

        public void InitializeGraph()
        {
            if (RoadGraph == null || RoadGraph.Nodes.Count < 2)
                return;

            pathfinder = new AStarPathfinder(RoadGraph);
            nodes = RoadGraph.Nodes.Keys.ToList();

            // Build the truck-eligible spawn pool once so we never waste a spawn slot.
            TruckEligibleNodes = nodes
                .Where(nid => RoadGraph.GetOutgoingEdges(nid)
                    .Any(e => !e.IsClosed && e.Metadata.TruckAllowance))
                .ToList();
        }

        /// <summary>
        /// Load census data and create the census-aware spawn manager.
        /// Call after InitializeGraph().
        /// </summary>
        public void InitializeCensusSpawning(string censusShapefilePath)
        {
            if (RoadGraph == null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(),
                    "[Census] Cannot load census data before road graph"));
                return;
            }

            var zones = CensusDataLoader.Load(censusShapefilePath, RoadGraph);
            CensusSpawn = new CensusSpawnManager(zones, RoadGraph);

            EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(),
                $"[Census] Spawn manager ready: {(CensusSpawn.IsLoaded ? "OK" : "FALLBACK MODE")}"));
        }

        public void SetRoadFeatureStats(List<IFeature> layerFeatures)
        {
            RoadFeatures = new Dictionary<string, IFeature>();
            foreach (IFeature feature in layerFeatures)
            {
                string key = Helper.TryGetFeatureKVPToString(feature, "OBJECTID", "");

                if (!string.IsNullOrEmpty(key))
                {
                    IFeature newFeature = feature.Copy();
                    newFeature["VehicleCount"] = 0;
                    newFeature["FromToSpeed"] = 0.0;
                    newFeature["ToFromSpeed"] = 0.0;
                    newFeature["Speed"] = 0.0;
                    SimManager.Instance.RoadFeatures.TryAdd(key, newFeature);
                }
            }
        }

        public void ResetRoadFeatureStats()
        {
            foreach (IFeature feature in RoadFeatures.Values)
            {
                feature["VehicleCount"] = 0;
                feature["FromToSpeed"] = 0.0;
                feature["ToFromSpeed"] = 0.0;
                feature["Speed"] = 0.0;
            }
        }

        public void ResetSim()
        {
            if (!currentSim.IsDisposed())
            {
                currentSim.Dispose();
            }
        }

        public void Clear()
        {
            Paused = false;
            RunSimulation = false;
            EventQueueForUI.Instance.Add(new ResetSimControlEvent());
            if (!currentSim.IsDisposed())
            {
                ResetSim();
                currentSim.CreateReport();
            }

            World.Clear(); //Reset world and Destroy all existing bodies

            foreach (RoadIntersection r in RoadIntersections)
            {
                r.Dispose();//need to dispose to clean up event subscriptions
            }
            RoadIntersections = new List<RoadIntersection>();

            RoadGraph = null;
            CensusSpawn = null;
            SpawnPoints = new List<SpawnPoint>();
            SpawnMode = SpawnMode.Gates;
            RoutingMode = RoutingMode.Aadt;
            pathfinder = null;
            nodes = null;
            TruckEligibleNodes = new List<int>();
            intersectionBodiesCreated = false;
            NodePenalties = new Dictionary<int, double>();
        }

        public void Free()
        {
            Cts.Cancel();

            try
            {
                if (SimTask != null)
                {
                    SimTask.Wait();

                    if (ReportTask.ExportTask != null)
                    {
                        if (!ReportTask.ExportTask.IsCompleted)
                        {
                            ReportTask.ExportTask.Wait();
                        }
                    }

                    ResetSim();

                    if (World.Created)
                    {
                        B2Api.b2DestroyWorld(World.WorldId);//Destroy world
                        World.Created = false;
                    }
                }
            }
            finally
            {
                Cts.Dispose();
            }
        }
    }
}