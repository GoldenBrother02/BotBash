namespace BotBash.Core;

/// <summary>The Bot implementation, defines minimum Bot functionality.</summary>
public interface IBot
{
    Coordinate Position { get; set; }
    int Vision { get; set; }
    int ScanCooldown { get; set; }
    int LungeCooldown { get; set; }

    Action RunLogic(HashSet<Coordinate> VisibleArea);
    Action Move(Coordinate Direction);
    Action Bash(Coordinate Direction);
    Action Lunge(Coordinate Direction);
    Action Scan();
    Action Wait();
}

public class TestBot : IBot
{
    public Coordinate Position { get; set; }
    public int Vision { get; set; }
    public int ScanCooldown { get; set; }
    public int LungeCooldown { get; set; }

    public Action RunLogic(HashSet<Coordinate> VisibleArea)
    {
        return RandomAction();
        //Is called by the engine, runs whatever logic the player makes, and returns an action
    }

    public Action Move(Coordinate Direction)
    {
        return new Action(ActionType.Move, Direction);
    }

    public Action Bash(Coordinate Direction)
    {
        return new Action(ActionType.Bash, Direction);
    }

    public Action Lunge(Coordinate Direction)
    {
        return new Action(ActionType.Lunge, Direction);
    }

    public Action Scan()
    {
        return new Action(ActionType.Scan, new Coordinate(0, 0));
    }

    public Action Wait()
    {
        return new Action(ActionType.Wait, new Coordinate(0, 0));
    }

    private Action RandomAction()
    {
        var RNG = new Random();
        var value = RNG.Next(1, 6);

        var directions = new Coordinate[]
        {
            new Coordinate(1, 0),
            new Coordinate(-1, 0),
            new Coordinate(0, 1),
            new Coordinate(0, -1)
        };

        var randomDirection = directions[RNG.Next(directions.Length)];


        return value switch
        {
            1 => Move(randomDirection),
            2 => Bash(randomDirection),
            3 => Lunge(randomDirection),
            4 => Scan(),
            5 => Wait(),
            _ => Wait(),
        };
    }
}