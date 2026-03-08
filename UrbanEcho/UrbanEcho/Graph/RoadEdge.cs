using Mapsui;
using UrbanEcho.Helpers;
using UrbanEcho.Reporting;
using UrbanEcho.Sim;

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

    private RecordedStats stats = new RecordedStats();

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
        stats.RecordVehicle(incomingStats);
        IncrementFeatureVolume();
    }

    public void IncrementFeatureVolume()
    {
        string key = Helper.TryGetFeatureKVPToString(Feature, "OBJECTID", "");
        if (!string.IsNullOrEmpty(key))
        {
            if (Sim.RoadFeatures.TryGetValue(key, out IFeature? dictionaryFeature))
            {
                if (dictionaryFeature != null)
                {
                    int vehicleCount = Helper.TryGetFeatureKVPToInt(dictionaryFeature, "VehicleCount", 0) + 1;

                    dictionaryFeature["VehicleCount"] = vehicleCount;

                    if (vehicleCount > Sim.RoadWithMaxVolume)
                    {
                        Sim.RoadWithMaxVolume = vehicleCount;
                    }
                }
            }
        }
    }

    public RecordedStats GetStats()
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