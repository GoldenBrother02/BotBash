using BotBash.Core;

class Program
{
    static void Main()
    {
        var world = new World(10, 10);

        var bots = new List<IBot>
        {
            new TestBot(),
            new TestBot(),
            new TestBot()
        };

        var engine = new Engine(world, bots);

        engine.Start();
    }
}
