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
using UrbanEcho.Models.UI;
using UrbanEcho.Physics;
using UrbanEcho.Reporting;
using UrbanEcho.Styles;

namespace UrbanEcho.Sim
{
    public class Sim
    {
        private List<Vehicle> Vehicles = new List<Vehicle>();
        public SimClock Clock = new SimClock(startHourOfDay: 6, simMinutesPerRealSecond: 1f);
        private readonly Random spawnRng = new Random();
        private float simTime = 0;
        public long SimFrames = 0;
        private int startingNumberOfVehicles = 3000; //Using number of nodes later in code
        private int maxVehicles = 5000;
        public int GroupToUpdate = 0;
        private bool flasher;

        private bool isDisposed = false;

        private bool didFirstRun = false;

        public Sim()
        {
        }

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

        public List<VehicleReadOnly> GetVehicles()
        {
            List<VehicleReadOnly> readOnlyVehicles = new List<VehicleReadOnly>();
            foreach (Vehicle vehicle in Vehicles)
            {
                readOnlyVehicles.Add(new VehicleReadOnly(vehicle));
            }
            return readOnlyVehicles;
        }

        public Vehicle? GetVehicle(VehicleReadOnly vehicleReadOnly)
        {
            foreach (Vehicle v in Vehicles)
            {
                if (vehicleReadOnly.InstanceMatches(v))
                    return v;
            }
            return null;
        }

        public void Step()
        {
            if (!didFirstRun)
            {
                didFirstRun = true;
                ResetStats();
            }
            float stepSize = SimManager.Instance.BaseStepSize * SimManager.Instance.SimSpeed;

            B2Api.b2World_Step(World.WorldId, stepSize, 1);

            simTime += stepSize;

            SimFrames++;

            bool useCensusSpawning = SimManager.Instance.SpawnMode == SpawnMode.Census
                && SimManager.Instance.CensusSpawn?.IsLoaded == true;

            if (SimManager.Instance.SpawnPoints.Count > 0 && !useCensusSpawning)
            {
                // Use spawner-based spawning when spawn points exist
                if (Vehicles.Count == 0)
                {
                    TrySpawnFromSpawners(true);
                    foreach (Vehicle v in Vehicles)
                    {
                        v.ResetStats();
                    }
                }
                else
                {
                    TrySpawnFromSpawners(false);
                }
            }
            else
            {
                // Fallback to census / random spawning
                if (Vehicles.Count == 0)
                {
                    if (SimManager.Instance.RoadGraph != null)
                    {
                        startingNumberOfVehicles = SimManager.Instance.RoadGraph.Nodes.Count / 2;
                    }
                    TrySpawnVehicle(startingNumberOfVehicles, false);
                    foreach (Vehicle v in Vehicles)
                    {
                        v.ResetStats();//Reset stats at start else the loading time is included when many are loaded
                    }
                }
                else
                {
                    TrySpawnVehicle();
                }
            }

            GroupToUpdate = (GroupToUpdate + 1) % Helper.NumberOfVehicleGroups;

            foreach (Vehicle v in Vehicles)
            {
                v.Update();
            }

            foreach (RoadIntersection roadIntersection in SimManager.Instance.RoadIntersections)
            {
                roadIntersection.UpdateTrafficRules();
            }

            //Update property panel
            updatePropertyPanel();
        }

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

