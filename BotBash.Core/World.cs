namespace BotBash.Core;

/// <summary>The board on which a game will take place.</summary>
public class World
{
    public int Width { get; set; }
    public int Height { get; set; }
    public Dictionary<Coordinate, Cell> Layout { get; set; }

    public World(int width, int height)
    {
        Width = width;
        Height = height;
        Layout = [];
    }

    public bool IsInBounds(Coordinate Pos)
    => Pos.X >= 1 && Pos.X <= Width && Pos.Y >= 1 && Pos.Y <= Height;

    public void InitialiseRandom()
    {
        Layout = (from x in Enumerable.Range(1, Width)
                  from y in Enumerable.Range(1, Height)
                  select new { x, y })
                  .ToDictionary(k => new Coordinate(k.x, k.y), v => new Cell(null!, Randomise()));
    }

    private static IEntity Randomise()
    {
        var RNG = new Random();
        var Weight = 20; //% chance

        if (RNG.Next(1, 101) <= Weight) //1 - 100 RNG
        {
            return Wall.Create();
        }
        return Empty.Create();
    }

    public bool IsWalkable(Coordinate Coord)
    {
        if (!Layout.ContainsKey(Coord)) { return false; }
        var cell = Layout[Coord];
        return cell.Construct is Empty;
    }

    public Dictionary<Coordinate, Cell> GetVisibleInfo(Coordinate BotPos, int ViewRange)
    {
        var Area = GetVisibleArea(BotPos, ViewRange);
        var Info = new Dictionary<Coordinate, Cell>();
        foreach (var tile in Area) { Info[tile] = Layout[tile]; }
        return Info;
    }

    private HashSet<Coordinate> GetVisibleArea(Coordinate BotPos, int ViewRange)
    {
        var VisibleTiles = new HashSet<Coordinate>();

        //Iterate over diamond-shaped area using Manhattan distance
        for (int offsetX = -ViewRange; offsetX <= ViewRange; offsetX++)
        {
            int MaxOffsetY = ViewRange - Math.Abs(offsetX); //Diamond shape means X + Y <= viewrange
            for (int offsetY = -MaxOffsetY; offsetY <= MaxOffsetY; offsetY++)
            {
                //Only check the outer tiles, the function will go over every tile regardless and this removes extra loops over the same tiles
                if (Math.Abs(offsetX) + Math.Abs(offsetY) == ViewRange)
                {
                    var TargetTile = new Coordinate(BotPos.X + offsetX, BotPos.Y + offsetY);
                    CastLine(BotPos, TargetTile, VisibleTiles);
                }
            }
        }
        return VisibleTiles;
    }

    //Cast a line of sight from StartTile to TargetTile, stopping at walls
    //Bresenhem's line algorythm
    private void CastLine(Coordinate StartTile, Coordinate TargetTile, HashSet<Coordinate> VisibleTiles)
    {
        int currentX = StartTile.X;
        int currentY = StartTile.Y;
        int endX = TargetTile.X;
        int endY = TargetTile.Y;

        int Xdistance = Math.Abs(endX - currentX);   //Total horizontal distance to target
        int Ydistance = Math.Abs(endY - currentY);   //Total vertical distance to target
        int stepX = currentX < endX ? 1 : -1;        //Direction along X axis
        int stepY = currentY < endY ? 1 : -1;        //Direction along Y axis
        int errorTerm = Xdistance - Ydistance;       //Tracks when to step horizontal or vertical via rounding error accrued

        while (true)
        {
            var Coord = new Coordinate(currentX, currentY);

            //Stop if tile is outside the game world
            if (!Layout.ContainsKey(Coord)) { break; }

            VisibleTiles.Add(Coord);

            //Stop line if wall
            if (Layout[Coord].Construct is Wall) { break; }

            //Stop if reach end
            if (currentX == endX && currentY == endY) { break; }

            int doubleError = 2 * errorTerm; //Avoid Fractions

            if (doubleError > -Ydistance) //Too much Y, do X step
            {
                errorTerm -= Ydistance;
                currentX += stepX;
            }

            if (doubleError < Xdistance)  //Too much X, do Y step
            {
                errorTerm += Xdistance;
                currentY += stepY;
            }
        }
    }

    public void RenderConsoleWorld()
    {
        int MinX = Layout.Keys.Min(p => p.X);
        int MaxX = Layout.Keys.Max(p => p.X);
        int MinY = Layout.Keys.Min(p => p.Y);
        int MaxY = Layout.Keys.Max(p => p.Y);
        char Symbol;

        Console.SetCursorPosition(0, 0);

        for (int y = MinY; y <= MaxY; y++)
        {
            for (int x = MinX; x <= MaxX; x++)
            {
                var Coord = new Coordinate(x, y);

                if (!Layout.TryGetValue(Coord, out var cell))
                {
                    Console.Write("  ");
                    continue;
                }

                //Tiles take priority
                if (cell.Construct is Wall)
                    Symbol = '#';
                else if (cell.Construct is Spike)
                    Symbol = 'S';
                else if (cell.Construct is Danger)
                    Symbol = 'D';
                else if (cell.Construct is Empty && cell.Player != null)
                    Symbol = 'P'; //Player only on empty tile
                else
                    Symbol = '.';

                Console.Write(Symbol + " ");
            }
            Console.WriteLine();
        }
        Console.WriteLine("====WORLD STATE====");
        Console.Out.Flush();
        Thread.Sleep(333);
    }

    public SerializableWorld ToSerializable()
    {
        var cells = Layout.Select(pair =>
        {
            var Coord = pair.Key;
            var cell = pair.Value;

            return new SerializableCell(
                Coord.X,
                Coord.Y,
                cell.Construct.GetType().Name,   //"Wall", "Spike", "Empty", ...
                cell.Player != null              //true if bot, false if not
            );
        }).ToList();

        return new SerializableWorld(
            Width,
            Height,
            cells
        );
    }
}

public record SerializableWorld
(
    int Width,
    int Height,
    List<SerializableCell> cells
);