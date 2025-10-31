using Microsoft.AspNetCore.SignalR;
using BotBash.Core;

namespace BotBash.Server;

public class GameHub : Hub
{
    private readonly EngineManager _engineManager;

    public GameHub(EngineManager engineManager)
    {
        _engineManager = engineManager;
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", "Welcome to BotBash!");
    }

    public async Task StartGame()
    {
        await _engineManager.StartMatchAsync();
    }

    public async Task StartManualGame()
    {
        await _engineManager.StartManualGame();
    }
    public async Task Tick()
    {
        await _engineManager.Tick();
    }
}
