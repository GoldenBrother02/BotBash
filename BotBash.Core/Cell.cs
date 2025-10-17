namespace BotBash.Core;

/// <summary>A single field of which the World is made of, consisting of possible Bot and entity.</summary>
public class Cell
{
    public IBot? Player { get; set; }
    public IEntity Construct { get; set; }

    public Cell(IBot player, IEntity construct)
    {
        Player = player;
        Construct = construct;
    }
}