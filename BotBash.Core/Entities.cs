namespace BotBash.Core;

/// <summary>Defines what entity currently occupies a Cell.</summary>
public interface IEntity
{
    void OnEnter();
}

/// <summary>An empty area, nothing.</summary>
public class Empty : IEntity
{
    public static Empty Create() => new Empty();

    public void OnEnter()
    {
        throw new NotImplementedException();
    }
}

/// <summary>A wall, blocking Bot visibility and movement</summary>
public class Wall : IEntity
{
    public static Wall Create() => new Wall();

    public void OnEnter() { throw new Exception("You shouldn't be able to move onto walls"); }
}


/// <summary>A spike, kills a Bot that walks over it, designed to shrink the playable area over time and resolve stalemates.</summary>
public class Spike : IEntity
{
    public static Spike Create() => new Spike();

    public void OnEnter()
    {
        throw new NotImplementedException();
    }
}

/// <summary>A marker indicating danger, will turn into a Spike during the next turn.</summary>
public class Danger : IEntity
{
    public static Danger Create() => new Danger();
    public void OnEnter()
    {
        throw new NotImplementedException();
    }
}