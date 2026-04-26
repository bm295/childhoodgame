namespace ChildhoodGame.Core.Strategy;

public sealed record AutoWinOptions(int MaxSteps, int DelayMilliseconds = 0);

public sealed record AutoWinProgress(
    int Step,
    GameRuntimeState StateBeforeActions,
    IReadOnlyList<string> Actions,
    GameRuntimeState StateAfterActions,
    bool IsWin,
    IReadOnlyList<string> SatisfiedConditions);

public sealed record AutoWinResult(bool IsWin, int StepsExecuted, IReadOnlyList<AutoWinProgress> Progress);

public sealed class AutoWinLoop
{
    private readonly IGameStateRuntime runtime;
    private readonly IActionSelectionStrategy actionStrategy;
    private readonly IReadOnlyList<IWinConditionDetector> winConditionDetectors;

    public AutoWinLoop(
        IGameStateRuntime runtime,
        IActionSelectionStrategy actionStrategy,
        IEnumerable<IWinConditionDetector> winConditionDetectors)
    {
        this.runtime = runtime;
        this.actionStrategy = actionStrategy;
        this.winConditionDetectors = winConditionDetectors.ToArray();
    }

    public async Task<AutoWinResult> RunAsync(AutoWinOptions options, CancellationToken cancellationToken = default)
    {
        if (options.MaxSteps <= 0)
        {
            return new AutoWinResult(false, 0, Array.Empty<AutoWinProgress>());
        }

        var progress = new List<AutoWinProgress>();

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
            progress.Add(new AutoWinProgress(step, stateBefore, actions, stateAfter, isWin, satisfied));

            if (isWin)
            {
                return new AutoWinResult(true, step, progress);
            }

            if (options.DelayMilliseconds > 0)
            {
                await Task.Delay(options.DelayMilliseconds, cancellationToken);
            }
        }

        return new AutoWinResult(false, options.MaxSteps, progress);
    }
}
