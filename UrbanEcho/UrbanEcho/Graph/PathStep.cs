using Mapsui.Nts;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Direction a vehicle must turn at the end of a path segment.
/// Computed from the 2D cross product of three consecutive graph nodes.
/// </summary>
public enum TurnDirection : byte
{
    Straight,
    Left,
    Right
}

/// <summary>
/// One step in a vehicle's precomputed route.
/// Stores the current edge, a direct reference to the next edge (if any),
/// and the turn direction the vehicle must execute at the junction.
/// </summary>
public readonly struct PathStep
{
    /// <summary>The road edge to traverse in this step.</summary>
    public readonly RoadEdge Edge;

    /// <summary>The edge that follows this one, or null if this is the last step.</summary>
    public readonly RoadEdge? NextEdge;

    /// <summary>
    /// Turn direction the vehicle needs to execute at the end of this edge.
    /// Used by lane selection so the vehicle is in the correct lane before the turn.
    /// </summary>
    public readonly TurnDirection Turn;

    public PathStep(RoadEdge edge, RoadEdge? nextEdge, TurnDirection turn)
    {
        Edge = edge;
        NextEdge = nextEdge;
        Turn = turn;
    }
}

/// <summary>
/// Builds a list of <see cref="PathStep"/> from A* edge output,
/// precomputing turn directions via the cross product of consecutive node positions.
/// </summary>
public static class PathStepBuilder
{
    /// <summary>
    /// Threshold for the normalized cross product (sin of the turn angle).
    /// sin(20°) ≈ 0.34 — anything below this is treated as going straight.
    /// </summary>
    private const double StraightThreshold = Math.PI / 9.0; // 20°

    /// <summary>
    /// Convert A* edge list into <see cref="PathStep"/>s with turn directions.
    /// </summary>
    public static List<PathStep> Build(IReadOnlyList<RoadEdge> edges, RoadGraph graph)
    {
        var steps = new List<PathStep>(edges.Count);

        for (int i = 0; i < edges.Count; i++)
        {
            RoadEdge? next = (i + 1 < edges.Count) ? edges[i + 1] : null;
            TurnDirection turn = (next != null)
                ? ComputeTurn(edges[i], next, graph)
                : TurnDirection.Straight;

            steps.Add(new PathStep(edges[i], next, turn));
        }

        return steps;
    }

    /// <summary>
    /// Determine the turn direction at the junction between two consecutive edges.
    /// <c>atan2(|cross|, dot)</c> gives the unsigned turn angle; the sign of the
    /// cross product distinguishes left from right.
    /// </summary>
    private static TurnDirection ComputeTurn(RoadEdge current, RoadEdge next, RoadGraph graph)
    {
        if (!graph.Nodes.TryGetValue(current.From, out var a) ||
            !graph.Nodes.TryGetValue(current.To, out var b) ||
            !graph.Nodes.TryGetValue(next.To, out var c))
            return TurnDirection.Straight;

        //Calculate better since some roads (Fischer Hallman on ramp) it was ignoring U Turn
        //This will help calculate turn direction better also

        double aX = a.X;
        double aY = a.Y;

        if (current.Feature is GeometryFeature gf && gf.Geometry is LineString ls1)
        {
            if (ls1.Coordinates.Length > 1)
            {
                if (current.IsFromStartOfLineString)
                {
                    aX = ls1.Coordinates[ls1.Coordinates.Length - 2].CoordinateValue.X;
                    aY = ls1.Coordinates[ls1.Coordinates.Length - 2].CoordinateValue.Y;
                }
                else
                {
                    aX = ls1.Coordinates[1].CoordinateValue.X;
                    aY = ls1.Coordinates[1].CoordinateValue.Y;
                }
            }
        }

        double cX = c.X;
        double cY = c.Y;

        if (next.Feature is GeometryFeature gf2 && gf2.Geometry is LineString ls2)
        {
            if (ls2.Coordinates.Length > 1)
            {
                if (next.IsFromStartOfLineString)
                {
                    cX = ls2.Coordinates[1].CoordinateValue.X;
                    cY = ls2.Coordinates[1].CoordinateValue.Y;
                }
                else
                {
                    cX = ls2.Coordinates[ls2.Coordinates.Length - 2].CoordinateValue.X;
                    cY = ls2.Coordinates[ls2.Coordinates.Length - 2].CoordinateValue.Y;
                }
            }
        }

        double abX = b.X - aX, abY = b.Y - aY;
        double bcX = cX - b.X, bcY = cY - b.Y;

        double cross = abX * bcY - abY * bcX;
        double dot = abX * bcX + abY * bcY;

        if (Math.Atan2(Math.Abs(cross), dot) < StraightThreshold)
            return TurnDirection.Straight;

        return cross > 0 ? TurnDirection.Left : TurnDirection.Right;
    }
}