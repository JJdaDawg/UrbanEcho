using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace UrbanEcho.Graph
{
    /// <summary>
    /// Represents a single census dissemination area with commuting statistics.
    /// Geometry is stored in EPSG:3857 to match the road graph.
    /// </summary>
    public sealed class CensusZone
    {
        public string GeoCode { get; init; } = "";
        public string GeoName { get; init; } = "";
        public Geometry Boundary { get; init; } = Polygon.Empty;

        public int Population { get; init; }
        public int TotalEmployed { get; init; }
        public int CarTruckVanDrivers { get; init; }

        // Commute destination breakdown
        public int CommuteWithinCSD { get; init; }

        public int CommuteDiffCSDSameCD { get; init; }

        // Commute duration
        public int CommuteLessThan15Min { get; init; }

        public int Commute15To29Min { get; init; }
        public int Commute30To44Min { get; init; }

        /// <summary>
        /// Road graph node IDs that fall within this zone's polygon.
        /// These are the spawn gates for this zone.
        /// </summary>
        public List<int> GateNodeIds { get; } = new();

        /// <summary>
        /// The number of vehicle trips this zone generates (drivers only).
        /// </summary>
        public int VehicleTripsGenerated => CarTruckVanDrivers;

        public double RatioOfArea;
    }
}