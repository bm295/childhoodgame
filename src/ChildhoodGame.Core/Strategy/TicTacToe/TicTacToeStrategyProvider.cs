using System.Diagnostics;

namespace ChildhoodGame.Core.Strategy.TicTacToe;

/// <summary>
/// Strategy provider for TicTacToe auto-win mode.
/// Handles game runtime, move strategy selection, and win condition detection.
/// </summary>
public sealed class TicTacToeStrategyProvider : IGameStrategyProvider
{
    public string GameName => "TicTacToe";

    /// <summary>
    /// Difficulty level for the AI strategy. Defaults to Hard.
    /// This can be configured via game.config.json or overridden per game instance.
    /// </summary>
    private readonly TicTacToeDifficulty _difficulty;

    public TicTacToeStrategyProvider(TicTacToeDifficulty difficulty = TicTacToeDifficulty.Hard)
    {
        _difficulty = difficulty;
    }

    public async Task<bool> ExecuteAutoWinAsync(
        GamePackage package,
        AutoWinOptions options,
        Action<string> outputHandler,
        CancellationToken cancellationToken = default)
    {
        await using var runtime = new MockedTicTacToeRuntime();

        await runtime.StartAsync(package, cancellationToken);

        var detectors = new IWinConditionDetector<TicTacToeGameState>[]
        {
            new TicTacToePlayerWinConditionDetector()
        };

        var loop = new AutoWinLoop<TicTacToeGameState>(
            runtime,
            new PaidBetaAlwaysWinActionStrategy(_difficulty),
            detectors);

        var result = await loop.RunAsync(options, cancellationToken);

        outputHandler($"TICTACTOE AUTOWIN MODE START;DIFFICULTY={(int)_difficulty}");
        foreach (var entry in result.Progress)
        {
            var actionsText = entry.Actions.Count == 0 ? "<none>" : string.Join(',', entry.Actions);
            var satisfiedText = entry.SatisfiedConditions.Count == 0 ? "<none>" : string.Join('|', entry.SatisfiedConditions);
            outputHandler(
                $"STEP={entry.Step};RESTARTS={entry.StateAfterActions.RestartCount};BOARD={entry.StateAfterActions.Board};OUTCOME={entry.StateAfterActions.Outcome};ACTIONS={actionsText};CONDITIONS={satisfiedText};WIN={(entry.IsWin ? "yes" : "no")}");
        }

        outputHandler(result.IsWin
            ? $"TICTACTOE AUTOWIN RESULT: WIN in {result.StepsExecuted} step(s)."
            : $"TICTACTOE AUTOWIN RESULT: INCOMPLETE after {result.StepsExecuted} step(s).");

        await runtime.StopAsync(cancellationToken);

        return result.IsWin;
    }
}
