using Box2dNet.Interop;
using DocumentFormat.OpenXml.Wordprocessing;
using Mapsui;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using UrbanEcho.Events.UI;
using UrbanEcho.Helpers;
using UrbanEcho.Models;
using UrbanEcho.Sim;

namespace UrbanEcho.Graph
{
    /// <summary>
    /// Vehicle path class, contains everything related to the vehicle following a path
    /// </summary>
    public class VehiclePath
    {
        private RoadGraph graph;

        private List<int>? path;
        private List<PathStep>? pathSteps;

        private int pathSegmentIndex = 0;

        private Vehicle parent;

        private RoadEdge currentRoadEdge;

        private int originNodeId;

        public VehiclePath(Vehicle parent, RoadGraph roadGraph, RoadEdge currentRoadEdge)
        {
            this.parent = parent;
            graph = roadGraph;
            this.currentRoadEdge = SetCurrentRoadEdge(currentRoadEdge);
            originNodeId = currentRoadEdge.From;
        }

        /// <summary>
        /// Advances vehicle to next road in the path
        /// </summary>
        public void AdvanceToNextRoad()
        {
            if (graph == null)
                return;

            if (path == null || pathSteps == null)
            {
                parent.RequestResetVehicleToNewPos();

                return;
            }

            if (pathSegmentIndex >= pathSteps.Count)
            {
                // Route complete — respawn at original gate/census node and start a new trip.
                parent.RequestResetVehicleToNewPos();
                return;
            }

            // setNewPath may have nulled the path on failure — re-check before proceeding
            if (path == null || pathSteps == null) return;

            stepThroughPath();
        }

        /// <summary>
        /// Sets a new path for the vehicle to follow
        /// </summary>
        private void setNewPath(int currentNodeId)
        {
            pathSegmentIndex = 0;
            if (graph == null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Could not run advance to next road"));
                path = null; pathSteps = null;
                return;
            }

            var nodes = graph.Nodes.Keys.ToList();
            if (nodes.Count < 2)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Nodes in graph were less than 2"));
                path = null; pathSteps = null;
                return;
            }
            var pathfinder = new AStarPathfinder(graph, SimManager.Instance.NodePenalties, parent.IsTruck);
            int goalNode = SimManager.Instance.RoutingMode switch
            {
                RoutingMode.Random => TrafficVolumeLoader.PickRandomDestination(graph, currentNodeId),
                RoutingMode.CensusOD when SimManager.Instance.CensusSpawn?.IsLoaded == true
                    => SimManager.Instance.CensusSpawn.PickDestinationNode(),
                _ => TrafficVolumeLoader.PickWeightedDestination(graph, currentNodeId)
            };

            var newPathEdges = pathfinder.FindPathEdges(currentNodeId, goalNode);
            if (newPathEdges.Count < 1)
            {
                path = null; pathSteps = null;
                return;
            }
            pathSteps = PathStepBuilder.Build(newPathEdges, graph);
            path = new List<int> { newPathEdges[0].From };
            for (int i = 0; i < newPathEdges.Count; i++)
                path.Add(newPathEdges[i].To);
        }

        /// <summary>
        /// Gets the current RoadGraph
        /// </summary>
        /// <returns>Returns the Road Graph the vehicle is using <see cref="RoadGraph"/> </returns>
        public RoadGraph GetRoadGraph()
        {
            return graph;
        }

