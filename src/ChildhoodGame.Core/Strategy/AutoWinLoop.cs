namespace ChildhoodGame.Core.Strategy;

public sealed record AutoWinOptions(int MaxSteps, int DelayMilliseconds = 0);

public sealed record AutoWinProgress<TState>(
    int Step,
    TState StateBeforeActions,
    IReadOnlyList<string> Actions,
    TState StateAfterActions,
    bool IsWin,
    IReadOnlyList<string> SatisfiedConditions);

public sealed record AutoWinResult<TState>(
    bool IsWin,
    int StepsExecuted,
    IReadOnlyList<AutoWinProgress<TState>> Progress);

public sealed class AutoWinLoop<TState>
{
    private readonly IGameStateRuntime<TState> runtime;
    private readonly IActionSelectionStrategy<TState> actionStrategy;
    private readonly IReadOnlyList<IWinConditionDetector<TState>> winConditionDetectors;

    public AutoWinLoop(
        IGameStateRuntime<TState> runtime,
        IActionSelectionStrategy<TState> actionStrategy,
        IEnumerable<IWinConditionDetector<TState>> winConditionDetectors)
    {
        this.runtime = runtime;
        this.actionStrategy = actionStrategy;
        this.winConditionDetectors = winConditionDetectors.ToArray();
    }

    public async Task<AutoWinResult<TState>> RunAsync(AutoWinOptions options, CancellationToken cancellationToken = default)
    {
        if (options.MaxSteps <= 0)
        {
            return new AutoWinResult<TState>(false, 0, Array.Empty<AutoWinProgress<TState>>());
        }

        var progress = new List<AutoWinProgress<TState>>();

        for (var step = 1; step <= options.MaxSteps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stateBefore = await runtime.ReadStateAsync(cancellationToken);
            var actions = actionStrategy.SelectActions(stateBefore);

            foreach (var action in actions)
            {
                await runtime.SendInputAsync(action, cancellationToken);
            }

            var stateAfter = await runtime.ReadStateAsync(cancellationToken);
            var satisfied = winConditionDetectors
                .Where(detector => detector.IsSatisfied(stateAfter))
                .Select(detector => detector.Name)
                .ToArray();

            var isWin = satisfied.Length == winConditionDetectors.Count;
            progress.Add(new AutoWinProgress<TState>(step, stateBefore, actions, stateAfter, isWin, satisfied));

            if (isWin)
            {
                return new AutoWinResult<TState>(true, step, progress);
            }

            if (actions.Count == 0)
            {
                return new AutoWinResult<TState>(false, step, progress);
            }

            if (options.DelayMilliseconds > 0)
            {
                await Task.Delay(options.DelayMilliseconds, cancellationToken);
            }
        }

        return new AutoWinResult<TState>(false, options.MaxSteps, progress);
    }
}
