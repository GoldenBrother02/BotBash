namespace BotBash.Core;

/// <summary>The Bot implementation, defines minimum Bot functionality.</summary>
public interface IBot
{
    (int, int) Position { get; set; }

    Action RunLogic();
    Action Move();
    Action Bash();
    Action Lunge(); //either a 1 tile move + attack 2nd tile, or a 2 tile move + attack 3rd tile, 
                    // unsure of which would be better but both implementations have a cooldown of 3? turns
    Action Scan(); //increases vision range from 1 to 5, potentially a 1 turn cooldown so you can't sit in a corner and spam vision to find enemy
    Action Wait();
}

public class TestBot : IBot
{
    public (int, int) Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Action RunLogic()
    {
        throw new NotImplementedException();
    }

    public Action Bash()
    {
        throw new NotImplementedException();
    }

    public Action Lunge()
    {
        throw new NotImplementedException();
    }

    public Action Move()
    {
        throw new NotImplementedException();
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