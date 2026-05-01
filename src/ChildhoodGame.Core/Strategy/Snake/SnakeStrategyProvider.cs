namespace ChildhoodGame.Core.Strategy.Snake;

/// <summary>
/// Strategy provider for Snake auto-win mode.
/// Handles game runtime, move strategy selection, and win condition detection.
/// </summary>
public sealed class SnakeStrategyProvider : IGameStrategyProvider
{
    public string GameName => "Snake";

    /// <summary>
    /// Number of apples the snake must eat to win. Defaults to 10.
    /// This can be configured via game.config.json or overridden per game instance.
    /// </summary>
    private readonly int _applesRequiredToWin;

    public SnakeStrategyProvider(int applesRequiredToWin = 10)
    {
        if (applesRequiredToWin <= 0)
        {
            throw new ArgumentException("Apples required must be positive.", nameof(applesRequiredToWin));
        }

        _applesRequiredToWin = applesRequiredToWin;
    }

    public async Task<bool> ExecuteAutoWinAsync(
        GamePackage package,
        AutoWinOptions options,
        Action<string> outputHandler,
        CancellationToken cancellationToken = default)
    {
        await using var runtime = new MockedSnakeRuntime();

        await runtime.StartAsync(package, cancellationToken);

        var detectors = new IWinConditionDetector<SnakeGameState>[]
        {
            new SnakeWinConditionDetector(_applesRequiredToWin)
        };

        var moveStrategy = new SnakeGreedyPathStrategy();
        var actionStrategy = new SnakeActionStrategy(moveStrategy);

        var loop = new AutoWinLoop<SnakeGameState>(
            runtime,
            actionStrategy,
            detectors);

        var result = await loop.RunAsync(options, cancellationToken);

        outputHandler($"SNAKE AUTOWIN MODE START;APPLES_REQUIRED={_applesRequiredToWin}");
        foreach (var entry in result.Progress)
        {
            var actionsText = entry.Actions.Count == 0 ? "<none>" : string.Join(',', entry.Actions);
            var satisfiedText = entry.SatisfiedConditions.Count == 0 ? "<none>" : string.Join('|', entry.SatisfiedConditions);
            var state = entry.StateAfterActions;
            outputHandler(
                $"STEP={entry.Step};LENGTH={state.SnakeBody.Count};APPLES={state.ApplesEaten};BOARD={state.Board};ACTIONS={actionsText};CONDITIONS={satisfiedText};WIN={(entry.IsWin ? "yes" : "no")}");
        }

        outputHandler(result.IsWin
            ? $"SNAKE AUTOWIN RESULT: WIN in {result.StepsExecuted} step(s)."
            : $"SNAKE AUTOWIN RESULT: INCOMPLETE after {result.StepsExecuted} step(s).");

        await runtime.StopAsync(cancellationToken);

        return result.IsWin;
    }
}
