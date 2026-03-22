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
    public class Sim
    {
        public List<Vehicle> Vehicles = new List<Vehicle>();
        public SimClock Clock = new SimClock(startHourOfDay: 6, simMinutesPerRealSecond: 1f);
        private readonly Random spawnRng = new Random();
        private float simTime = 0;
        public long SimFrames = 0;
        private int startingNumberOfVehicles = 3000;
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

            if (Vehicles.Count == 0)
            {
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

                var edge = outgoing[spawnRng.Next(outgoing.Count)];

                if (!SimManager.Instance.RoadGraph.Nodes.TryGetValue(edge.From, out RoadNode? fromNode) || fromNode == null)
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
    }
}