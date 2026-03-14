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

    public bool IsClosed { get; private set; }

    public void Close() => IsClosed = true;

    public void Open() => IsClosed = false;

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
        UpdateAverageSpeed(incomingStats.AverageSpeed);
    }

    public void IncrementFeatureVolume()
    {
        string key = Helper.TryGetFeatureKVPToString(Feature, "OBJECTID", "");
        if (!string.IsNullOrEmpty(key))
        {
            if (SimManager.Instance.RoadFeatures.TryGetValue(key, out IFeature? dictionaryFeature))
            {
                if (dictionaryFeature != null)
                {
                    int vehicleCount = Helper.TryGetFeatureKVPToInt(dictionaryFeature, "VehicleCount", 0) + 1;

                    dictionaryFeature["VehicleCount"] = vehicleCount;

                    if (vehicleCount > SimManager.Instance.RoadWithMaxVolume)
                    {
                        SimManager.Instance.RoadWithMaxVolume = vehicleCount;
                    }
                }
            }
        }
    }

    public void UpdateAverageSpeed(double incomingSpeed)
    {
        string key = Helper.TryGetFeatureKVPToString(Feature, "OBJECTID", "");
        if (!string.IsNullOrEmpty(key))
        {
            if (SimManager.Instance.RoadFeatures.TryGetValue(key, out IFeature? dictionaryFeature))
            {
                if (dictionaryFeature != null)
                {
                    if (Metadata.OneWay == true)
                    {
                        dictionaryFeature["Speed"] = incomingSpeed;
                    }
                    else
                    {
                        if (Metadata.FromToFlowDirection == true)
                        {
                            //Get the speed the other edge contributed
                            double toFromSpeed = Helper.TryGetFeatureKVPToInt(dictionaryFeature, "ToFromSpeed", 0);
                            if (toFromSpeed > 0)
                            {
                                dictionaryFeature["Speed"] = toFromSpeed / 2.0f + incomingSpeed / 2.0f;
                            }
                            else
                            {
                                dictionaryFeature["Speed"] = incomingSpeed;
                            }
                        }
                        else
                        {
                            double fromToSpeed = Helper.TryGetFeatureKVPToInt(dictionaryFeature, "FromToSpeed", 0);

                            if (fromToSpeed > 0)
                            {
                                dictionaryFeature["Speed"] = fromToSpeed / 2.0f + incomingSpeed / 2.0f;
                            }
                            else
                            {
                                dictionaryFeature["Speed"] = incomingSpeed;
                            }
                        }
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