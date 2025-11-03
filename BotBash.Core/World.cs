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

    public void InitialiseRandom()
    {
        Layout = (from x in Enumerable.Range(1, Width)
                  from y in Enumerable.Range(1, Height)
                  select new { x, y })
                  .ToDictionary(k => new Coordinate(k.x, k.y), v => new Cell(null!, Randomise()));
    }

    public Dictionary<Coordinate, Cell> GetVisibleInfo(Coordinate botPos, int viewRange)
    {
        var Area = GetVisibleArea(botPos, viewRange);
        var Info = new Dictionary<Coordinate, Cell>();
        foreach (var tile in Area) { Info[tile] = Layout[tile]; }
        return Info;
    }

    private HashSet<Coordinate> GetVisibleArea(Coordinate botPos, int viewRange)
    {
        var visibleTiles = new HashSet<Coordinate>();

        //Iterate over diamond-shaped area using Manhattan distance
        for (int offsetX = -viewRange; offsetX <= viewRange; offsetX++)
        {
            int maxOffsetY = viewRange - Math.Abs(offsetX); //Diamond shape means X + Y <= viewrange
            for (int offsetY = -maxOffsetY; offsetY <= maxOffsetY; offsetY++)
            {
                //Only check the outer tiles, the function will go over every tile regardless and this removes extra loops over the same tiles
                if (Math.Abs(offsetX) + Math.Abs(offsetY) == viewRange)
                {
                    var targetTile = new Coordinate(botPos.X + offsetX, botPos.Y + offsetY);
                    CastLine(botPos, targetTile, visibleTiles);
                }
            }
        }
        return visibleTiles;
    }

    //Cast a line of sight from startTile to targetTile, stopping at walls
    //Bresenhem's line algorythm
    private void CastLine(Coordinate startTile, Coordinate targetTile, HashSet<Coordinate> visibleTiles)
    {
        int currentX = startTile.X;
        int currentY = startTile.Y;
        int endX = targetTile.X;
        int endY = targetTile.Y;

        int Xdistance = Math.Abs(endX - currentX);   //Total horizontal distance to target
        int Ydistance = Math.Abs(endY - currentY);   //Total vertical distance to target
        int stepX = currentX < endX ? 1 : -1;        //Direction along X axis
        int stepY = currentY < endY ? 1 : -1;        //Direction along Y axis
        int errorTerm = Xdistance - Ydistance;       //Tracks when to step horizontal or vertical via rounding error accrued

        while (true)
        {
            var coord = new Coordinate(currentX, currentX);

            //Stop if tile is outside the game world
            if (!Layout.ContainsKey(coord)) { break; }

            visibleTiles.Add(coord);

            //Stop line if wall
            if (Layout[coord].Construct is Wall) { break; }

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

    private static IEntity Randomise() //Current rando will create scenarios where players cannot reach eachother
    {
        var RNG = new Random();
        var Weight = 20; //% chance

        if (RNG.Next(1, 101) <= Weight) //1 - 100 RNG
        {
            return Wall.Create();
        }
        return Empty.Create();
    }

    public bool IsInBounds(Coordinate pos)
    => pos.X >= 1 && pos.X <= Width && pos.Y >= 1 && pos.Y <= Height;

    public void RenderConsoleWorld()
    {
        int minX = Layout.Keys.Min(p => p.X);
        int maxX = Layout.Keys.Max(p => p.X);
        int minY = Layout.Keys.Min(p => p.Y);
        int maxY = Layout.Keys.Max(p => p.Y);
        char symbol;

        Console.SetCursorPosition(0, 0);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                var coord = new Coordinate(x, y);

                if (!Layout.TryGetValue(coord, out var cell))
                {
                    Console.Write("  ");
                    continue;
                }

                //Tiles take priority
                if (cell.Construct is Wall)
                    symbol = '#';
                else if (cell.Construct is Spike)
                    symbol = 'S';
                else if (cell.Construct is Danger)
                    symbol = 'D';
                else if (cell.Construct is Empty && cell.Player != null)
                    symbol = 'P'; //Player only on empty tile
                else
                    symbol = '.';

                Console.Write(symbol + " ");
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
            var coord = pair.Key;
            var cell = pair.Value;

            return new SerializableCell(
                coord.X,
                coord.Y,
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
    List<SerializableCell> Cells
);