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
    public (int Xmove, int Ymove)? Direction { get; set; }

    public Action(ActionType type, (int xmove, int ymove)? direction)
    {
        Type = type;

        if (type == ActionType.Scan || type == ActionType.Wait) //scan/wait don't use direction
        {
            Direction = (0, 0); //I could also check if direction still has a value and throw error but that seems unnecessary
            return;
        }
        else if (direction.HasValue)
        {
            var (x, y) = direction.Value;

            if ((Math.Abs(x) == 1 && y == 0) || (Math.Abs(y) == 1 && x == 0))
            {
                Direction = direction;
                return;
            }
            throw new ArgumentException("Direction must be one of (1,0), (-1,0), (0,1), (0,-1)");
        }
        throw new ArgumentException("This actiontype needs a direction");
    }
}
