namespace BotBash.Core;

/// <summary>The Bot implementation, defines minimum Bot functionality.</summary>
public interface IBot
{
    (int, int) Position { get; set; }

    Action Move();
    Action Attack();
    Action Lunge();
    Action Scan();
    Action Wait();
}