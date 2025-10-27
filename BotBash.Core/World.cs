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
        Layout = (from x in Enumerable.Range(1, Width)
                  from y in Enumerable.Range(1, Height)
                  select new { x, y })
                  .ToDictionary(k => (k.x, k.y), v => new Cell(null!, Randomise()));
    }

    public HashSet<(int x, int y)> GetVisibleArea((int x, int y) botPos, int viewRange)
    {
        var visibleTiles = new HashSet<(int x, int y)>();

        //Iterate over diamond-shaped area using Manhattan distance
        for (int offsetX = -viewRange; offsetX <= viewRange; offsetX++)
        {
            int maxOffsetY = viewRange - Math.Abs(offsetX); //Diamond shape means X + Y <= viewrange
            for (int offsetY = -maxOffsetY; offsetY <= maxOffsetY; offsetY++)
            {
                //Only check the outer tiles, the function will go over every tile regardless and this removes extra loops over the same tiles
                if (Math.Abs(offsetX) + Math.Abs(offsetY) == viewRange)
                {
                    var targetTile = (botPos.x + offsetX, botPos.y + offsetY);
                    CastLine(botPos, targetTile, visibleTiles);
                }
            }
        }
        return visibleTiles;
    }

    //Cast a line of sight from startTile to targetTile, stopping at walls
    //Bresenhem's line algorythm
    private void CastLine((int x, int y) startTile, (int x, int y) targetTile, HashSet<(int x, int y)> visibleTiles)
    {
        int currentX = startTile.x;
        int currentY = startTile.y;
        int endX = targetTile.x;
        int endY = targetTile.y;

        int Xdistance = Math.Abs(endX - currentX);   //Total horizontal distance to target
        int Ydistance = Math.Abs(endY - currentY);   //Total vertical distance to target
        int stepX = currentX < endX ? 1 : -1;        //Direction along X axis
        int stepY = currentY < endY ? 1 : -1;        //Direction along Y axis
        int errorTerm = Xdistance - Ydistance;       //Tracks when to step horizontal or vertical via rounding error accrued

        while (true)
        {
            //Stop if tile is outside the game world
            if (!Layout.ContainsKey((currentX, currentY))) { break; }

            visibleTiles.Add((currentX, currentY));

            //Stop line if wall
            if (Layout[(currentX, currentY)].Construct is Wall) { break; }

            //Stop if reach end
            if (currentX == endX && currentY == endY) { break; }

            int doubleError = 2 * errorTerm;  //Avoid Fractions

            if (doubleError > -Ydistance) //too much Y, do X step
            {
                errorTerm -= Ydistance;
                currentX += stepX;
            }

            if (doubleError < Xdistance)  //too much X, do Y step
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

    public bool IsInBounds((int x, int y) pos)
    => pos.x >= 1 && pos.x <= Width && pos.y >= 1 && pos.y <= Height;

    public void RenderWorld()
    {
        //Reset cursor to top-left
        Console.SetCursorPosition(0, 0);

        //Frame counter
        Console.WriteLine("=== WORLD STATE ===");
        Console.WriteLine();

        int minX = Layout.Keys.Min(p => p.x);
        int maxX = Layout.Keys.Max(p => p.x);
        int minY = Layout.Keys.Min(p => p.y);
        int maxY = Layout.Keys.Max(p => p.y);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                var pos = (x, y);
                if (!Layout.ContainsKey(pos))
                {
                    Console.Write("  ");
                    continue;
                }

                var cell = Layout[pos];
                char symbol = cell.Construct switch
                {
                    Wall => '#',
                    Spike => 'S',
                    Danger => 'D',
                    Empty => '.',
                    _ => '?'
                };

                if (cell.Player != null)
                    symbol = 'P';

                Console.Write(symbol + " ");
            }
            Console.WriteLine();
        }

        Console.WriteLine();
        Console.Out.Flush();
        Thread.Sleep(250); //delay between frames
    }
}