namespace BotBash.Core;

/// <summary>The Bot implementation, defines minimum Bot functionality.</summary>
public interface IBot
{
    Coordinate Position { get; set; }
    BotAction? Action { get; set; }
    int Vision { get; set; }
    int ScanCooldown { get; set; }
    int LungeCooldown { get; set; }

    Action RunLogic(HashSet<Coordinate> VisibleArea);
}

public class TestBot : IBot
{
    public Coordinate Position { get; set; }
    public BotAction? Action { get; set; }
    public int Vision { get; set; }
    public int ScanCooldown { get; set; }
    public int LungeCooldown { get; set; }

    public Action RunLogic(HashSet<Coordinate> VisibleArea)
    {
        return RandomAction();
        //Is called by the engine, runs whatever logic the player makes, and returns an action
    }

    private Action RandomAction()
    {
        var RNG = new Random();
        var value = RNG.Next(1, 6);

        var directions = new Direction[]
        {
            Direction.Up,
            Direction.Down,
            Direction.Left,
            Direction.Right
        };

        var randomDirection = directions[RNG.Next(directions.Length)];

        return value switch
        {
            1 => Action!.Move(randomDirection),
            2 => Action!.Bash(randomDirection),
            3 => Action!.Lunge(randomDirection),
            4 => Action!.Scan(),
            5 => Action!.Wait(),
            _ => Action!.Wait(),
        };
    }
}

public class ManualBot : IBot
{
    public Coordinate Position { get; set; }
    public BotAction? Action { get; set; }
    public int Vision { get; set; }
    public int ScanCooldown { get; set; }
    public int LungeCooldown { get; set; }

    public Action RunLogic(HashSet<Coordinate> VisibleArea)  //add an input here so players can manually control a bot
    {
        return Action!.Wait();
    }
}