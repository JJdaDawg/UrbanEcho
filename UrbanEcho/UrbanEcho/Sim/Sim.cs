using Box2dNet.Interop;
using Mapsui;
using Mapsui.Layers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;
using UrbanEcho.FileManagement;
using UrbanEcho.Helpers;
using UrbanEcho.Models;
using UrbanEcho.Physics;
using UrbanEcho.Reporting;
using UrbanEcho.Styles;

namespace UrbanEcho.Sim
{
    /// <summary>
    /// Class for the simulation
    /// </summary>
    public class Sim
    {
        private List<Vehicle> Vehicles = new List<Vehicle>();

        public SimClock Clock = new SimClock(
            startHourOfDay: 7,
            simMinutesPerRealSecond: 1f / 60f);

        private readonly Random spawnRng = new Random();
        private float simTime = 0;
        public long SimFrames = 0;
        private int maxVehicles = 5000;
        public int GroupToUpdate = 0;
        private bool flasher;

        private bool isDisposed = false;

        private bool didFirstRun = false;

        public Sim()
        {
        }

        /// <summary>
        /// Resets all the stats for the simulation
        /// </summary>
        public void ResetStats()
        {
            //Clear stats for a simulation
            foreach (RoadIntersection roadIntersection in SimManager.Instance.RoadIntersections)
            {
                roadIntersection.ResetStats();
            }

            if (SimManager.Instance.RoadGraph != null)
            {
                foreach (RoadEdge roadEdge in SimManager.Instance.RoadGraph.Edges)
                {
                    roadEdge.ResetStats();
                }
            }

            SimManager.Instance.ResetRoadFeatureStats();

            foreach (Vehicle vehicle in Vehicles)
            {
                vehicle.ResetStats();
            }
        }

        /// <summary>
        /// Returns a list of the readonly Vehicles
        /// </summary>
        /// <returns>Returns a <see cref="VehicleReadOnly"/> List </returns>
        public List<VehicleReadOnly> GetVehicles()
        {
            List<VehicleReadOnly> readOnlyVehicles = new List<VehicleReadOnly>();
            foreach (Vehicle vehicle in Vehicles)
            {
                readOnlyVehicles.Add(new VehicleReadOnly(vehicle));
            }
            return readOnlyVehicles;
        }

        /// <summary>
        /// Returns a Vehicle that matches the instance of the read only vehicle
        /// </summary>
        /// <returns>Returns matching <see cref="Vehicle"/> </returns>
        public Vehicle? GetVehicle(VehicleReadOnly vehicleReadOnly)
        {
            foreach (Vehicle v in Vehicles)
            {
                if (vehicleReadOnly.InstanceMatches(v))
                    return v;
            }
            return null;
        }

