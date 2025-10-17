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
        }
    }

    private void BotDecisions()  //saving bot decisions to run in order later.
    {
        foreach (var Bot in AlivePlayers)
        {
            BotActions.Add(Bot, Bot.RunLogic());
        }
    }

    private void BotMovement()
    {
        /*
        2 bots moving to the same tile causes them both to explode
        not sure how to do their view yet but may need to update it here
        lunge, despite moving, does not get updated here
        */
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
            GameWorld.Layout[danger.Key].Construct = Spike.Create();
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
            State = GameState.Victory;
        }
    }
}