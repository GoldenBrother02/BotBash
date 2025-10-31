using System;
using System.Collections.Generic;

namespace BotBash.Core;

class Program
{
    static async Task Main()
    {
        var world = new World(width: 10, height: 10);

        var bots = new List<IBot>
        {
            new TestBot(),
            new TestBot()
        };

        var engine = new Engine(world, bots);
        await engine.Start();

        Console.WriteLine("Game has ended!");
    }
}
