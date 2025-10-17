namespace BotBash.Core;

/// <summary>The Bot implementation, defines minimum Bot functionality.</summary>
public interface IBot
{
    (int, int) Position { get; set; }

    Action Move();
    Action Bash();
    Action Lunge();
    Action Scan();
    Action Wait();
}

public class TestBot : IBot
{
    public (int, int) Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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
        throw new NotImplementedException();
    }

    public Action Wait()
    {
        throw new NotImplementedException();
    }
}