using NetTopologySuite.Geometries;

public sealed class RoadNode
{
    public int Id { get; }
    public double X { get; }
    public double Y { get; }
    public Coordinate Position { get; }

    public RoadNode(int id, double x, double y)
    {
        Id = id;
        X = x;
        Y = y;
        Position = new Coordinate(x, y);
    }
}