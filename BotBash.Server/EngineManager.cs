using BotBash.Core;
using Microsoft.AspNetCore.SignalR;

namespace BotBash.Server;

public class EngineManager
{
    private readonly IHubContext<GameHub> HubContext;
    private readonly string RoomName;
    private World? Game { get; set; }
    private List<IBot>? Bots { get; set; }
    private Engine? Motor { get; set; }
    public ManualBot? ManualPlayer { get; private set; }

    public EngineManager(IHubContext<GameHub> hubContext, string roomName)
    {
        HubContext = hubContext;
        RoomName = roomName;

    }

    public async Task StartMatchAsync()
    {
        Game = new World(10, 10);
        Bots = new List<IBot> { new TestBot(), new TestBot() };
        Motor = new Engine(Game, Bots);

        //Hook into world updates
        Motor.OnWorldUpdated = async (updatedWorld) =>
        {
            Console.WriteLine("[DEBUG] Sending world update to clients...");

            try
            {
                var serial = updatedWorld.ToSerializable();
                var json = System.Text.Json.JsonSerializer.Serialize(serial);
                Console.WriteLine($"[DEBUG] JSON length: {json.Length}");
                await HubContext.Clients.Group(RoomName).SendAsync("WorldUpdated", serial);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Serializing world: " + ex.Message);
            }
        };
        _ = Task.Run(async () => await Motor.Start());
    }

    public async Task StartManualGame()
    {
        Game = new World(10, 10);
        var manualBot = new ManualBot();
        var testBot = new TestBot();
        Bots = new List<IBot> { manualBot, testBot };
        Motor = new Engine(Game, Bots);

        ManualPlayer = manualBot; //Save reference for Hub

        Motor.OnWorldUpdated = async (updatedWorld) =>
        {
            try
            {
                var serial = updatedWorld.ToSerializable();
                await HubContext.Clients.Group(RoomName).SendAsync("WorldUpdated", serial);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Serializing world: " + ex.Message);
            }
        };

        Motor.InitialiseGame();
        await HubContext.Clients.Group(RoomName).SendAsync("WorldUpdated", Game.ToSerializable());
    }


    public async Task Tick()
    {
        if (Motor != null)
        {
            await Motor.GameTick();
        }
        else { throw new Exception("Game not started"); }
    }
}