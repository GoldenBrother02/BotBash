public enum ActionType
{
    Move,
    Attack,
    Lunge,
    Scan,
    Wait
}

/// <summary>A class defining what a Bot has decided to do for it's current turn.</summary>
public class Action
{
    public ActionType Type { get; set; }
    public (int dx, int dy)? Direction { get; set; } //scan/wait don't use direction
}
