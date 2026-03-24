using System.Collections.Generic;
using UrbanEcho.Events.UI;
using UrbanEcho.FileManagement;
using UrbanEcho.Models;

namespace UrbanEcho.Events.Sim
{
    /// <summary>
    /// Adds a pre-built list of <see cref="SpawnPoint"/> objects to the simulation
    /// and rebuilds the spawner map layer.
    /// </summary>
    public class AutoPlaceSpawnersEvent : IEventForSim
    {
        private readonly List<SpawnPoint> _spawnPoints;

        public AutoPlaceSpawnersEvent(List<SpawnPoint> spawnPoints)
        {
            _spawnPoints = spawnPoints;
        }

        public void Run()
        {
            foreach (var sp in _spawnPoints)
                ProjectLayers.AddSpawnPoint(sp);

            EventQueueForUI.Instance.Add(new LogToConsole(
                MainWindow.Instance.GetMainViewModel(),
                $"[AutoSpawn] Placed {_spawnPoints.Count} preset spawner(s)"));
        }
    }
}
