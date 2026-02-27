public sealed class RoadMetadata
{
    public string RoadType { get; init; } = "";
    public double SpeedLimit { get; init; }
    public bool OneWay { get; init; }
    public bool FromToFlowDirection { get; init; }
    public double TrafficVolume { get; set; }
}