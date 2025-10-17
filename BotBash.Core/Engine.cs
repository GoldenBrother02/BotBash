namespace BotBash.Core;

/// <summary>Calculates the logic of the game and keeps it running.</summary>
public class Engine
{
    private World GameWorld { get; set; }
    private bool Playing = false;
    private int HazardCountdown = 5;

    public Engine(World gameworld)
    {
        GameWorld = gameworld;
    }

    public void Start()
    {
        Playing = true;
        while (Playing)
        {
            /*
            BotDecisions();
            BotMovement();
            BotBashes();
            */
            WorldEdits();
            //VictoryCheck();
        }
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
}