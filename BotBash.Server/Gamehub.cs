using Microsoft.AspNetCore.SignalR;
using BotBash.Core;
using System.ComponentModel;

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

    public async Task SendManualBotAction(string roomName, string action)
    {
        if (Rooms.TryGetValue(roomName, out var engine))
        {
            var bot = engine.ManualPlayer;
            if (bot == null) return;

            var act = new BotAction();

            switch (action)
            {
                case "MoveUp": bot.QueuedAction = act.Move(Direction.Up); break;
                case "MoveDown": bot.QueuedAction = act.Move(Direction.Down); break;
                case "MoveLeft": bot.QueuedAction = act.Move(Direction.Left); break;
                case "MoveRight": bot.QueuedAction = act.Move(Direction.Right); break;
                case "LungeUp": bot.QueuedAction = act.Lunge(Direction.Up); break;
                case "LungeDown": bot.QueuedAction = act.Lunge(Direction.Down); break;
                case "LungeLeft": bot.QueuedAction = act.Lunge(Direction.Left); break;
                case "LungeRight": bot.QueuedAction = act.Lunge(Direction.Right); break;
                case "Scan": bot.QueuedAction = act.Scan(); break;
                default: bot.QueuedAction = act.Wait(); break;
            }
        }
    }
}