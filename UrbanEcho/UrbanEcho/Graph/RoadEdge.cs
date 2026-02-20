using Mapsui;

public sealed class RoadEdge
{
    public int From { get; }
    public int To { get; }
    public double Length { get; }
    public RoadMetadata Metadata { get; }

    public IFeature Feature { get; }

    public bool IsFromStartOfLineString { get; }

    public RoadEdge(int from, int to, double length, RoadMetadata metadata, IFeature feature, bool isFromStartOfLineString)
    {
        From = from;
        To = to;
        Length = length;
        Metadata = metadata;
        Feature = feature;

        IsFromStartOfLineString = isFromStartOfLineString;
    }

    public double TravelTimeSeconds =>
        Length / Metadata.SpeedLimit;
}