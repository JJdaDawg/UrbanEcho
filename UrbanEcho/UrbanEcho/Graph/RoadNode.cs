public sealed class RoadNode
{
    public int Id { get; }
    public double X { get; }
    public double Y { get; }

    public RoadNode(int id, double x, double y)
    {
        Id = id;
        X = x;
        Y = y;
    }
}