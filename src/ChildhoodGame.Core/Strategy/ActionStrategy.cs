namespace ChildhoodGame.Core.Strategy;

public interface IActionSelectionStrategy
{
    IReadOnlyList<string> SelectActions(GameRuntimeState state);
}

public sealed class DeterministicAutoWinActionStrategy : IActionSelectionStrategy
{
    public const string BossKeyObjective = "boss_key";

    public IReadOnlyList<string> SelectActions(GameRuntimeState state)
    {
        if (state.Score < 50)
        {
            return new[] { "GAIN_SCORE" };
        }

        if (state.Level < 3)
        {
            return new[] { "ADVANCE_LEVEL" };
        }

        if (!state.TryGetObjectiveFlag(BossKeyObjective, out var objectiveComplete) || !objectiveComplete)
        {
            return new[] { $"COMPLETE_OBJECTIVE:{BossKeyObjective}" };
        }

        return Array.Empty<string>();
    }
}
