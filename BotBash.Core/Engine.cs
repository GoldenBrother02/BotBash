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
    private List<IBot> StartingPlayers { get; set; } //could be used for a statistic?
    private List<IBot> AlivePlayers = [];
    private Dictionary<IBot, Action> BotActions = [];
    private GameState State;
    private int HazardCountdown = 5;

    public Engine(World gameworld, List<IBot> players)
    {
        GameWorld = gameworld;
        StartingPlayers = players;
        AlivePlayers = new List<IBot>(players);
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
            BotScan();
            VictoryCheck();
            BotActions.Clear();
        }

        if (State is GameState.Victory) { } // *_*-*[TODO]*-*_*
        if (State is GameState.Draw) { } //    *_*-*[TODO]*-*_*
    }

    private void InitialiseGame()
    {
        GameWorld.InitialiseRandom();
        State = GameState.Playing;

        var EmptyCells = GameWorld.Layout.Where(cell => cell.Value.Construct is Empty && cell.Value.Player == null).ToList();
        if (EmptyCells.Count < AlivePlayers.Count) { throw new Exception("Not enough empty cells to place all players!"); }

        var RandomCells = EmptyCells.OrderBy(_ => Guid.NewGuid()).Take(AlivePlayers.Count).ToList();

        for (int i = 0; i < AlivePlayers.Count; i++)
        {
            GameWorld.Layout[RandomCells[i].Key].Player = AlivePlayers[i];
            AlivePlayers[i].Position = RandomCells[i].Key;

            AlivePlayers[i].Vision = 1;
            AlivePlayers[i].ScanCooldown = 0;
            AlivePlayers[i].LungeCooldown = 0;
        }
    }

    private void BotDecisions()  //saving bot decisions to run in order later.
    {
        foreach (var bot in AlivePlayers)
        {
            var VisibleArea = GameWorld.GetVisibleArea(bot.Position, bot.Vision);
            BotActions.Add(bot, bot.RunLogic(VisibleArea));
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
            NewPositions.Remove(dead);
            Kill(dead);
        }

        foreach (var (bot, newPos) in NewPositions)
        {
            GameWorld.Layout[bot.Position].Player = null;
            bot.Position = newPos;
            GameWorld.Layout[newPos].Player = bot;
        }
    }

    private void BotBashes()
    {
        var Bashers = new Dictionary<IBot, (int x, int y)>();
        var Lungers = new Dictionary<IBot, (int x, int y)>();
        var Bashed = new Dictionary<IBot, (int x, int y)>(); //Basher Bashed Target
        //Probably can merge Lungers and Bashers, but I don't feel like it rn
        var ToKill = new List<IBot>();

        foreach (var bot in AlivePlayers)
        {
            var Decision = BotActions[bot];

            if (Decision.Type is ActionType.Bash)
            {
                Bashers.Add(bot, Decision.Direction!.Value);
            }

            if (Decision.Type is ActionType.Lunge)
            {
                if (bot.LungeCooldown != 0) { continue; }
                Lungers.Add(bot, Decision.Direction!.Value);
                bot.LungeCooldown = 3; //2 turn cooldown
            }
        }

        foreach (var bot in Bashers)
        {
            var BashedCell = bot.Key.Position.Add(bot.Value);
            Bashed.Add(bot.Key, BashedCell); //Basher Bashed Target
        }

        foreach (var bot in Lungers)
        {
            int Movement = 1; //how far Lunge lunges
            var EndPos = bot.Key.Position;

            for (int i = 1; i <= Movement; i++) //this is very similar to Bonk so might refactor at some point
            {
                var NextPos = (bot.Key.Position.x + bot.Value.x * i, bot.Key.Position.y + bot.Value.y * i);
                if (GameWorld.Layout[NextPos].Construct is Wall) { break; } //your nose on the wall
                EndPos = NextPos;
            }

            if (GameWorld.Layout[EndPos].Construct is Spike) //can jump over spikes, but not land on them
            {
                ToKill.Add(bot.Key);
            }

            if (GameWorld.Layout[EndPos].Player != null)
            {
                ToKill.Add(bot.Key);
                ToKill.Add(GameWorld.Layout[EndPos].Player!);
            }

            bot.Key.Position = EndPos; //update position
            var BashedCell = EndPos.Add(bot.Value); //Bash after Lunge movement
            Bashed.Add(bot.Key, BashedCell); //Basher Bashed Target
        }

        var HitEachother = Bashed.SelectMany(pair =>  //Pair up A bashing B with B bashing A
                                    Bashed.Where(other =>
                                    other.Key != pair.Key && pair.Value == other.Key.Position && other.Value == pair.Key.Position)
                                    .Select(other => // swap the input so (B, A) becomes (A, B) which is equal to (A, B)
                                        pair.Key.GetHashCode() < other.Key.GetHashCode()
                                            ? (Attacker: pair.Key, Target: other.Key)
                                            : (Attacker: other.Key, Target: pair.Key)))
                                .Distinct() // remove duplicates
                                .ToList();

        foreach (var (basher, target) in HitEachother)
        {
            Bonk(basher, GetIntent(basher, Bashers, Lungers), 2);
            Bonk(target, GetIntent(target, Bashers, Lungers), 2);

            //they don't get to attack twice in a turn
            Bashed.Remove(basher);
            Bashed.Remove(target);
        }

        foreach (var (_, attack) in Bashed.ToList()) //copy for safety
        {
            if (GameWorld.Layout[attack].Player != null)
            {
                ToKill.Add(GameWorld.Layout[attack].Player!);
            }
        }

        foreach (var dead in ToKill)
        {
            Kill(dead);
        }
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

    private void VictoryCheck() //victory if you're the last bot alive, draw if you both died same turn aka 0 Bots alive
    {
        foreach (var bot in AlivePlayers)
        {
            //resets vision each turn, move to render function when making display later
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

    private void Bonk(IBot bot, (int x, int y) direction, int movement = 2)
    {
        var EndPos = bot.Position;
        var Knockback = (-direction.x, -direction.y);

        for (int i = 1; i <= movement; i++)
        {
            var NextPos = (bot.Position.x + Knockback.Item1 * i, bot.Position.y + Knockback.Item2 * i);
            if (!GameWorld.Layout.ContainsKey(NextPos)) { break; } //prevent OoB

            if (GameWorld.Layout[NextPos].Construct is Wall) { break; } //your nose on the wall

            EndPos = NextPos;
        }

        if (GameWorld.Layout[EndPos].Construct is Spike)
        {
            Kill(bot);
        }

        GameWorld.Layout[bot.Position].Player = null;
        bot.Position = EndPos;
        GameWorld.Layout[EndPos].Player = bot;
    }

    private (int x, int y) GetIntent(IBot bot, Dictionary<IBot, (int x, int y)> dict1, Dictionary<IBot, (int x, int y)> dict2)
    {
        if (dict1.ContainsKey(bot)) { return dict1[bot]; }
        else if (dict2.ContainsKey(bot)) { return dict2[bot]; }
        throw new Exception("Where'd you get this bot? It isn't part of your dictionaries!");
    }

    private void Kill(IBot bot)
    {
        if (!AlivePlayers.Remove(bot)) return;
        if (GameWorld.Layout.ContainsKey(bot.Position))
            GameWorld.Layout[bot.Position].Player = null;
    }
}