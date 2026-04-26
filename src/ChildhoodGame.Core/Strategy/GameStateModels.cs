namespace ChildhoodGame.Core.Strategy;

public sealed record GameRuntimeState(
    int Score,
    int Level,
    IReadOnlyDictionary<string, bool> ObjectiveFlags)
{
    public bool TryGetObjectiveFlag(string flagName, out bool isCompleted)
    {
        if (ObjectiveFlags.TryGetValue(flagName, out var value))
        {
            isCompleted = value;
            return true;
        }

        isCompleted = false;
        return false;
    }
}

public interface IGameStateRuntime : IGameRuntime
{
    Task<GameRuntimeState> ReadStateAsync(CancellationToken cancellationToken = default);
}
