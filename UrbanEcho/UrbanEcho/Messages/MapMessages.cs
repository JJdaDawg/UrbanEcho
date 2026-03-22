using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Models;
using UrbanEcho.Sim;

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

    public class MapFeatureDeselectedMessage { }

    public class ActiveLayerChangedMessage
    {
        public SelectionLayer ActiveLayer { get; }
        public ActiveLayerChangedMessage(SelectionLayer activeLayer) => ActiveLayer = activeLayer;
    }

    public class TrackVehicleMessage
    {
        public Vehicle? Vehicle { get; }
        public TrackVehicleMessage(Vehicle? vehicle) => Vehicle = vehicle;
    }

    public class PickDestinationMessage
    {
        public Vehicle? Vehicle { get; }
        public PickDestinationMessage(Vehicle? vehicle) => Vehicle = vehicle;
    }

    public class DestinationPickedMessage { }

    public class ShowVehiclePathMessage
    {
        public Vehicle Vehicle { get; }
        public ShowVehiclePathMessage(Vehicle vehicle) => Vehicle = vehicle;
    }

    public class HideVehiclePathMessage { }

    public class AddSpawnerMessage { }

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

    public class CancelMoveSpawnerMessage { }

    public class SpawnerMovedMessage { }
}
