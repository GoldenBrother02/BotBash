namespace BotBash.Core;

/// <summary>The Bot implementation, defines minimum Bot functionality.</summary>
public interface IBot
{
    Coordinate Position { get; set; }
    BotAction? GameAction { get; set; }
    int Vision { get; set; }
    int ScanCooldown { get; set; }
    int LungeCooldown { get; set; }

    Action RunLogic(Dictionary<Coordinate, Cell> VisibleInfo);
}

public class TestBot : IBot
{
    public Coordinate Position { get; set; }
    public BotAction? GameAction { get; set; }
    public int Vision { get; set; }
    public int ScanCooldown { get; set; }
    public int LungeCooldown { get; set; }

    public Action RunLogic(Dictionary<Coordinate, Cell> VisibleInfo)
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
            1 => GameAction!.Move(randomDirection),
            2 => GameAction!.Bash(randomDirection),
            3 => GameAction!.Lunge(randomDirection),
            4 => GameAction!.Scan(),
            5 => GameAction!.Wait(),
            _ => GameAction!.Wait(),
        };
    }
}

public class ManualBot : IBot
{
    public Coordinate Position { get; set; }
    public BotAction? GameAction { get; set; }
    public int Vision { get; set; }
    public int ScanCooldown { get; set; }
    public int LungeCooldown { get; set; }

    public Action RunLogic(Dictionary<Coordinate, Cell> VisibleInfo)  //add an input here so players can manually control a bot
    {
        return GameAction!.Wait();
    }
}