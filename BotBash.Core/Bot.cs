namespace BotBash.Core;

/// <summary>The Bot implementation, defines minimum Bot functionality.</summary>
public interface IBot
{
    (int x, int y) Position { get; set; }
    int Vision { get; set; }
    int ScanCooldown { get; set; }
    int LungeCooldown { get; set; }

    Action RunLogic(HashSet<(int x, int y)> VisibleArea);
    Action Move((int x, int y) Direction);
    Action Bash((int x, int y) Direction);
    Action Lunge((int x, int y) Direction);
    Action Scan();
    Action Wait();
}

public class TestBot : IBot
{
    public (int x, int y) Position { get; set; }
    public int Vision { get; set; }
    public int ScanCooldown { get; set; }
    public int LungeCooldown { get; set; }

    public Action RunLogic(HashSet<(int x, int y)> VisibleArea)
    {
        return RandomAction();
        //is called by the engine, runs whatever logic the player makes, and returns an action
    }

    public Action Move((int x, int y) Direction)
    {
        return new Action(ActionType.Move, Direction);
    }

    public Action Bash((int x, int y) Direction)
    {
        return new Action(ActionType.Bash, Direction);
    }

    public Action Lunge((int x, int y) Direction)
    {
        return new Action(ActionType.Lunge, Direction);
    }

    public Action Scan()
    {
        return new Action(ActionType.Scan, (0, 0));
    }

    public Action Wait()
    {
        return new Action(ActionType.Wait, (0, 0));
    }

    private Action RandomAction()
    {
        var RNG = new Random();
        var value = RNG.Next(1, 6);

        var directions = new (int x, int y)[]
        {(1, 0), (-1, 0), (0, 1), (0, -1)};

        var randomDirection = directions[RNG.Next(directions.Length)];


        switch (value)
        {
            case 1:
                return Move(randomDirection);
            case 2:
                return Bash(randomDirection);
            case 3:
                return Lunge(randomDirection);
            case 4:
                return Scan();
            case 5:
                return Wait();
            default:
                return Wait();
        }
    }
}