public sealed class RoadEdge
{
    public int From { get; }
    public int To { get; }
    public double Length { get; }
    public RoadMetadata Metadata { get; }

    public RoadEdge(int from, int to, double length, RoadMetadata metadata)
    {
        From = from;
        To = to;
        Length = length;
        Metadata = metadata;
    }

    public double TravelTimeSeconds =>
        Length / Metadata.SpeedLimit;
}