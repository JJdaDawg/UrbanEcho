using Mapsui;
using UrbanEcho.Reporting;

public delegate void RoadEdgeStatsUpDateEvent(Stats stats);//called by Vehicle to update how long they spent on roadEdge

public sealed class RoadEdge
{
    public event RoadEdgeStatsUpDateEvent? UpdateIntersectionStats;

    public int From { get; }
    public int To { get; }
    public double Length { get; }
    public RoadMetadata Metadata { get; }

    public IFeature Feature { get; }

    public bool IsFromStartOfLineString { get; }

    private RoadEdgeStats stats = new RoadEdgeStats();

    public RoadEdge(int from, int to, double length, RoadMetadata metadata, IFeature feature, bool isFromStartOfLineString)
    {
        From = from;
        To = to;
        Length = length;
        Metadata = metadata;
        Feature = feature;

        IsFromStartOfLineString = isFromStartOfLineString;
    }

    public void VehicleLeaving(Stats incomingStats)
    {
        UpdateIntersectionStats?.Invoke(incomingStats);
        UpdateEdgeStats(incomingStats);
    }

    public void UpdateEdgeStats(Stats incomingStats)
    {
        stats.RecordVehicleExited(incomingStats);
    }

    public RoadEdgeStats GetStats()
    {
        return stats;
    }

    public void ResetStats()
    {
        stats.Reset();
    }

    public double TravelTimeSeconds =>
        Length / Metadata.SpeedLimit;
}