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
    private readonly SemaphoreSlim _tickSemaphore = new SemaphoreSlim(1, 1);
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
    }

    public async Task GameTick()
    {
        if (!Initialized || State != GameState.Playing) { return; }

        if (!await _tickSemaphore.WaitAsync(0)) { return; } //Another tick running
        try
        {
            BotDecisions();
            BotMovement();
            BotBashes();
            WorldEdits();
            BotScan();
            VictoryCheck();

            Console.WriteLine($"Alive players: {AlivePlayers.Count}");
        }
        finally { _tickSemaphore.Release(); }

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

        var EmptyCells = GameWorld.Layout.Where(cell => cell.Value.Construct is Empty && cell.Value.Player == null)
                                         .Select(empty => empty.Key)
                                         .ToList();

        if (EmptyCells.Count < AlivePlayers.Count) { throw new Exception("Not enough empty cells to place all players!"); }

        var AvailableCells = EmptyCells.OrderBy(_ => Guid.NewGuid()).ToList();
        var RNG = new Random();
        var PlacedPositions = new HashSet<Coordinate>();

        foreach (var bot in AlivePlayers)
        {
            //Empty cells not already taken
            var ValidChoices = EmptyCells.Except(PlacedPositions)
                                         .Where(cell =>  //Don't like this bit but it auto indents
                                         {
                                             var TempPositions = new HashSet<Coordinate>(PlacedPositions) { cell };
                                             return AreAllPlayersConnected(TempPositions);
                                         })
                                         .ToList();

            if (ValidChoices.Count == 0) { throw new Exception("Cannot place all bots connected on empty tiles!"); }

            var Chosen = ValidChoices[RNG.Next(ValidChoices.Count)];

            bot.Position = Chosen;
            GameWorld.Layout[Chosen].Player = bot;
            PlacedPositions.Add(Chosen);

            //Initialize bot stats
            bot.Vision = 1;
            bot.ScanCooldown = 0;
            bot.LungeCooldown = 0;
            bot.GameAction = new BotAction();

            Console.WriteLine($"Bot {bot} spawns at {Chosen}");
        }
    }

    private void BotDecisions()
    {
        BotActions.Clear();
        foreach (var bot in AlivePlayers)
        {
            var VisibleInfo = GameWorld.GetVisibleInfo(bot.Position, bot.Vision);
            var Action = bot.RunLogic(VisibleInfo) ?? bot.GameAction!.Wait();
            BotActions[bot] = Action;
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

                if (!GameWorld.IsInBounds(NextPos)) { NewPositions.Add(bot, bot.Position); } //Stay in bounds
                else { NewPositions.Add(bot, NextPos); }
            }
        }

        foreach (var pos in NewPositions.ToList()) //Iterate over a copy
        {
            if (TryGetCell(pos.Value, out var cell))
            {
                //Bot stays in place if hitting wall
                if (cell.Construct is Wall) { NewPositions[pos.Key] = pos.Key.Position; }

                if (cell.Construct is Spike) { ToKill.Add(pos.Key); }
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

        foreach (var dead in ToKill.Distinct())
        {
            NewPositions.Remove(dead);
            Kill(dead);
        }

        foreach (var (bot, newPos) in NewPositions)
        {
            if (GameWorld.IsInBounds(newPos)) { MoveBotToTile(bot, newPos); }
        }
    }

    private void BotBashes()
    {
        var Bashed = new Dictionary<IBot, Coordinate>(); //Basher Bashed Target
        var ToKill = new List<IBot>();

        foreach (var bot in AlivePlayers)
        {
            var Decision = BotActions[bot];

            if (Decision.Type is ActionType.Bash) { DoBash(bot, Decision, Bashed); }

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
                                 .Distinct(); //Remove duplicates

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
                if (victim != null && AlivePlayers.Contains(victim)) { ToKill.Add(victim); }
            }
        }

        foreach (var dead in ToKill.Distinct())
        {
            Kill(dead);
        }
    }

    private void WorldEdits()
    {
        HazardCountdown--;

        if (HazardCountdown == 4) //1 turn for danger to become spikes, can change
        {
            var DangerCells = GameWorld.Layout.Where(cell => cell.Value.Construct is Danger);
            foreach (var danger in DangerCells)
            {
                GameWorld.Layout[danger.Key].Construct = Spike.Create(); //Danger => Spike

                if (GameWorld.Layout[danger.Key].Player != null) //Kill Bots on new Spike
                {
                    Kill(GameWorld.Layout[danger.Key].Player!);
                }
            }
        }

        if (HazardCountdown == 0)
        {
            HazardCountdown = 5;
            var EmptyCells = GameWorld.Layout.Where(cell => cell.Value.Construct is Empty && cell.Value.Player == null)
                                             .Select(empty => empty.Key)
                                             .OrderBy(_ => Guid.NewGuid())
                                             .ToList();

            int Amount = GameWorld.Layout.Count / 20; //5% of cells, rounded down
            int Placed = 0;

            foreach (var empty in EmptyCells)
            {
                //Place Danger
                GameWorld.Layout[empty].Construct = Danger.Create();

                //Check all players still connected
                var PlayerPositions = AlivePlayers.Select(player => player.Position).ToHashSet();
                if (!AreAllPlayersConnected(PlayerPositions))
                {
                    //Undo if disconnected
                    GameWorld.Layout[empty].Construct = Empty.Create();
                    continue;
                }

                Placed++;
                if (Placed >= Amount) break;
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
            bot.ScanCooldown = Math.Max(0, bot.ScanCooldown - 1);
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

    // ↑ Game Logic
    // |
    // ↓ Helper Functions

    private void Bonk(IBot bot, Coordinate direction, int movement = 2)
    {
        if (!AlivePlayers.Contains(bot)) { return; }

        var Knockback = new Coordinate(-direction.X, -direction.Y);
        var EndPos = GoTheDistance(bot.Position, Knockback, movement);

        if (GameWorld.Layout[EndPos].Construct is Spike)
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
            if (cell.Value.Player == bot) { cell.Value.Player = null; }
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
        var TargetPos = bot.Position + decision.Direction!.Value;

        if (!GameWorld.IsInBounds(TargetPos)) { return; } //ALL BECAUSE I DIDN'T CHECK THE IMMEDIATE TILE NEXT TO ME?!?!?!
        if (GameWorld.Layout[TargetPos].Construct is Wall) { return; }

        if (!GameWorld.IsInBounds(EndPos) || GameWorld.Layout[EndPos].Construct is Wall) { return; }

        if (GameWorld.Layout[EndPos].Construct is Spike)
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

        if (GameWorld.IsInBounds(BashedCell) && GameWorld.Layout[BashedCell].Construct is not Wall) { Bashed[bot] = BashedCell; }
    }

    private Coordinate GoTheDistance(Coordinate start, Coordinate direction, int maxSteps)
    {
        var EndPos = start;

        for (int i = 1; i <= maxSteps; i++)
        {
            var NextPos = new Coordinate(EndPos.X + direction.X, EndPos.Y + direction.Y);

            if (!TryGetCell(NextPos, out var cell)) { break; }
            if (cell.Construct is Wall) { break; } //your nose on the wall

            EndPos = NextPos;
        }

        return EndPos;
    }

    private void MoveBotToTile(IBot bot, Coordinate pos)
    {
        if (!AlivePlayers.Contains(bot)) { return; }
        if (!GameWorld.Layout.TryGetValue(pos, out Cell? Destination)) { return; }

        if (Destination.Construct is Wall) { return; }

        if (GameWorld.Layout.TryGetValue(bot.Position, out var currentCell))
        {
            if (currentCell.Player == bot) { currentCell.Player = null; }
        }

        //Assign to new tile
        Destination.Player = bot;
        bot.Position = pos;
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

    private bool AreAllPlayersConnected(HashSet<Coordinate> PlayerPositions)
    {
        if (PlayerPositions.Count == 0) { return true; }

        var Visited = new HashSet<Coordinate>();
        var Queue = new Queue<Coordinate>();
        var Directions = new List<Coordinate>
    {
        new Coordinate(0, 1),
        new Coordinate(0, -1),
        new Coordinate(1, 0),
        new Coordinate(-1, 0)
    };

        //Start from the first
        Queue.Enqueue(PlayerPositions.First());
        Visited.Add(PlayerPositions.First());

        while (Queue.Count > 0)
        {
            var Current = Queue.Dequeue();

            foreach (var direction in Directions)
            {
                var Neighbor = Current + direction;
                if (Visited.Contains(Neighbor)) { continue; }
                if (!GameWorld.IsInBounds(Neighbor)) { continue; }

                //Only consider Empty tiles
                if (GameWorld.Layout[Neighbor].Construct is Empty)
                {
                    Visited.Add(Neighbor);
                    Queue.Enqueue(Neighbor);
                }
            }
        }

        //All players must be on Visited tiles
        return PlayerPositions.All(player => Visited.Contains(player));
    }
}