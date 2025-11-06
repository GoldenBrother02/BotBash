namespace BotBash.Core;

public enum Direction
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>A class defining the scope of a Bot's actions.</summary>
public class BotAction()
{
    private static Coordinate Translate(Direction direction)
    {
        return direction switch
        {
            Direction.Up => new Coordinate(0, -1),
            Direction.Down => new Coordinate(0, 1),
            Direction.Left => new Coordinate(-1, 0),
            Direction.Right => new Coordinate(1, 0),
            _ => new Coordinate(0, 0)
        };
    }
    public Action Move(Direction direction)
    {
        return new Action(ActionType.Move, Translate(direction));
    }

    public Action Bash(Direction direction)
    {
        return new Action(ActionType.Bash, Translate(direction));
    }

    public Action Lunge(Direction direction)
    {
        return new Action(ActionType.Lunge, Translate(direction));
    }

    public Action Scan()
    {
        return new Action(ActionType.Scan, new Coordinate(0, 0));
    }

    public Action Wait()
    {
        return new Action(ActionType.Wait, new Coordinate(0, 0));
    }
}