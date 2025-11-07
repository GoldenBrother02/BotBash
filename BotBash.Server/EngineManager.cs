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
    private bool IsRunning { get; set; } = false;
    private bool HasEnded { get; set; } = false;
    public ManualBot? ManualPlayer { get; private set; }

    public EngineManager(IHubContext<GameHub> hubContext, string roomName)
    {
        HubContext = hubContext;
        RoomName = roomName;
    }

    public bool CanRestart() => HasEnded || !IsRunning;

    public async Task StartMatchAsync()
    {
        if (IsRunning) { return; }
        IsRunning = true;
        HasEnded = false;

        Game = new World(10, 10);
        Bots = new List<IBot> { new TestBot(), new TestBot() };
        Motor = new Engine(Game, Bots);

        //Hook into world updates
        Motor.OnWorldUpdated = async (updatedWorld) =>
        {
            var serial = updatedWorld.ToSerializable();
            var json = System.Text.Json.JsonSerializer.Serialize(serial);
            await HubContext.Clients.Group(RoomName).SendAsync("WorldUpdated", serial);
        };

        //Hook into game end
        Motor.OnGameEnded = async (result) =>
        {
            await HubContext.Clients.Group(RoomName).SendAsync("GameEnded", result);
            IsRunning = false;
            HasEnded = true;
        };

        _ = Task.Run(async () => { await Motor.Start(); });
    }

    public async Task StartManualGame()
    {
        if (IsRunning) { return; }
        IsRunning = true;
        HasEnded = false;

        Game = new World(10, 10);
        var manualBot = new ManualBot();
        var testBot = new TestBot();
        Bots = new List<IBot> { manualBot, testBot };
        Motor = new Engine(Game, Bots);

        ManualPlayer = manualBot; //Save reference for Hub


        //Hook into world updates
        Motor.OnWorldUpdated = async (updatedWorld) =>
        {
            var serial = updatedWorld.ToSerializable();
            await HubContext.Clients.Group(RoomName).SendAsync("WorldUpdated", serial);
        };


        //Hook into game end
        Motor.OnGameEnded = async (result) =>
        {
            await HubContext.Clients.Group(RoomName).SendAsync("GameEnded", result);
            IsRunning = false;
            HasEnded = true;
        };

        Motor.InitialiseGame();
        await HubContext.Clients.Group(RoomName).SendAsync("WorldUpdated", Game.ToSerializable());
    }

    public async Task Tick()
    {
        if (!IsRunning) { throw new Exception("No game started"); }
        if (HasEnded) { throw new Exception("Game has ended"); }
        if (Motor != null) { await Motor.GameTick(); }
        else { throw new Exception("Engine not started"); }
    }
}