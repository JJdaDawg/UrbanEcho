using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Models;

namespace UrbanEcho.Messages
{
    public class MapFeatureSelectedMessage
    {
        public MapFeatureType Type { get; }
        public object Data { get; }

        public MapFeatureSelectedMessage(MapFeatureType type, object data)
        {
            Type = type;
            Data = data;
        }
    }

    public class MapFeatureDeselectedMessage
    { }

    public class ActiveLayerChangedMessage
    {
        public SelectionLayer ActiveLayer { get; }

        public ActiveLayerChangedMessage(SelectionLayer activeLayer) => ActiveLayer = activeLayer;
    }

    public class TrackVehicleMessage
    {
        public VehicleReadOnly? Vehicle { get; }

        public TrackVehicleMessage(VehicleReadOnly? vehicle) => Vehicle = vehicle;
    }

    public class PickDestinationMessage
    {
        public VehicleReadOnly? Vehicle { get; }

        public PickDestinationMessage(VehicleReadOnly? vehicle) => Vehicle = vehicle;
    }

    public class DestinationPickedMessage
    { }

    public class ShowVehiclePathMessage
    {
        public VehicleReadOnly Vehicle { get; }

        public ShowVehiclePathMessage(VehicleReadOnly vehicle) => Vehicle = vehicle;
    }

    public class HideVehiclePathMessage
    { }

    public class CensusLoadedMessage
    { }

    public class AddSpawnerMessage
    { }

    public class DeleteSpawnerMessage
    {
        public SpawnPoint SpawnPoint { get; }

        public DeleteSpawnerMessage(SpawnPoint spawnPoint) => SpawnPoint = spawnPoint;
    }

    public class MoveSpawnerMessage
    {
        public SpawnPoint SpawnPoint { get; }

        public MoveSpawnerMessage(SpawnPoint spawnPoint) => SpawnPoint = spawnPoint;
    }

    public class CancelMoveSpawnerMessage
    { }

    public class SpawnerMovedMessage
    { }

    /// <summary>
    /// Requests auto-placement of spawner gates along the convex hull of the
    /// loaded road network's boundary.
    /// </summary>
    public class AutoPlaceSpawnersFromExtentMessage
    {
        public int MaxGates { get; }
        public double Tolerance { get; }
        public int VehiclesPerMinute { get; }

        public AutoPlaceSpawnersFromExtentMessage(int maxGates = 6, double tolerance = 400.0, int vehiclesPerMinute = 5)
        {
            MaxGates = maxGates;
            Tolerance = tolerance;
            VehiclesPerMinute = vehiclesPerMinute;
        }
    }

    /// <summary>
    /// Requests auto-placement of spawner gates at the boundaries of all
    /// residential areas (landuse=residential) found in the given OSM file.
    /// </summary>
    public class AutoPlaceSpawnersFromOsmResidentialMessage
    {
        public string OsmPath { get; }
        public int MaxGatesPerArea { get; }
        public int VehiclesPerMinute { get; }

        public AutoPlaceSpawnersFromOsmResidentialMessage(string osmPath, int maxGatesPerArea = 4, int vehiclesPerMinute = 3)
        {
            OsmPath = osmPath;
            MaxGatesPerArea = maxGatesPerArea;
            VehiclesPerMinute = vehiclesPerMinute;
        }
    }
}