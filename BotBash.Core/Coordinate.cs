using System.Runtime.CompilerServices;

namespace BotBash.Core;

/// <summary>A value object holding a location for the map or a direction for actions</summary>
public readonly record struct Coordinate
{
    public int X { get; init; }
    public int Y { get; init; }
    public Coordinate(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static Coordinate operator +(Coordinate coordL, Coordinate coordR)
    {
        return new Coordinate(coordL.X + coordR.X, coordL.Y + coordR.Y);
    }
}