        public void CreateReport()
        {
            /*This part is just for showing on console highest vehicle incoming stat*/
            RoadIntersection? highestIncomingVehiclesIntersection = null;
            int highestIncomingVehiclesCount = 0;
            foreach (RoadIntersection roadIntersection in SimManager.Instance.RoadIntersections)
            {
                RecordedStats stats = roadIntersection.GetStats();
                int incoming = stats.VehicleCount;
                if (incoming > highestIncomingVehiclesCount)
                {
                    highestIncomingVehiclesCount = incoming;
                    highestIncomingVehiclesIntersection = roadIntersection;
                }
            }
            if (highestIncomingVehiclesIntersection != null)
            {
                //Just to test
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Intersection {highestIncomingVehiclesIntersection.Name} had the most vehicles entered with {highestIncomingVehiclesCount} vehicles entered"));
            }
            if (SimManager.Instance.RoadGraph != null)
            {
                Map map = MainWindow.Instance.GetMap();

                EventQueueForUI.Instance.Add(new ZoomEvent(map));
                Thread.Sleep(1000);//Give time for map to zoom out so export image looks correct
                foreach (ILayer layer in map.Layers)
                {
                    while (layer.Busy)
                    {
                        Thread.Sleep(100);//Wait until all layers are not busy
                    }
                }
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Generating Report"));
                ReportTask report = new ReportTask(SimManager.Instance.RoadIntersections, SimManager.Instance.RoadGraph);
            }

            EventQueueForUI.Instance.Add(new RefreshMapEvent(MainWindow.Instance.GetMap()));
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
                int spawnNodeId;
                if (SimManager.Instance.CensusSpawn != null && SimManager.Instance.CensusSpawn.IsLoaded)
                {
                    spawnNodeId = SimManager.Instance.CensusSpawn.PickWeightedSpawnNode();
                }
                else if (SimManager.Instance.nodes != null && SimManager.Instance.nodes.Count > 0)
                {
                    spawnNodeId = SimManager.Instance.nodes[spawnRng.Next(SimManager.Instance.nodes.Count)];
                }
                else
                {
                    continue;
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
                pf["VehicleType"] = "Car" + spawnRng.Next(0, VehicleStyles.NumberOFCarColors);
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
                    //if (i == 0)
                    //{
                    //    EventQueueForUI.Instance.Add(new LogToConsole(mainViewModel,
                    //        $"[Spawn {Clock.FormatTimeOfDay(simTime)}] Vehicle #{vehicleId} spawned at node {spawnNodeId} — total: {Vehicles.Count} - trying to spawn {numberToSpawn}"));
                    //}

                    vehicle.Update();//Call once so path is loaded
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
        /// </summary>
        private void TrySpawnFromSpawners(bool initialBurst)
        {
            if (SimManager.Instance.RoadGraph == null || !World.Created)
                return;

            foreach (SpawnPoint sp in SimManager.Instance.SpawnPoints)
            {
                if (Vehicles.Count >= maxVehicles)
                    return;

                int toSpawn;
                if (initialBurst)
                {
                    // On first frame, give each spawner a small initial batch
                    toSpawn = Math.Min(sp.VehiclesPerMinute, 20);
                }
                else
                {
                    // Convert VehiclesPerMinute to a sim-time interval.
                    // SimMinutesPerRealSecond controls the sim clock rate.
                    // At 60 fps, one step ≈ 1/60 real second.
                    float spawnIntervalSec = 60.0f / (sp.VehiclesPerMinute * Clock.SimMinutesPerRealSecond);
                    if (spawnIntervalSec < 0.016f) spawnIntervalSec = 0.016f;

                    if (simTime - sp.LastSpawnTime < spawnIntervalSec)
                        continue;

                    toSpawn = 1;
                }

                for (int i = 0; i < toSpawn; i++)
                {
                    if (Vehicles.Count >= maxVehicles)
                        return;

                    if (SpawnVehicleAtNode(sp.NearestNodeId))
                    {
                        sp.LastSpawnTime = simTime;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a single vehicle at the given graph node and adds it to the simulation.
        /// Returns true if the vehicle was successfully created.
        /// </summary>
        public bool SpawnVehicleAtNode(int nodeId)
        {
            if (SimManager.Instance.RoadGraph == null || !World.Created)
                return false;

            var outgoing = SimManager.Instance.RoadGraph.GetOutgoingEdges(nodeId);
            if (outgoing.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[Sim] SpawnVehicleAtNode failed: node {nodeId} has no outgoing edges");
                return false;
            }

            bool isTruck = Random.Shared.NextDouble() <= 0.1;

            var validEdges = isTruck
                ? outgoing.Where(e => !e.IsClosed && e.Metadata.TruckAllowance).ToList()
                : outgoing.Where(e => !e.IsClosed).ToList();

            // Spawn point has a fixed node; if no truck roads exist here, demote to car
            // so we never silently drop a spawn from a gate.
            if (isTruck && validEdges.Count == 0)
            {
                isTruck = false;
                validEdges = outgoing.Where(e => !e.IsClosed).ToList();
            }

            if (validEdges.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[Sim] SpawnVehicleAtNode failed: node {nodeId} has no valid edges for vehicle type");
                return false;
            }

            var edge = validEdges[spawnRng.Next(validEdges.Count)];

            if (!SimManager.Instance.RoadGraph.Nodes.TryGetValue(edge.From, out RoadNode? fromNode) || fromNode == null)
                return false;

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
            return false;
        }

        public float GetSimTime()
        {
            return simTime;
        }

        public bool IsDisposed()
        {
            return isDisposed;
        }

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