using Microsoft.VisualBasic;

namespace BotBash.Core;

/// <summary>The board on which a game will take place.</summary>
public class World
{
    public int Width { get; set; }
    public int Height { get; set; }
    public Dictionary<(int x, int y), Cell> Layout { get; set; }

    public World(int width, int height)
    {
        Width = width;
        Height = height;
        Layout = new Dictionary<(int x, int y), Cell>();
    }

    public void InitialiseRandom()
    {
        Layout = (from x in Enumerable.Range(0, Width)
                  from y in Enumerable.Range(0, Height)
                  select new { x, y })
                  .ToDictionary(k => (k.x, k.y), v => new Cell(null!, Randomise()));
    }

    private static IEntity Randomise()
    {
        var RNG = new Random();
        var Weight = 20;  //% chance

        if (RNG.Next(1, 101) < Weight) //1 - 100 RNG
        {
            return Wall.Create();
        }
        return Empty.Create();
    }
}