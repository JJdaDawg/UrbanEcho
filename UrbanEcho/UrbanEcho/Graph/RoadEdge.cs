using DocumentFormat.OpenXml.Drawing;
using Mapsui;
using Mapsui.Projections;
using NetTopologySuite.Geometries;
using UrbanEcho;
using UrbanEcho.Events.UI;
using UrbanEcho.Helpers;
using UrbanEcho.Reporting;
using UrbanEcho.Sim;

public delegate void RoadEdgeStatsUpDateEvent(Stats stats);//called by Vehicle to update how long they spent on roadEdge

/// <summary>
/// A directed edge in the road graph connecting two nodes. Tracks live simulation stats
/// and mirrors state (volume, speed, closed) back to the Mapsui feature layer for rendering.
/// </summary>
public sealed class RoadEdge
{
    public event RoadEdgeStatsUpDateEvent? UpdateIntersectionStats;

    public int From { get; }
    public int To { get; }
    public double Length { get; }
    public RoadMetadata Metadata { get; }

    public IFeature Feature { get; }

    /// <summary>
    /// True if this edge runs in the same direction as the source LineString (start→end).
    /// False means it was built as the reverse edge (end→start) for bidirectional roads.
    /// </summary>
    public bool IsFromStartOfLineString { get; }

    /// <summary>
    /// When true the edge is treated as impassable by the pathfinder.
    /// </summary>
    public bool IsClosed { get; private set; }

    public void Close()
    {
        IsClosed = true;
        stats.SetClosed();
        UpdateFeatureClosedStatus(true);
        if (!Helper.TestMode)
        {
            // skip map refresh in tests - no map instance available
            EventQueueForUI.Instance.Add(new RefreshMapEvent(MainWindow.Instance.GetMap()));
        }
    }

    public void Open()
    {
        IsClosed = false;
        UpdateFeatureClosedStatus(false);
        EventQueueForUI.Instance.Add(new RefreshMapEvent(MainWindow.Instance.GetMap()));
    }

    private RecordedStats stats = new RecordedStats();

    public RoadEdge(int from, int to, double length, RoadMetadata metadata, IFeature feature, bool isFromStartOfLineString)
    {
        From = from;
        To = to;
        Length = length;
        Metadata = metadata;
        Feature = feature;

        IsFromStartOfLineString = isFromStartOfLineString;

        if (feature is Mapsui.Nts.GeometryFeature gf && gf.Geometry is LineString ls)
        {
            (double lon, double lat) = SphericalMercator.ToLonLat(ls.Centroid.X, ls.Centroid.Y);
            stats.SetPosition(lat, lon);
        }
    }

    /// <summary>
    /// Called by a vehicle as it exits this edge. Fires <see cref="UpdateIntersectionStats"/>
    /// for any listening intersection, then records the stats locally.
    /// </summary>
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

    private void UpdateFeatureClosedStatus(bool isClosed)
    {
        string key = Helper.TryGetFeatureKVPToString(Feature, "OBJECTID", "");
        if (!string.IsNullOrEmpty(key))
        {
            if (SimManager.Instance.RoadFeatures.TryGetValue(key, out IFeature? dictionaryFeature))
            {
                if (dictionaryFeature != null)
                {
                    if (isClosed)
                    {
                        dictionaryFeature["Closed"] = 1;
                    }
                    else
                    {
                        dictionaryFeature["Closed"] = 0;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Bumps the VehicleCount on the shared Mapsui feature so the heatmap renderer
    /// picks up the new value. Also updates the simulation-wide max volume tracker.
    /// </summary>
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

    /// <summary>
    /// Writes the latest average speed to the shared Mapsui feature for rendering.
    /// For bidirectional roads, blends both directional speeds into one feature-level value.
    /// </summary>
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
                                // blend both directions into one feature-level speed for rendering
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
                                // blend both directions into one feature-level speed for rendering
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

        if (IsClosed)
        {
            // closed is a road state, not a stat - preserve it across resets
            stats.SetClosed();
        }
    }

    // Length in metres, SpeedLimit in m/s -> result in seconds
    public double TravelTimeSeconds =>
        Length / Metadata.SpeedLimit;
}