        /// <summary>
        /// Does one physics step of the simulation
        /// </summary>
        public void Step()
        {
            if (!didFirstRun)
            {
                didFirstRun = true;
                Clock.StartHourOfDay = SimManager.Instance.ObservationStartHour;
                ResetStats();
            }
            float stepSize = SimManager.Instance.BaseStepSize * SimManager.Instance.SimSpeed;

            B2Api.b2World_Step(World.WorldId, stepSize, 1);

            simTime += stepSize;

            SimFrames++;

            // --- Auto-stop when the observation window has elapsed ---
            float windowSeconds = Clock.GetWindowDurationSeconds(
                SimManager.Instance.ObservationStartHour,
                SimManager.Instance.ObservationEndHour);
            if (simTime >= windowSeconds)
            {
                SimManager.Instance.RunSimulation = false;
                SimManager.Instance.Paused = false;
                EventQueueForUI.Instance.Add(new ResetSimControlEvent());
                EventQueueForUI.Instance.Add(new LogToConsole(
                    MainWindow.Instance.GetMainViewModel(),
                    $"Observation period complete – report generated. Select a new period to run again."));
                return;
            }

            bool useCensusSpawning = SimManager.Instance.SpawnMode == SpawnMode.Census
                && SimManager.Instance.CensusSpawn?.IsLoaded == true;

            int targetCount = GetTargetVehicleCount();
            int activeCount = GetActiveVehicleCount();

            if (SimManager.Instance.SpawnPoints.Count > 0 && !useCensusSpawning)
            {
                // Use spawner-based spawning when spawn points exist
                if (Vehicles.Count == 0)
                {
                    //TrySpawnFromSpawners(true, targetCount);
                    TrySpawnFromSpawners(false, targetCount);

                    foreach (Vehicle v in Vehicles)
                    {
                        v.ResetStats();
                    }
                }
                else if (activeCount < targetCount)
                {
                    WakeDormantVehicles(targetCount - activeCount);
                    TrySpawnFromSpawners(false);
                }
            }
            else
            {
                // Fallback to census / random spawning
                if (Vehicles.Count == 0)
                {
                    TrySpawnVehicle(targetCount, false);
                    foreach (Vehicle v in Vehicles)
                    {
                        v.ResetStats();
                    }
                }
                else if (activeCount < targetCount)
                {
                    WakeDormantVehicles(targetCount - activeCount);
                    TrySpawnVehicle();
                }
            }

            GroupToUpdate = (GroupToUpdate + 1) % Helper.NumberOfVehicleGroups;

            foreach (Vehicle v in Vehicles)
            {
                if (!v.IsDormant)
                    v.Update();
            }

            foreach (RoadIntersection roadIntersection in SimManager.Instance.RoadIntersections)
            {
                roadIntersection.UpdateTrafficRules();
            }

            //Update property panel
            updatePropertyPanel();
        }

        /// <summary>
        /// Refreshes the Vehicle traffic rules (called if intersection type changed)
        /// </summary>
        public void RefreshTrafficRuleReferences()
        {
            foreach (Vehicle v in Vehicles)
            {
                v.RefreshTrafficRuleReferences();
            }
        }

        /// <summary>
        /// Updates the property panel
        /// </summary>
        private void updatePropertyPanel()
        {
            bool flasherLastValue = flasher;
            //Panel updates once a half second of the simulation
            if (SimFrames % 30 > 15)
            {
                flasher = true;
            }
            else
            {
                flasher = false;
            }

            if (flasherLastValue != flasher)
            {
                UpdatePropertyPanelEvent updatePropertyPanelEvent = new UpdatePropertyPanelEvent();
                EventQueueForUI.Instance.Add(updatePropertyPanelEvent);
            }
        }

        /// <summary>
        /// Creates the report
        /// </summary>
        public void CreateReport()
        {
            if (SimManager.Instance.RoadGraph != null)
            {
                Map map = MainWindow.Instance.GetMap();

                EventQueueForUI.Instance.Add(new ZoomEvent(map));

                if (SimManager.Instance.GetSimTime() > 30)
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Generating Report"));
                    ReportTask report = new ReportTask(SimManager.Instance.RoadIntersections, SimManager.Instance.RoadGraph);
                }
                else
                {
                    EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Report was not generated since simulation ran less than 30 seconds"));
                }
            }

            EventQueueForUI.Instance.Add(new RefreshMapEvent(MainWindow.Instance.GetMap()));
        }

        /// <summary>Returns the number of vehicles that are not dormant.</summary>
        public int GetActiveVehicleCount()
        {
            int count = 0;
            foreach (Vehicle v in Vehicles)
            {
                if (!v.IsDormant) count++;
            }
            return count;
        }

        /// <summary>
        /// Returns the ideal number of active vehicles based on the graph size
        /// (capped at 5 000) scaled by the average demand for the observation window.
        /// </summary>
        public int GetTargetVehicleCount()
        {
            int nodeCount = SimManager.Instance.nodes?.Count ?? 0;
            int base_ = Math.Min(nodeCount, maxVehicles);
            float demand = Clock.GetTrafficDemandFraction(
                SimManager.Instance.ObservationStartHour,
                SimManager.Instance.ObservationEndHour);
            return Math.Max(1, (int)(base_ * demand));
        }

