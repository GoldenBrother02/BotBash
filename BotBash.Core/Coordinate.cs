using System.Runtime.CompilerServices;

namespace BotBash.Core;

public readonly struct Coordinate
{
    public int X { get; init; }
    public int Y { get; init; }
    public Coordinate(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Coordinate Add(Coordinate coord)
    {
        return new Coordinate(X + coord.X, Y + coord.Y);
    }

    //all of this for "=="
    public static bool operator ==(Coordinate coordL, Coordinate coordR)
    {
        return coordL.X.Equals(coordR.X) && coordL.Y.Equals(coordR.Y);
    }

    public static bool operator !=(Coordinate left, Coordinate right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        return obj is Coordinate coord && this == coord;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}