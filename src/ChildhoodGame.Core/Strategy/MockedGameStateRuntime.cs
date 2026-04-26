namespace ChildhoodGame.Core.Strategy;

public sealed class MockedGameStateRuntime : IGameStateRuntime
{
    private GameRuntimeState state;

    public MockedGameStateRuntime(GameRuntimeState initialState)
    {
        state = initialState;
    }

    public bool IsRunning { get; private set; }

    public Task StartAsync(GamePackage gamePackage, CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public Task SendInputAsync(string inputCommand, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException("Runtime has not been started.");
        }

        var objectiveFlags = new Dictionary<string, bool>(state.ObjectiveFlags, StringComparer.OrdinalIgnoreCase);

        if (inputCommand.Equals("GAIN_SCORE", StringComparison.OrdinalIgnoreCase))
        {
            state = state with { Score = state.Score + 10 };
            return Task.CompletedTask;
        }

        if (inputCommand.Equals("ADVANCE_LEVEL", StringComparison.OrdinalIgnoreCase))
        {
            state = state with { Level = state.Level + 1 };
            return Task.CompletedTask;
        }

        if (inputCommand.StartsWith("COMPLETE_OBJECTIVE:", StringComparison.OrdinalIgnoreCase))
        {
            var objective = inputCommand["COMPLETE_OBJECTIVE:".Length..];
            objectiveFlags[objective] = true;
            state = state with { ObjectiveFlags = objectiveFlags };
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    public Task<GameRuntimeState> ReadStateAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException("Runtime has not been started.");
        }

        var snapshot = state with { ObjectiveFlags = new Dictionary<string, bool>(state.ObjectiveFlags, StringComparer.OrdinalIgnoreCase) };
        return Task.FromResult(snapshot);
    }

    public ValueTask DisposeAsync()
    {
        IsRunning = false;
        return ValueTask.CompletedTask;
    }
}
