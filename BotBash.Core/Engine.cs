using System.Numerics;
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
    private List<IBot> StartingPlayers { get; set; } //Could be used for a statistic?
    private List<IBot> AlivePlayers = [];
    private Dictionary<IBot, Action> BotActions = [];
    private GameState State;
    private bool Initialized = false;
    private int HazardCountdown = 5;

    public Func<World, Task>? OnWorldUpdated;

    public Engine(World gameworld, List<IBot> players)
    {
        GameWorld = gameworld;
        StartingPlayers = players;
        AlivePlayers = new List<IBot>(players);
    }

    public async Task Start()
    {
        InitialiseGame();
        Console.Clear();

        while (State is GameState.Playing)
        {
            await GameTick();
            await Task.Delay(500); //2 updates per second
        }

        if (State is GameState.Victory) { Console.WriteLine("Winner"); } // *_-_x*[TODO]*x_-_*
        if (State is GameState.Draw) { Console.WriteLine("Draw"); } //      *_-_x*[TODO]*x_-_*

        /*      //sanity check
                foreach (var kvp in GameWorld.Layout)
                {
                    if (kvp.Value.Player != null)
                    {
                        Console.WriteLine($"Tile {kvp.Key} has player {kvp.Value.Player.GetHashCode()}");
                    }
                }
        */
    }

    public async Task GameTick()
    {
        if (!Initialized || State != GameState.Playing) { return; }

        BotDecisions();
        BotMovement();
        BotBashes();
        WorldEdits();
        BotScan();
        VictoryCheck();

        BotActions.Clear();
        Console.WriteLine($"Alive players: {AlivePlayers.Count}");

        if (OnWorldUpdated != null) { await OnWorldUpdated(GameWorld); }

        if (State is GameState.Victory) { Console.WriteLine("Winner"); } // *_-_x*[TODO]*x_-_*
        if (State is GameState.Draw) { Console.WriteLine("Draw"); } //      *_-_x*[TODO]*x_-_*
    }

    public void InitialiseGame()
    {
        if (Initialized) { return; }
        Initialized = true;

        GameWorld.InitialiseRandom();
        State = GameState.Playing;

        var EmptyCells = GameWorld.Layout.Where(cell => cell.Value.Construct is Empty && cell.Value.Player == null).ToList();
        if (EmptyCells.Count < AlivePlayers.Count) { throw new Exception("Not enough empty cells to place all players!"); }

        var RandomCells = EmptyCells.OrderBy(_ => Guid.NewGuid()).Take(AlivePlayers.Count).ToList();

        for (int i = 0; i < AlivePlayers.Count; i++)
        {
            MoveBotToTile(AlivePlayers[i], RandomCells[i].Key);

            AlivePlayers[i].Vision = 1;
            AlivePlayers[i].ScanCooldown = 0;
            AlivePlayers[i].LungeCooldown = 0;
        }
    }

    private void BotDecisions()
    {
        foreach (var bot in AlivePlayers)
        {
            var VisibleArea = GameWorld.GetVisibleArea(bot.Position, bot.Vision);
            BotActions.Add(bot, bot.RunLogic(VisibleArea));
        }
    }

    private void BotMovement()
    {
        var NewPositions = new Dictionary<IBot, Coordinate>();
        var ToKill = new List<IBot>();

        foreach (var bot in AlivePlayers)
        {
            var Decision = BotActions[bot];
            if (Decision.Type is ActionType.Move)
            {
                var NextPos = bot.Position + Decision.Direction!.Value;
                NewPositions.Add(bot, NextPos);
            }
        }

        foreach (var pos in NewPositions.ToList()) //Iterate over a copy
        {
            if (TryGetCell(pos.Value, out var cell))
            {
                if (cell.Construct is Wall)
                {
                    NewPositions[pos.Key] = pos.Key.Position; //Bot stays in place if hitting wall
                }

                if (cell.Construct is Spike)
                {
                    ToKill.Add(pos.Key);
                }
            }
        }

        //Bots go to same tile
        var DuplicateBots = NewPositions.GroupBy(entry => entry.Value)
                                        .Where(duplicates => duplicates.Count() > 1)
                                        .SelectMany(bots => bots.Select(key => key.Key));

        //Bots go to eachother's tile
        var SwapPairs = NewPositions.Where(pair => NewPositions
                                        .Any(other => other.Key != pair.Key
                                            && other.Value.Equals(pair.Key.Position)
                                            && pair.Value.Equals(other.Key.Position)))
                                    .Select(pair => pair.Key);

        ToKill.AddRange(DuplicateBots);
        ToKill.AddRange(SwapPairs);

        foreach (var dead in ToKill.Distinct()) //Don't delete bots that might have been added twice cus that'd error
        {
            NewPositions.Remove(dead);
            Kill(dead);
        }

        foreach (var (bot, newPos) in NewPositions)
        {
            if (GameWorld.IsInBounds(newPos))
            {
                MoveBotToTile(bot, newPos);
            }
        }
    }

    private void BotBashes()
    {
        var Bashed = new Dictionary<IBot, Coordinate>(); //Basher Bashed Target
        var ToKill = new List<IBot>();

        foreach (var bot in AlivePlayers)
        {
            var Decision = BotActions[bot];

            if (Decision.Type is ActionType.Bash)
            {
                DoBash(bot, Decision, Bashed);
            }

            if (Decision.Type is ActionType.Lunge)
            {
                if (bot.LungeCooldown != 0) { continue; }
                DoLunge(bot, Decision, Bashed, ToKill);
                bot.LungeCooldown = 3; //2 turn cooldown
            }
        }

        var HitEachother = Bashed.SelectMany(pair =>  //Pair up A bashing B with B bashing A
                                    Bashed.Where(other =>
                                    other.Key != pair.Key && pair.Value == other.Key.Position && other.Value == pair.Key.Position)
                                    .Select(other => //Swap the input so (B, A) becomes (A, B) which is equal to (A, B)
                                        pair.Key.GetHashCode() < other.Key.GetHashCode()
                                            ? (Attacker: pair.Key, Target: other.Key)
                                            : (Attacker: other.Key, Target: pair.Key)))
                                .Distinct() //Remove duplicates
                                .ToList();

        foreach (var (basher, target) in HitEachother)
        {
            if (!AlivePlayers.Contains(basher) || !AlivePlayers.Contains(target)) { continue; }

            Bonk(basher, BotActions[basher].Direction!.Value, 2);
            Bonk(target, BotActions[target].Direction!.Value, 2);

            //They don't get to attack twice in a turn
            Bashed.Remove(basher);
            Bashed.Remove(target);
        }

        foreach (var (_, attack) in Bashed)
        {
            if (TryGetCell(attack, out var cell))
            {
                var victim = cell.Player;
                if (victim != null && AlivePlayers.Contains(victim))
                {
                    ToKill.Add(victim);
                }
            }
        }

        foreach (var dead in ToKill.Distinct())
        {
            Kill(dead);
        }
    }

    private void WorldEdits()  //The current logic will cause scenarios where bots get cut off from eachother and they have to wait to die
    {
        HazardCountdown--;
        var DangerCells = GameWorld.Layout.Where(cell => cell.Value.Construct is Danger);

        foreach (var danger in DangerCells)
        {
            GameWorld.Layout[danger.Key].Construct = Spike.Create(); //Danger => Spike

            if (GameWorld.Layout[danger.Key].Player != null) //Kill Bots on new Spike
            {
                Kill(GameWorld.Layout[danger.Key].Player!);
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

    private void BotScan()
    {
        foreach (var bot in AlivePlayers)
        {
            var Decision = BotActions[bot];
            if (Decision.Type is ActionType.Scan && bot.ScanCooldown == 0)
            {
                bot.Vision = 5;
                bot.ScanCooldown = 2;  //1 turn cooldown
            }
        }
    }

    private void VictoryCheck()
    {
        foreach (var bot in AlivePlayers)
        {
            //Resets vision each turn, move to render function when making display outside of console later
            bot.ScanCooldown -= 1;
            bot.LungeCooldown = Math.Max(0, bot.LungeCooldown - 1);
            if (bot.ScanCooldown == 0) { bot.Vision = 1; }
        }

        if (AlivePlayers.Count == 1)
        {
            State = GameState.Victory;

        }
        if (AlivePlayers.Count == 0)
        {
            State = GameState.Draw;
        }
    }

    //
    //
    //

    private void Bonk(IBot bot, Coordinate direction, int movement = 2)
    {
        if (!AlivePlayers.Contains(bot)) { return; }

        var Knockback = new Coordinate(-direction.X, -direction.Y);
        var EndPos = GoTheDistance(bot.Position, Knockback, movement);

        if (GameWorld.Layout[EndPos].Construct is Spike) //I don't want to run TryGetcell again just to get cell, should do differently
        {
            Kill(bot);
            return;
        }

        MoveBotToTile(bot, EndPos);
    }

    private void Kill(IBot bot)
    {
        //Now removes every mention of the bot on the field
        foreach (var cell in GameWorld.Layout)
        {
            if (cell.Value.Player == bot)
                cell.Value.Player = null;
        }

        AlivePlayers.Remove(bot);
    }

    private void DoBash(IBot bot, Action decision, Dictionary<IBot, Coordinate> Bashed)
    {
        var BashedCell = bot.Position + decision.Direction!.Value;
        Bashed[bot] = BashedCell;
    }

    private void DoLunge(IBot bot, Action decision, Dictionary<IBot, Coordinate> Bashed, List<IBot> ToKill)
    {
        int Movement = 1; //How far Lunge lunges
        var EndPos = GoTheDistance(bot.Position, decision.Direction!.Value, Movement);

        if (GameWorld.Layout[EndPos].Construct is Spike) //Can jump over spikes, but not land on them
        {
            ToKill.Add(bot);
            return;
        }

        if (GameWorld.Layout[EndPos].Player != null)
        {
            ToKill.Add(bot);
            ToKill.Add(GameWorld.Layout[EndPos].Player!);
            return;
        }

        MoveBotToTile(bot, EndPos);
        var BashedCell = EndPos + decision.Direction!.Value; //Bash after Lunge movement
        Bashed[bot] = BashedCell; //Basher Bashed Target
    }

    private Coordinate GoTheDistance(Coordinate start, Coordinate direction, int maxSteps)
    {
        var EndPos = start;

        for (int i = 1; i <= maxSteps; i++)
        {
            var NextPos = new Coordinate(start.X + direction.X * i, start.Y + direction.Y * i);

            if (!TryGetCell(NextPos, out var cell)) { break; }
            if (cell.Construct is Wall) { break; } //your nose on the wall

            EndPos = NextPos;
        }

        return EndPos;
    }

    private bool TryGetCell(Coordinate pos, out Cell cell)
    {
        if (GameWorld.IsInBounds(pos))
        {
            cell = GameWorld.Layout[pos];
            return true;
        }
        cell = null!;
        return false;
    }

    private void MoveBotToTile(IBot bot, Coordinate pos)
    {
        if (!AlivePlayers.Contains(bot)) return;

        //Remove bot from any tile
        foreach (var cell in GameWorld.Layout.Values)
        {
            if (cell.Player == bot)
                cell.Player = null;
        }

        //Assign to new tile
        GameWorld.Layout[pos].Player = bot;
        bot.Position = pos;
    }
}