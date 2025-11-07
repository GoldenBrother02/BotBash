using Microsoft.AspNetCore.SignalR;
using BotBash.Core;

namespace BotBash.Server;

public class GameHub : Hub
{
    private static readonly Dictionary<string, EngineManager> Rooms = new();
    private readonly IHubContext<GameHub> HubContext;

    public GameHub(IHubContext<GameHub> hubContext)
    {
        HubContext = hubContext;
    }

    public async Task JoinRoom(string RoomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, RoomName);
        Console.WriteLine($"Client {Context.ConnectionId} joined room {RoomName}");
    }

    public async Task LeaveRoom(string RoomName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomName);
        await Clients.Caller.SendAsync("LeftRoom", RoomName);
        Console.WriteLine($"Client {Context.ConnectionId} left room {RoomName}");
    }

    public async Task StartGame(string RoomName)
    {
        if (!Rooms.ContainsKey(RoomName)) { Rooms[RoomName] = new EngineManager(HubContext, RoomName); }

        var Manager = Rooms[RoomName];
        if (Manager.CanRestart()) { await Manager.StartMatchAsync(); }
    }

    public async Task StartManualGame(string RoomName)
    {
        if (!Rooms.ContainsKey(RoomName)) { Rooms[RoomName] = new EngineManager(HubContext, RoomName); }

        var Manager = Rooms[RoomName];
        if (Manager.CanRestart()) { await Manager.StartManualGame(); }
    }

    public async Task Tick(string RoomName)
    {
        if (Rooms.TryGetValue(RoomName, out var engine)) { await engine.Tick(); }
    }

    public async Task SendManualBotAction(string RoomName, string Action)
    {
        if (Rooms.TryGetValue(RoomName, out var engine))
        {
            var bot = engine.ManualPlayer;
            if (bot == null) return;

            var act = new BotAction();

            switch (Action)
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