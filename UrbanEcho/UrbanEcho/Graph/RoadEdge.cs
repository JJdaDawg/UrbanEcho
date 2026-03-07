using Mapsui;
using UrbanEcho.Reporting;

public delegate void RoadEdgeStatsUpDateEvent(float timeSpent);//called by Vehicle to update how long they spent on roadEdge

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

    internal delegate void MonitorEvent(string str);

    public RoadEdge(int from, int to, double length, RoadMetadata metadata, IFeature feature, bool isFromStartOfLineString)
    {
        From = from;
        To = to;
        Length = length;
        Metadata = metadata;
        Feature = feature;

        IsFromStartOfLineString = isFromStartOfLineString;
    }

    public void VehicleLeaving(float timeSpentOnRoadEdge)
    {
        UpdateIntersectionStats?.Invoke(timeSpentOnRoadEdge);
        UpdateEdgeStats(timeSpentOnRoadEdge);
    }

    public void UpdateEdgeStats(float timeSpentOnRoadEdge)
    {
        stats.RecordVehicleExited(timeSpentOnRoadEdge);
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