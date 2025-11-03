using Microsoft.AspNetCore.SignalR;

namespace BotBash.Server;

public class GameHub : Hub
{
    private static readonly Dictionary<string, EngineManager> Rooms = new();
    private readonly IHubContext<GameHub> HubContext;

    public GameHub(IHubContext<GameHub> hubContext)
    {
        HubContext = hubContext;
    }

    public async Task JoinRoom(string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        Console.WriteLine($"Client {Context.ConnectionId} joined room {roomName}");
    }

    public async Task LeaveRoom(string roomName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        await Clients.Caller.SendAsync("LeftRoom", roomName);
        Console.WriteLine($"Client {Context.ConnectionId} left room {roomName}");
    }

    public async Task StartGame(string roomName)
    {
        if (!Rooms.ContainsKey(roomName))
        {
            Rooms[roomName] = new EngineManager(HubContext, roomName);
        }
        await Rooms[roomName].StartMatchAsync();
    }

    public async Task StartManualGame(string roomName)
    {
        if (!Rooms.ContainsKey(roomName))
        {
            Rooms[roomName] = new EngineManager(HubContext, roomName);
        }
        await Rooms[roomName].StartManualGame();
    }

    public async Task Tick(string roomName)
    {
        if (Rooms.TryGetValue(roomName, out var engine))
        {
            await engine.Tick();
        }
    }
}