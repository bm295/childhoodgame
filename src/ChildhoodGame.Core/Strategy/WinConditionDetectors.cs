namespace ChildhoodGame.Core.Strategy;

public sealed class ScoreWinConditionDetector : IWinConditionDetector
{
    private readonly int requiredScore;

    public ScoreWinConditionDetector(int requiredScore)
    {
        this.requiredScore = requiredScore;
    }

    public string Name => $"Score >= {requiredScore}";

    public bool IsSatisfied(GameRuntimeState state) => state.Score >= requiredScore;
}

public sealed class LevelWinConditionDetector : IWinConditionDetector
{
    private readonly int requiredLevel;

    public LevelWinConditionDetector(int requiredLevel)
    {
        this.requiredLevel = requiredLevel;
    }

    public string Name => $"Level >= {requiredLevel}";

    public bool IsSatisfied(GameRuntimeState state) => state.Level >= requiredLevel;
}

public sealed class ObjectiveFlagWinConditionDetector : IWinConditionDetector
{
    private readonly string objectiveFlagName;

    public ObjectiveFlagWinConditionDetector(string objectiveFlagName)
    {
        this.objectiveFlagName = objectiveFlagName;
    }

    public string Name => $"Objective '{objectiveFlagName}' complete";

    public bool IsSatisfied(GameRuntimeState state) =>
        state.TryGetObjectiveFlag(objectiveFlagName, out var isCompleted) && isCompleted;
}
