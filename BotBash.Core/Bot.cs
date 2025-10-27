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
        return Wait();
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
}