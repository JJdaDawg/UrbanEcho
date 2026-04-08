namespace UrbanEcho.Graph
{
    /// <summary>
    /// Functional classification of a road segment.
    /// Values match the CARTO_CLAS field in the kitchener road network shapefile.
    /// </summary>
    public enum RoadType
    {
        Freeway,
        Expressway,
        Arterial,
        Collector,
        LocalStreet,
        Ramp,
        Roundabout,
        AlleywayLane,
        CulDeSac,
        Private,
        Unknown
    }

    public static class RoadTypeExtensions
    {
        /// <summary>
        /// Multiplier applied to edge travel time during A* routing.
        /// Higher values make the pathfinder avoid that road type.
        /// </summary>
        public static double RoutingCostMultiplier(this RoadType type) => type switch
        {
            RoadType.Freeway      => 0.9,
            RoadType.Expressway   => 0.9,
            RoadType.Arterial     => 1.0,
            RoadType.Collector    => 2,
            RoadType.Ramp         => 1.0,
            RoadType.Roundabout   => 1.1,
            RoadType.LocalStreet  => 3.0,
            RoadType.AlleywayLane => 5.0,
            RoadType.CulDeSac     => 5.0,
            RoadType.Private      => 6.0,
            RoadType.Unknown      => 2.0,
            _                     => 2.0
        };

        /// <summary>
        /// Parse value from the shapefile into a <see cref="RoadType"/>.
        /// </summary>
        public static RoadType ParseCartoClass(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return RoadType.Unknown;

            return value.Trim() switch
            {
                "Freeway"                            => RoadType.Freeway,
                "Expressway / Highway"               => RoadType.Expressway,
                "Arterial"                           => RoadType.Arterial,
                "Collector"                          => RoadType.Collector,
                "Local Street"                       => RoadType.LocalStreet,
                "Ramp"                               => RoadType.Ramp,
                "Roundabout"                         => RoadType.Roundabout,
                "Alleyway / Lane"                    => RoadType.AlleywayLane,
                "Cul-de-Sac"                         => RoadType.CulDeSac,
                "Private"                            => RoadType.Private,
                _                                    => RoadType.Unknown
            };
        }
    }
}
