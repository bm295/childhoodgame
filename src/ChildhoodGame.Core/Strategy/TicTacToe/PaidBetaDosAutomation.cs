namespace ChildhoodGame.Core.Strategy.TicTacToe;

public sealed record PaidBetaAutomationOptions(
    TicTacToeDifficulty Difficulty = TicTacToeDifficulty.Hard,
    int InitialDelayMilliseconds = 1200,
    int AfterDifficultyDelayMilliseconds = 600,
    int AfterMoveDelayMilliseconds = 800,
    int RestartDelayMilliseconds = 600,
    int MaxAttempts = 12,
    int MaxTurnsPerAttempt = 8);

public sealed record PaidBetaAutomationResult(
    bool IsWin,
    int Attempts,
    TicTacToeGameState? FinalState,
    string Summary);

public sealed class PaidBetaDosAutomation
{
    private readonly PaidBetaScreenReader screenReader;
    private readonly PaidBetaAlwaysWinActionStrategy actionStrategy;

    public PaidBetaDosAutomation(
        PaidBetaScreenReader? screenReader = null,
        PaidBetaAlwaysWinActionStrategy? actionStrategy = null)
    {
        this.screenReader = screenReader ?? new PaidBetaScreenReader();
        this.actionStrategy = actionStrategy ?? new PaidBetaAlwaysWinActionStrategy();
    }

    public async Task<PaidBetaAutomationResult> RunAsync(
        IGameRuntime runtime,
        IInputController inputController,
        IWindowCaptureRuntime captureRuntime,
        GamePackage gamePackage,
        PaidBetaAutomationOptions options,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        TicTacToeGameState? finalState = null;

        for (var attempt = 1; attempt <= options.MaxAttempts; attempt++)
        {
            log?.Invoke($"PAIDBETA automation attempt {attempt}: choosing difficulty {(int)options.Difficulty}.");
            await Task.Delay(options.InitialDelayMilliseconds, cancellationToken);
            await inputController.SendCommandAsync(TicTacToeInput.SelectDifficulty(options.Difficulty), cancellationToken);
            await Task.Delay(options.AfterDifficultyDelayMilliseconds, cancellationToken);

            for (var turn = 1; turn <= options.MaxTurnsPerAttempt; turn++)
            {
                var state = await ReadStateAsync(captureRuntime, options.Difficulty, cancellationToken);
                finalState = state;
                log?.Invoke($"PAIDBETA observed board: {state.Board}; outcome={state.Outcome}.");

                if (state.Outcome == TicTacToeOutcome.PlayerWin)
                {
                    return new PaidBetaAutomationResult(true, attempt, state, "Human O won.");
                }

                if (state.Outcome is TicTacToeOutcome.ComputerWin or TicTacToeOutcome.Draw)
                {
                    break;
                }

                var actions = actionStrategy.SelectActions(state);
                if (actions.Count == 0)
                {
                    break;
                }

                if (actions.Any(action => action.Equals(TicTacToeInput.RestartCommand, StringComparison.OrdinalIgnoreCase)))
                {
                    log?.Invoke("PAIDBETA RNG branch cannot be forced to win; restarting DOS runtime.");
                    await RestartRuntimeAsync(runtime, gamePackage, options.RestartDelayMilliseconds, cancellationToken);
                    finalState = null;
                    break;
                }

                foreach (var action in actions)
                {
                    log?.Invoke($"PAIDBETA sending move: {action}.");
                    await inputController.SendCommandAsync(action, cancellationToken);
                    await Task.Delay(options.AfterMoveDelayMilliseconds, cancellationToken);
                }
            }

            if (attempt < options.MaxAttempts)
            {
                log?.Invoke("PAIDBETA attempt ended without a win; restarting DOS runtime.");
                await RestartRuntimeAsync(runtime, gamePackage, options.RestartDelayMilliseconds, cancellationToken);
            }
        }

        return new PaidBetaAutomationResult(
            false,
            options.MaxAttempts,
            finalState,
            "Unable to force a win within the configured attempt limit.");
    }

    private async Task<TicTacToeGameState> ReadStateAsync(
        IWindowCaptureRuntime captureRuntime,
        TicTacToeDifficulty difficulty,
        CancellationToken cancellationToken)
    {
        var capture = await captureRuntime.CaptureWindowAsync(cancellationToken);
        return screenReader.ReadState(capture, difficulty);
    }

    private static async Task RestartRuntimeAsync(
        IGameRuntime runtime,
        GamePackage gamePackage,
        int restartDelayMilliseconds,
        CancellationToken cancellationToken)
    {
        await runtime.StopAsync(cancellationToken);
        await Task.Delay(restartDelayMilliseconds, cancellationToken);
        await runtime.StartAsync(gamePackage, cancellationToken);
    }
}
