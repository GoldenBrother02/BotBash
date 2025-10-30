namespace BotBash.Core;

public enum ActionType
{
    Move,
    Bash,
    Lunge,
    Scan,
    Wait
}

/// <summary>A class defining what a Bot has decided to do for it's current turn.</summary>
public class Action
{
    public ActionType Type { get; set; }
    public Coordinate? Direction { get; set; }

    public Action(ActionType type, Coordinate? direction)
    {
        Type = type;

        if (type == ActionType.Scan || type == ActionType.Wait) //scan/wait don't use direction
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
