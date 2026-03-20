using Mapsui;
using System;

namespace UrbanEcho.Models
{
    /// <summary>
    /// Represents a vehicle spawn point placed on the map.
    /// Each spawn point is tied to the nearest road graph node and
    /// creates vehicles at a configurable rate.
    /// </summary>
    public class SpawnPoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>World-coordinate X (Spherical Mercator).</summary>
        public double X { get; set; }

        /// <summary>World-coordinate Y (Spherical Mercator).</summary>
        public double Y { get; set; }

        /// <summary>The nearest road graph node id resolved at placement time.</summary>
        public int NearestNodeId { get; set; }

        /// <summary>How many vehicles this spawner creates per simulated minute.</summary>
        public int VehiclesPerMinute { get; set; } = 5;

        /// <summary>Simulation time of the last spawn from this point.</summary>
        [Newtonsoft.Json.JsonIgnore]
        public float LastSpawnTime { get; set; }

        public MPoint ToMPoint() => new MPoint(X, Y);
    }
}
