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
}