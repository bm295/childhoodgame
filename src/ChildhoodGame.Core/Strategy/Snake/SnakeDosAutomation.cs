namespace ChildhoodGame.Core.Strategy.Snake;

/// <summary>
/// Automation for the classic DOS Snake game using screen capture and AI to win automatically.
/// </summary>
public sealed class SnakeDosAutomation
{
    private readonly SnakeScreenReader _screenReader;
    private readonly ISnakeMoveStrategy _moveStrategy;

    public SnakeDosAutomation(
        SnakeScreenReader? screenReader = null,
        ISnakeMoveStrategy? moveStrategy = null)
    {
        _screenReader = screenReader ?? new SnakeScreenReader();
        _moveStrategy = moveStrategy ?? new SnakeGreedyPathStrategy();
    }

    /// <summary>
    /// Runs the snake automation, attempting to eat apples and win the game.
    /// </summary>
    public async Task<SnakeAutomationResult> RunAsync(
        IGameRuntime runtime,
        IInputController inputController,
        IWindowCaptureRuntime captureRuntime,
        GamePackage gamePackage,
        SnakeAutomationOptions options,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        SnakeGameState? finalState = null;
        var applesEaten = 0;
        var turnCount = 0;

        log?.Invoke($"Snake automation started: attempting to eat {options.ApplesRequiredToWin} apples.");
        
        await Task.Delay(options.InitialDelayMilliseconds, cancellationToken);

        for (var turn = 1; turn <= options.MaxTurnsPerAttempt; turn++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            turnCount++;

            try
            {
                var state = await ReadStateAsync(
                    captureRuntime,
                    options.CaptureWidth,
                    options.CaptureHeight,
                    options.BoardWidth,
                    options.BoardHeight,
                    cancellationToken);
                finalState = state;
                
                log?.Invoke($"Turn {turn}: Snake at {state.SnakeBody[0]}, Apple at {state.ApplePosition}, Apples eaten: {applesEaten}.");

                if (state.IsGameOver)
                {
                    log?.Invoke("Snake died. Ending automation.");
                    break;
                }

                if (state.ApplePosition != null && state.SnakeBody[0].Equals(state.ApplePosition))
                {
                    applesEaten++;
                    log?.Invoke($"Apple eaten! Total: {applesEaten}/{options.ApplesRequiredToWin}");
                }

                if (applesEaten >= options.ApplesRequiredToWin)
                {
                    log?.Invoke($"Snake automation succeeded! Ate {applesEaten} apples.");
                    return new SnakeAutomationResult(true, 1, turnCount, applesEaten, state, "Snake ate the required number of apples.");
                }

                if (turn <= options.StartupNoInputTurns)
                {
                    log?.Invoke($"Startup grace turn {turn}/{options.StartupNoInputTurns}: not sending movement input.");
                    await Task.Delay(options.MoveDelayMilliseconds, cancellationToken);
                    continue;
                }

                var direction = _moveStrategy.SelectNextDirection(state);
                if (direction == SnakeDirection.None)
                {
                    log?.Invoke("No valid moves available. Ending automation.");
                    break;
                }

                var command = EncodeDirection(direction);
                log?.Invoke($"Sending command: {command}");
                await inputController.SendCommandAsync(command, cancellationToken);
                
                await Task.Delay(options.MoveDelayMilliseconds, cancellationToken);
            }
            catch (Exception ex)
            {
                log?.Invoke($"Error during turn {turn}: {ex.Message}");
                break;
            }
        }

        return new SnakeAutomationResult(
            false,
            1,
            turnCount,
            applesEaten,
            finalState,
            $"Snake automation ended after {turnCount} turns. Ate {applesEaten} apples.");
    }

    private async Task<SnakeGameState> ReadStateAsync(
        IWindowCaptureRuntime captureRuntime,
        int captureWidth,
        int captureHeight,
        int boardWidth,
        int boardHeight,
        CancellationToken cancellationToken)
    {
        var capture = await captureRuntime.CaptureWindowAsync(cancellationToken);
        return _screenReader.ReadGameState(capture, boardWidth, boardHeight);
    }

    private static string EncodeDirection(SnakeDirection direction)
    {
        return direction switch
        {
            SnakeDirection.Up => "UP",
            SnakeDirection.Down => "DOWN",
            SnakeDirection.Left => "LEFT",
            SnakeDirection.Right => "RIGHT",
            _ => ""
        };
    }

    private static async Task RestartRuntimeAsync(
        IGameRuntime runtime,
        GamePackage gamePackage,
        int delayMilliseconds,
        CancellationToken cancellationToken)
    {
        await runtime.StopAsync(cancellationToken);
        await Task.Delay(delayMilliseconds, cancellationToken);
        await runtime.StartAsync(gamePackage, cancellationToken);
        await Task.Delay(500, cancellationToken); // Wait for the game to fully start
    }
}

public sealed record SnakeAutomationOptions(
    int ApplesRequiredToWin = 10,
    int InitialDelayMilliseconds = 0,
    int StartupNoInputTurns = 8,
    int MoveDelayMilliseconds = 100,
    int RestartDelayMilliseconds = 1000,
    int MaxAttempts = 5,
    int MaxTurnsPerAttempt = 1000,
    int CaptureWidth = 320,
    int CaptureHeight = 200,
    int BoardWidth = 20,
    int BoardHeight = 20);

public sealed record SnakeAutomationResult(
    bool IsWin,
    int Attempts,
    int TurnsPlayed,
    int ApplesEaten,
    SnakeGameState? FinalState,
    string Summary);
