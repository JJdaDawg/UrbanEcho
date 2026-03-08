using UrbanEcho.Graph;

public sealed class RoadMetadata
{
    public string RoadName { get; init; } = "";
    public double SpeedLimit { get; set; }
    public bool TruckAllowance { get; set; } = true;
    public bool OneWay { get; init; }
    public bool FromToFlowDirection { get; init; }
    public double TrafficVolume { get; set; }
    public RoadType RoadType { get; init; } = RoadType.Unknown;
}