        /// <summary>
        /// steps through the path (to next road edge along path)
        /// </summary>
        private void stepThroughPath()
        {
            if (pathSteps == null || pathSegmentIndex >= pathSteps.Count)
            {
                parent.RequestResetVehicleToNewPos();
                return;
            }

            PathStep step = pathSteps[pathSegmentIndex];

            if (step.Edge.Feature is GeometryFeature theRoad && theRoad.Geometry is LineString newLineString)
            {
                currentRoadEdge = SetCurrentRoadEdge(step.Edge, step.Turn);

                parent.StepThroughLineString(true);

                pathSegmentIndex++;
            }
            else
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Failed to advance to next road"));
                parent.RequestResetVehicleToNewPos();
                return;
            }
        }

        /// <summary>
        /// Resets the vehicle to a new position
        /// </summary>
        public void ResetVehicleToNewPos()
        {
            int goalNode;
            int startNode;

            if (graph == null)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Tried to set path when graph was null"));
                return;
            }

            bool useCensus = SimManager.Instance.SpawnMode == SpawnMode.Census
                && SimManager.Instance.CensusSpawn?.IsLoaded == true;

            if (SimManager.Instance.SpawnPoints.Count > 0 && !useCensus)
            {
                // Gates mode: respawn back to this vehicle's original spawn node
                startNode = originNodeId;
            }
            else if (SimManager.Instance.CensusSpawn != null && SimManager.Instance.CensusSpawn.IsLoaded)
            {
                // Return to the node this vehicle originally spawned from.
                startNode = originNodeId;
            }
            else
            {
                startNode = TrafficVolumeLoader.PickWeightedDestination(graph, -1);
            }
            parent.SetUsingShorterRayForTurn(false);

            goalNode = TrafficVolumeLoader.PickWeightedDestination(graph, startNode); //  we don't use goal node here but it is needed to call pick weighted destination which also sets the path for the vehicle

            setNewPath(startNode);
            pathSegmentIndex = 0;

            if (path is not null)
            {
                AdvanceToNextRoad();
            }
            else
            {
                // startNode failed — try one random fallback to avoid getting permanently stuck
                int fallbackNode = TrafficVolumeLoader.PickWeightedDestination(graph, -1);
                setNewPath(fallbackNode);
                pathSegmentIndex = 0;
                if (path is not null)
                    AdvanceToNextRoad();
                else
                    return; // Give up this cycle; vehicle will retry on the next update
            }

            parent.ResetBodyTransform();
        }

        /// <summary>
        /// Sets Initial path of the vehicle
        /// </summary>
        public bool SetInitialPath()
        {
            bool pathWasSet = false;
            if (path == null || pathSteps == null)
            {
                ResetVehicleToNewPos();
            }

            // Only make the vehicle visible and active once it has a valid path.
            // Without a path the vehicle would drive in a straight line from its
            // body position toward the road — through the map, not the network.
            if (path != null && pathSteps != null)
            {
                pathWasSet = true;
            }

            return pathWasSet;
        }

        /// <summary>
        /// Gets current road edge the vehicle is on
        /// </summary>
        public RoadEdge GetCurrentRoadEdge()
        {
            return currentRoadEdge;
        }

        /// <summary>
        /// Sets the current RoadEdge
        /// </summary>
        /// <returns>Returns the current road edge the vehicle has been set to <see cref="RoadEdge"/> </returns>
        private RoadEdge SetCurrentRoadEdge(RoadEdge updatedRoadEdge, TurnDirection turn = TurnDirection.Straight)
        {
            float newSpeedLimit = (float)(updatedRoadEdge.Metadata.SpeedLimit * 3.6);
            if (newSpeedLimit < 30.0f)
            {
                newSpeedLimit = 30.0f;
            }
            parent.UpdateSpeedLimit(newSpeedLimit);

            if (updatedRoadEdge.Feature is GeometryFeature g)
            {
                if (g.Geometry is LineString lineString)
                {
                    if (lineString.Count >= 2)
                    {
                        parent.UpdatePrevIndexLineString(updatedRoadEdge.IsFromStartOfLineString ? 0 : lineString.Count - 1);
                        parent.UpdateIndexLineString(updatedRoadEdge.IsFromStartOfLineString ? 1 : lineString.Count - 2);
                    }
                }
            }

            parent.SetLane(updatedRoadEdge, turn);

            parent.UpdateStatsOnNewRoad();

            return updatedRoadEdge;
        }

        /// <summary>
        /// Returns the line-string features for the edge currently being traversed
        /// plus every remaining edge in the vehicle's path. Used by the path overlay.
        /// </summary>
        public IReadOnlyList<IFeature> GetRemainingPathFeatures()
        {
            var features = new List<IFeature>();

            // Always include the edge the vehicle is currently driving on.
            IFeature? current = currentRoadEdge?.Feature;
            if (current != null)
                features.Add(current);

            if (pathSteps == null) return features;

            for (int i = pathSegmentIndex; i < pathSteps.Count; i++)
            {
                // Avoid duplicating the current edge if pathSegmentIndex hasn't advanced yet.
                if (pathSteps[i].Edge.Feature is IFeature f && f != current)
                    features.Add(f);
            }
            return features;
        }

        /// <summary>
        /// Sets the destination for the vehicle
        /// </summary>
        public void SetDestination(int goalNodeId)
        {
            if (graph == null) return;

            var pathfinder = new AStarPathfinder(graph, SimManager.Instance.NodePenalties, parent.IsTruck);
            var newPathEdges = pathfinder.FindPathEdges(currentRoadEdge.From, goalNodeId);

            if (newPathEdges.Count < 1)
            {
                EventQueueForUI.Instance.Add(new LogToConsole(MainWindow.Instance.GetMainViewModel(), $"Could not find path to destination {goalNodeId}"));
                return;
            }

            pathSteps = PathStepBuilder.Build(newPathEdges, graph);
            path = new List<int> { newPathEdges[0].From };
            for (int i = 0; i < newPathEdges.Count; i++)
            {
                path.Add(newPathEdges[i].To);
            }

            pathSegmentIndex = 0;
            stepThroughPath();
        }

        /// <summary>
        /// If <paramref name="closedEdge"/> appears in the vehicle's remaining route (or is the
        /// edge currently being traversed), discards the stale path and builds a new one from
        /// the current destination node, which A* will automatically route around the closed edge.
        /// </summary>
        public void RerouteAroundEdge(RoadEdge closedEdge)
        {
            if (pathSteps == null) return;

            bool needsReroute = currentRoadEdge.Feature == closedEdge.Feature;

            if (!needsReroute)
            {
                for (int i = pathSegmentIndex; i < pathSteps.Count; i++)
                {
                    if (pathSteps[i].Edge.Feature == closedEdge.Feature)
                    {
                        needsReroute = true;
                        break;
                    }
                }
            }

            if (!needsReroute) return;

            // Preserve the original destination so the vehicle doesn't lose its goal.
            int originalGoal = path != null && path.Count > 1 ? path[path.Count - 1] : -1;
            int fromNode = currentRoadEdge.To;

            if (originalGoal >= 0 && graph != null)
            {
                var pathfinder = new AStarPathfinder(graph, SimManager.Instance.NodePenalties, parent.IsTruck);
                var newPathEdges = pathfinder.FindPathEdges(fromNode, originalGoal);

                if (newPathEdges.Count > 0)
                {
                    pathSteps = PathStepBuilder.Build(newPathEdges, graph);
                    path = new List<int> { newPathEdges[0].From };
                    for (int i = 0; i < newPathEdges.Count; i++)
                        path.Add(newPathEdges[i].To);
                    pathSegmentIndex = 0;
                    return;
                }
            }

            // Original destination is unreachable — pick a new one as a fallback.
            setNewPath(fromNode);
            pathSegmentIndex = 0;
        }
    }
}