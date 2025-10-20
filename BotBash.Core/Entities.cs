namespace BotBash.Core;

/// <summary>Defines what entity currently occupies a Cell.</summary>
public interface IEntity
{

}

/// <summary>An empty area, nothing.</summary>
public class Empty : IEntity
{
    public static Empty Create() => new Empty();
}

/// <summary>A wall, blocking Bot visibility and movement</summary>
public class Wall : IEntity
{
    public static Wall Create() => new Wall();
}


/// <summary>A spike, kills a Bot that walks over it, designed to shrink the playable area over time and resolve stalemates.</summary>
public class Spike : IEntity
{
    public static Spike Create() => new Spike();
}

/// <summary>A marker indicating danger, will turn into a Spike during the next turn.</summary>
public class Danger : IEntity
{
    public static Danger Create() => new Danger();
}