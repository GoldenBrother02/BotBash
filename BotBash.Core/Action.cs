namespace BotBash.Core;

public enum ActionType
{
    Move,
    Bash,
    Lunge,
    Scan,
    Wait
}

public enum Direction
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>A class defining what a Bot has decided to do for it's current turn.</summary>
public class Action
{
    public ActionType Type { get; set; }
    public Coordinate? Direction { get; set; }

    public Action(ActionType type, Coordinate? direction)
    {
        Type = type;

        if (type == ActionType.Scan || type == ActionType.Wait) //Scan/Wait don't use direction
        {
            Direction = new Coordinate(0, 0); //I could also check if direction still has a value and throw error but that seems unnecessary
            return;
        }

        else if (direction.HasValue)
        {
            var coord = direction.Value;

            if ((Math.Abs(coord.X) == 1 && coord.Y == 0) || (Math.Abs(coord.Y) == 1 && coord.X == 0))
            {
                Direction = direction;
                return;
            }
            throw new ArgumentException("Direction must be one of (1,0), (-1,0), (0,1), (0,-1)");
        }
        throw new ArgumentException("This actiontype needs a direction");
    }
}

/// <summary>A class defining the scope of a Bot's actions.</summary>
public class BotAction()
{
    private Coordinate Translate(Direction direction)
    {
        return direction switch
        {
            Direction.Up => new Coordinate(0, 1),
            Direction.Down => new Coordinate(0, -1),
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
