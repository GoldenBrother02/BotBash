using BotBash.Core;
using Microsoft.AspNetCore.SignalR;

namespace BotBash.Server;

public class EngineManager
{
    private readonly IHubContext<GameHub> _hubContext;

    public EngineManager(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task StartMatchAsync()
    {
        var world = new World(10, 10);
        var bots = new List<IBot> { new TestBot(), new TestBot() };

        var engine = new Engine(world, bots);

        //Hook into world updates
        engine.OnWorldUpdated = async (updatedWorld) =>
        {
            Console.WriteLine("[DEBUG] Sending world update to clients...");

            try
            {
                var serial = updatedWorld.ToSerializable();
                var json = System.Text.Json.JsonSerializer.Serialize(serial);
                Console.WriteLine($"[DEBUG] JSON length: {json.Length}");
                await _hubContext.Clients.All.SendAsync("WorldUpdated", serial);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Serializing world: " + ex.Message);
            }
        };
        _ = Task.Run(async () => await engine.Start());
    }
}
