namespace ChildhoodGame.Core;

public sealed class GameOrchestrator
{
    public GameSessionResult Run(GameSessionRequest request)
    {
        if (request.MaxTurns <= 0)
        {
            return new GameSessionResult(false, "No turns were available for the session.", 0);
        }

        var turnsPlayed = 0;

        while (turnsPlayed < request.MaxTurns)
        {
            turnsPlayed++;
            if (turnsPlayed >= request.TurnsRequiredToWin)
            {
                return new GameSessionResult(
                    true,
                    $"Victory unlocked after {turnsPlayed} turn(s) in {request.DosGameTitle}.",
                    turnsPlayed);
            }
        }

        return new GameSessionResult(false, "Session ended before the win condition was met.", turnsPlayed);
    }
}

public sealed record GameSessionRequest(string DosGameTitle, int MaxTurns, int TurnsRequiredToWin);

public sealed record GameSessionResult(bool IsWin, string Summary, int TurnsPlayed);
