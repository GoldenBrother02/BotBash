using System.Security.Cryptography.X509Certificates;

namespace BotBash.Core;

public enum GameState
{
    Playing,
    Draw,
    Victory,
}

/// <summary>Calculates the logic of the game and keeps it running.</summary>
public class Engine
{
    private World GameWorld { get; set; }
    private List<IBot> StartingPlayers { get; set; } //could be used for a statistic?
    private List<IBot> AlivePlayers = [];
    private Dictionary<IBot, Action> BotActions = [];
    private GameState State;
    private int HazardCountdown = 5;

    public Engine(World gameworld, List<IBot> players)
    {
        GameWorld = gameworld;
        StartingPlayers = players;
        AlivePlayers = players;
    }

    public void Start()
    {
        InitialiseGame();

        while (State is GameState.Playing)
        {
            BotDecisions();
            BotMovement();
            BotBashes();
            WorldEdits();
            VictoryCheck();
            BotActions.Clear();
        }

        if (State is GameState.Victory) { }
        if (State is GameState.Draw) { }
    }

    private void InitialiseGame()
    {
        GameWorld.InitialiseRandom();
        State = GameState.Playing;

        var EmptyCells = GameWorld.Layout.Where(cell => cell.Value.Construct is Empty && cell.Value.Player == null);
        if (EmptyCells.Count() < AlivePlayers.Count) { throw new Exception("Not enough empty cells to place all players!"); }

        var RandomCells = EmptyCells.OrderBy(_ => Guid.NewGuid()).Take(AlivePlayers.Count).ToList();

        for (int i = 0; i < AlivePlayers.Count; i++)
        {
            GameWorld.Layout[RandomCells[i].Key].Player = AlivePlayers[i];
            AlivePlayers[i].Position = RandomCells[i].Key;
        }
    }

    private void BotDecisions()  //saving bot decisions to run in order later.
    {
        foreach (var bot in AlivePlayers)
        {
            BotActions.Add(bot, bot.RunLogic());
        }
    }

    private void BotMovement()
    {
        var NewPositions = new Dictionary<IBot, (int x, int y)>();
        var ToKill = new List<IBot>();

        foreach (var bot in AlivePlayers)
        {
            var Decision = BotActions[bot];
            if (Decision.Type is ActionType.Move)
            {
                var NextPos = bot.Position.Add(Decision.Direction!.Value);
                NewPositions.Add(bot, NextPos);
            }
        }

        foreach (var pos in NewPositions.ToList()) //iterate over a copy
        {
            var Location = GameWorld.Layout[pos.Value].Construct;

            if (Location is Wall)
            {
                NewPositions[pos.Key] = pos.Key.Position; //bot stays in place if hitting wall
            }

            if (Location is Spike)
            {
                ToKill.Add(pos.Key);
            }
        }

        var DuplicateBots = NewPositions.GroupBy(entry => entry.Value)
                                        .Where(duplicates => duplicates.Count() > 1)
                                        .SelectMany(bots => bots.Select(key => key.Key))
                                        .ToList();

        ToKill.AddRange(DuplicateBots);

        foreach (var dead in ToKill.Distinct()) //Don't delete bots that might have been added twice cus that'd error
        {
            AlivePlayers.Remove(dead);
            NewPositions.Remove(dead);
        }

        foreach (var (bot, newPos) in NewPositions)
        {
            GameWorld.Layout[bot.Position].Player = null;
            bot.Position = newPos;
            GameWorld.Layout[newPos].Player = bot;
        }

        //not sure how to do their view yet but may need to update it here
    }

    private void BotBashes()
    {
        /*
        positions have been updated, so now check if the attack in direction hits a bot in its new place
        lunge moves first and then bashes, calculated here
        2 bots bashing eachother at the same time, or lunge + lunge or lunge + bash, results in 
        both the boths getting pushed back 2 spaces should space allow it, this moves you back out of vision from eachother
        whilst potentially killing a person by knocking them into spikes
        */
    }

    private void WorldEdits()  //the current logic will cause scenarios where bots get cut off from eachother and they have to wait to die
    {
        HazardCountdown--;
        var DangerCells = GameWorld.Layout.Where(cell => cell.Value.Construct is Danger);

        foreach (var danger in DangerCells)
        {
            GameWorld.Layout[danger.Key].Construct = Spike.Create(); //Danger => Spike

            if (GameWorld.Layout[danger.Key].Player != null) //Kill Bots on new Spike
            {
                AlivePlayers.Remove(GameWorld.Layout[danger.Key].Player!);
                GameWorld.Layout[danger.Key].Player = null;
            }
        }

        if (HazardCountdown == 0)
        {
            HazardCountdown = 5;
            var EmptyCells = GameWorld.Layout.Where(cell => cell.Value.Construct is Empty && cell.Value.Player == null);
            int Amount = GameWorld.Layout.Count / 10; //10% of cells, rounded down

            var RandomDangerZones = EmptyCells.OrderBy(_ => Guid.NewGuid()).Take(Amount); //Empty cells get randomly ordered and the Amount get taken.

            foreach (var cell in RandomDangerZones) //Random Amount turns Danger
            {
                GameWorld.Layout[cell.Key].Construct = Danger.Create();
            }
        }
    }

    private void VictoryCheck() //victory if you're the last bot alive, draw if you both died same turn aka 0 Bots alive
    {
        if (AlivePlayers.Count == 1)
        {
            State = GameState.Victory;

        }
        if (AlivePlayers.Count == 0)
        {
            State = GameState.Draw;
        }
    }
}