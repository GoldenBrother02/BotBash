namespace BotBash.Core;

/// <summary>Calculates the logic of the game and keeps it running.</summary>
public class Engine
{
    private bool Playing = false;
    public void Start()
    {
        Playing = true;
        while (Playing)
        {
            /*
            BotDecisions();
            BotMovement();
            BotAttacks(); //Check for win here or a separate, final, stage?
            WorldEdits();
            */
        }
    }
}