        private const int MaxWakePerFrame = 10;

        /// <summary>
        /// Wakes up to <paramref name="count"/> dormant vehicles so they
        /// rejoin traffic with fresh paths.  Capped per frame to avoid
        /// A* pathfinding spikes.
        /// </summary>
        private void WakeDormantVehicles(int count)
        {
            int toWake = Math.Min(count, MaxWakePerFrame);
            int woken = 0;
            foreach (Vehicle v in Vehicles)
            {
                if (woken >= toWake) break;
                if (v.IsDormant)
                {
                    v.WakeUp();
                    woken++;
                }
            }
        }

        /// <summary>
        /// Checks the SimClock and, when the spawn interval has elapsed, creates
        /// one new vehicle using the census-weighted origin node (or a random
        /// node as fallback) and adds it to the simulation.
        /// </summary>
        private void TrySpawnVehicle(int numberRequestingToSpawn = 1, bool waitOnSpawnTimer = true)
        {
            if (waitOnSpawnTimer)
            {
                if (!Clock.ShouldSpawn(simTime))
                    return;
            }
            if (SimManager.Instance.RoadGraph == null || !World.Created)
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
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Loading vehicle paths"));
            }
            int vehiclesAddedThisSpawn = 0;
            for (int i = 0; i < numberToSpawn; i++)
            {
                const int maxRetries = 3;
                bool spawned = false;

                for (int attempt = 0; attempt <= maxRetries && !spawned; attempt++)
                {
                    int spawnNodeId;
                    if (SimManager.Instance.CensusSpawn != null && SimManager.Instance.CensusSpawn.IsLoaded)
                    {
                        spawnNodeId = SimManager.Instance.CensusSpawn.PickWeightedSpawnNode();
                    }
                    else if (SimManager.Instance.AadtHighwaySpawnNodes.Count > 0)
                    {
                        spawnNodeId = SimManager.Instance.AadtHighwaySpawnNodes[
                            spawnRng.Next(SimManager.Instance.AadtHighwaySpawnNodes.Count)];
                    }
                    else if (SimManager.Instance.nodes != null && SimManager.Instance.nodes.Count > 0)
                    {
                        spawnNodeId = SimManager.Instance.nodes[spawnRng.Next(SimManager.Instance.nodes.Count)];
                    }
                    else
                    {
                        break;
                    }

                    var outgoing = SimManager.Instance.RoadGraph.GetOutgoingEdges(spawnNodeId);
                    if (outgoing.Count == 0)
                        continue;

                    double randomValue = Random.Shared.NextDouble();
                    double truckRatio = 0.1f;
                    bool isTruck = randomValue <= truckRatio;

                    var validEdges = isTruck
                        ? outgoing.Where(e => !e.IsClosed && e.Metadata.TruckAllowance).ToList()
                        : outgoing.Where(e => !e.IsClosed).ToList();

                    // Truck landed on a no-truck node: redirect to eligible pool instead of wasting the slot.
                    if (isTruck && validEdges.Count == 0)
                    {
                        var eligible = SimManager.Instance.TruckEligibleNodes;
                        if (eligible.Count > 0)
                        {
                            spawnNodeId = eligible[spawnRng.Next(eligible.Count)];
                            outgoing = SimManager.Instance.RoadGraph.GetOutgoingEdges(spawnNodeId);
                            validEdges = outgoing.Where(e => !e.IsClosed && e.Metadata.TruckAllowance).ToList();
                        }
                    }

                    if (validEdges.Count == 0)
                        continue;

                    var edge = validEdges[spawnRng.Next(validEdges.Count)];

                    if (!SimManager.Instance.RoadGraph.Nodes.TryGetValue(edge.From, out RoadNode? fromNode) || fromNode == null)
                        continue;

                    int vehicleId = Vehicles.Count;
                    MPoint mPoint = new MPoint(fromNode.X, fromNode.Y);
                    PointFeature pf = new PointFeature(mPoint);
                    pf["VehicleNumber"] = vehicleId;
                    pf["Hidden"] = "true";
                    pf["Angle"] = 0.0f;

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

                    Vehicle vehicle = new Vehicle(pf, edge, type, vehicleId % Helper.NumberOfVehicleGroups, SimManager.Instance.RoadGraph);

                    if (vehicle.IsCreated)
                    {
                        vehiclesAddedThisSpawn++;
                        Vehicles.Add(vehicle);
                        lock (SimManager.Instance.LockChangeVehicleFeatureList)
                        {
                            ProjectLayers.VehicleFeatures.Add(pf);
                        }
                        vehicle.Update();
                        spawned = true;
                    }
                }
            }
            if (numberToSpawn > 1)
            {//Show message if more than 1 vehicle being spawned
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Done adding vehicle paths {vehiclesAddedThisSpawn} vehicles added this spawn"));
            }
        }

        /// <summary>
        /// Iterates over all spawn points and spawns vehicles from each one
        /// according to its configured VehiclesPerMinute rate.
        /// When <paramref name="targetCount"/> is supplied the total initial
        /// burst is capped at that value and distributed proportionally across
        /// spawners by their VPM rate so the sim never overshoots its target.
        /// </summary>
        private void TrySpawnFromSpawners(bool initialBurst, int targetCount = int.MaxValue)
        {
            if (SimManager.Instance.RoadGraph == null || !World.Created)
                return;

            // Pre-compute total VPM so we can proportion the target across spawners.
            int totalVpm = 0;
            if (initialBurst)
            {
                foreach (SpawnPoint s in SimManager.Instance.SpawnPoints)
                    totalVpm += s.VehiclesPerMinute;
            }

            foreach (SpawnPoint sp in SimManager.Instance.SpawnPoints)
            {
                if (Vehicles.Count >= maxVehicles || Vehicles.Count >= targetCount)
                    return;

                int toSpawn;
                if (initialBurst)
                {
                    // Give each spawner a share of the target proportional to its VPM.
                    if (totalVpm > 0)
                        toSpawn = Math.Max(1, (int)Math.Round((double)sp.VehiclesPerMinute / totalVpm * targetCount));
                    else
                        toSpawn = 1;

                    // Never exceed remaining room toward the target.
                    toSpawn = Math.Min(toSpawn, targetCount - Vehicles.Count);
                    if (toSpawn <= 0)
                        continue;
                }
                else
                {
                    if (sp.VehiclesPerMinute >= 60)
                    {
                        // High rate: spawn ceil(VPM/60) vehicles at once (one per
                        // nearby node), at a wider interval that keeps the total
                        // rate equal to VPM.  e.g. 120 VPM → 2 nodes, 180 → 3.
                        int nodesNeeded = (int)Math.Ceiling(sp.VehiclesPerMinute / 60.0);
                        float spawnIntervalSec = nodesNeeded / (sp.VehiclesPerMinute * Clock.SimMinutesPerRealSecond);
                        if (spawnIntervalSec < 0.016f) spawnIntervalSec = 0.016f;

                        if (simTime - sp.LastSpawnTime < spawnIntervalSec)
                            continue;

                        toSpawn = nodesNeeded;
                    }
                    else
                    {
                        // Low rate: one vehicle at a time
                        float spawnIntervalSec = 1.0f / (sp.VehiclesPerMinute * Clock.SimMinutesPerRealSecond);
                        if (spawnIntervalSec < 0.016f) spawnIntervalSec = 0.016f;

                        if (simTime - sp.LastSpawnTime < spawnIntervalSec)
                            continue;

                        toSpawn = 1;
                    }
                }

                if (sp.VehiclesPerMinute >= 60)
                {
                    // High rate: spread spawns across nearby nodes
                    var spawnNodes = GetNearbySpawnNodes(sp.NearestNodeId);
                    for (int i = 0; i < toSpawn; i++)
                    {
                        if (Vehicles.Count >= maxVehicles || Vehicles.Count >= targetCount) return;
                        int nodeId = spawnNodes[i % spawnNodes.Count];
                        if (SpawnVehicleAtNode(nodeId))
                            sp.LastSpawnTime = simTime;
                    }
                }
                else
                {
                    // Low rate: single spawn at the gate node
                    for (int i = 0; i < toSpawn; i++)
                    {
                        if (Vehicles.Count >= maxVehicles || Vehicles.Count >= targetCount) return;
                        if (SpawnVehicleAtNode(sp.NearestNodeId))
                            sp.LastSpawnTime = simTime;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list of node IDs near the given gate node, suitable for
        /// distributing high-rate spawns. Includes the gate node itself plus
        /// nodes reachable within two hops on the road graph.
        /// </summary>
        private List<int> GetNearbySpawnNodes(int gateNodeId)
        {
            var graph = SimManager.Instance.RoadGraph!;
            var visited = new HashSet<int> { gateNodeId };

            // 1-hop neighbors
            foreach (var edge in graph.GetOutgoingEdges(gateNodeId))
            {
                if (!edge.IsClosed)
                    visited.Add(edge.To);
            }

            // 2-hop neighbors
            var oneHop = visited.ToList();
            foreach (int n in oneHop)
            {
                foreach (var edge in graph.GetOutgoingEdges(n))
                {
                    if (!edge.IsClosed)
                        visited.Add(edge.To);
                }
            }

            // Keep only nodes that have at least one open outgoing edge
            var result = new List<int>();
            foreach (int n in visited)
            {
                foreach (var edge in graph.GetOutgoingEdges(n))
                {
                    if (!edge.IsClosed)
                    {
                        result.Add(n);
                        break;
                    }
                }
            }

            return result.Count > 0 ? result : new List<int> { gateNodeId };
        }

        /// <summary>
        /// Creates a single vehicle at the given graph node and adds it to the simulation.
        /// If the requested node has no usable edges, up to 3 random fallback
        /// nodes are tried so the spawn slot is not silently lost.
        /// Returns true if the vehicle was successfully created.
        /// </summary>
        public bool SpawnVehicleAtNode(int nodeId)
        {
            if (SimManager.Instance.RoadGraph == null || !World.Created)
                return false;

            const int maxAttempts = 4;
            int attemptNode = nodeId;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (attempt > 0)
                {
                    var fallbackNodes = SimManager.Instance.nodes;
                    if (fallbackNodes == null || fallbackNodes.Count == 0)
                        return false;
                    attemptNode = fallbackNodes[spawnRng.Next(fallbackNodes.Count)];
                }

                var outgoing = SimManager.Instance.RoadGraph.GetOutgoingEdges(attemptNode);
                if (outgoing.Count == 0)
                    continue;

                bool isTruck = Random.Shared.NextDouble() <= 0.1;

                var validEdges = isTruck
                    ? outgoing.Where(e => !e.IsClosed && e.Metadata.TruckAllowance).ToList()
                    : outgoing.Where(e => !e.IsClosed).ToList();

                // Demote to car if no truck roads exist here so we don't lose the slot.
                if (isTruck && validEdges.Count == 0)
                {
                    isTruck = false;
                    validEdges = outgoing.Where(e => !e.IsClosed).ToList();
                }

                if (validEdges.Count == 0)
                    continue;

                var edge = validEdges[spawnRng.Next(validEdges.Count)];

                if (!SimManager.Instance.RoadGraph.Nodes.TryGetValue(edge.From, out RoadNode? fromNode) || fromNode == null)
                    continue;

                int vehicleId = Vehicles.Count;
                MPoint mPoint = new MPoint(fromNode.X, fromNode.Y);
                PointFeature pf = new PointFeature(mPoint);
                pf["VehicleNumber"] = vehicleId;
                pf["Hidden"] = "true";
                pf["Angle"] = 0.0f;
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

                Vehicle vehicle = new Vehicle(pf, edge, type, vehicleId % Helper.NumberOfVehicleGroups, SimManager.Instance.RoadGraph);

                if (vehicle.IsCreated)
                {
                    Vehicles.Add(vehicle);
                    lock (SimManager.Instance.LockChangeVehicleFeatureList)
                    {
                        ProjectLayers.VehicleFeatures.Add(pf);
                    }
                    vehicle.Update();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets simulation time
        /// </summary>
        /// <returns>Returns simulation time as <see cref="float"/> seconds </returns>
        public float GetSimTime()
        {
            return simTime;
        }

        /// <summary>
        /// Gets count of vehicles
        /// </summary>
        /// <returns>Returns a count of the vehicles <see cref="int"/> </returns>
        public int GetVehicleCount()
        {
            return Vehicles.Count;
        }

        /// <summary>
        /// Gets simulation time of day
        /// </summary>
        /// <returns>Returns a string representing simulation time of day <see cref="string"/> </returns>
        public string GetSimTimeOfDay()
        {
            return Clock.FormatTimeOfDay(simTime);
        }

        /// <summary>
        /// Gets if simulation was disposed
        /// </summary>
        /// <returns>Returns a bool true or false if disposed </returns>
        public bool IsDisposed()
        {
            return isDisposed;
        }

        /// <summary>
        /// Disposes the simulation
        /// </summary>
        public void Dispose()
        {
            if (isDisposed == false)
            {
                if (World.Created)
                {
                    foreach (Vehicle vehicle in Vehicles)
                    {
                        if (vehicle.Body != null)
                        {
                            vehicle.Body.Dispose(); //need to dispose to clean up IntPtr
                        }
                    }

                    World.Clear();
                }
                else
                {
                    if (Vehicles.Count > 0)
                    {
                        EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Tried to free up vehicles but world was not created"));
                    }
                }

                Vehicles.Clear();

                lock (SimManager.Instance.LockChangeVehicleFeatureList)//Make sure we dont change the list if being iterated
                {
                    ProjectLayers.VehicleFeatures.Clear();
                }
                isDisposed = true;
            }
        }

        /// <summary>
        /// Marks every edge that belongs to the same physical road as <paramref name="edge"/> as
        /// closed (covers both travel directions), then reroutes every vehicle whose remaining
        /// path includes that road.
        /// </summary>
        public void CloseRoad(RoadEdge edge)
        {
            foreach (Vehicle vehicle in Vehicles)
                vehicle.RerouteAroundEdge(edge);
        }

        /// <summary>
        /// Updates truck allowance on every edge sharing <paramref name="edge"/>'s feature and,
        /// when restricting trucks, reroutes any truck whose remaining path uses that road.
        /// </summary>
        public void SetTruckAllowance(RoadEdge edge)
        {
            foreach (Vehicle vehicle in Vehicles)
            {
                if (vehicle.IsTruck)
                    vehicle.RerouteAroundEdge(edge);
            }
        }

        /// <summary>
        /// Updates the speed limit on every edge sharing <paramref name="edge"/>'s feature and
        /// immediately applies the new limit to any vehicle currently travelling on that road.
        /// </summary>
        public void SetSpeedLimit(RoadEdge edge, float corrected)
        {
            foreach (Vehicle vehicle in Vehicles)
            {
                if (vehicle.GetRoadEdge().Feature == edge.Feature)
                    vehicle.SpeedLimit = corrected;
            }
        }
    }
}