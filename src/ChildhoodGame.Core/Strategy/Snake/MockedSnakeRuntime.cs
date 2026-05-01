using System.Collections.Immutable;

namespace ChildhoodGame.Core.Strategy.Snake;

/// <summary>
/// In-memory mocked Snake game runtime for testing and automation.
/// Simulates the snake movement, collisions, and apple mechanics without requiring DOS emulation.
/// </summary>
public sealed class MockedSnakeRuntime : IGameStateRuntime<SnakeGameState>
{
    private SnakeGameState? _currentState;
    private bool _isRunning;
    private bool _disposed;

    // Standard game board size (20x20 like classic Snake)
    private const int BoardWidth = 20;
    private const int BoardHeight = 20;

    public bool IsRunning => _isRunning && !_disposed;

    public async Task StartAsync(GamePackage gamePackage, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MockedSnakeRuntime));
        }

        await Task.Yield(); // Simulate async work

        _currentState = InitializeGameState();
        _isRunning = true;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Simulate async work
        _isRunning = false;
    }

    public async Task<SnakeGameState> ReadStateAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning || _currentState == null)
        {
            throw new InvalidOperationException("Runtime is not running.");
        }

        await Task.Yield(); // Simulate async read

        return _currentState.Clone();
    }

    public async Task SendInputAsync(string inputCommand, CancellationToken cancellationToken = default)
    {
        if (!_isRunning || _currentState == null)
        {
            throw new InvalidOperationException("Runtime is not running.");
        }

        await Task.Yield(); // Simulate async input

        // Parse the input command and apply game logic
        var direction = ParseDirection(inputCommand);
        if (direction != SnakeDirection.None)
        {
            _currentState.NextDirection = direction;
        }

        // Simulate one game step
        SimulateGameStep(_currentState);
    }

    /// <summary>
    /// Initializes the game state with a standard snake at the center of the board and an apple.
    /// </summary>
    private static SnakeGameState InitializeGameState()
    {
        var board = new SnakeBoard(BoardWidth, BoardHeight);

        // Start the snake in the middle, 3 segments long, moving right
        var snakeHead = new SnakeSegment(BoardWidth / 2, BoardHeight / 2);
        var snakeBody = ImmutableList.Create(
            snakeHead,
            new SnakeSegment(snakeHead.X - 1, snakeHead.Y),
            new SnakeSegment(snakeHead.X - 2, snakeHead.Y)
        );

        // Place the snake on the board
        board.Set(snakeHead.X, snakeHead.Y, SnakeMark.SnakeHead);
        board.Set(snakeBody[1].X, snakeBody[1].Y, SnakeMark.SnakeBody);
        board.Set(snakeBody[2].X, snakeBody[2].Y, SnakeMark.SnakeBody);

        // Place an apple at a random location (simple: right side of board)
        var appleX = BoardWidth - 2;
        var appleY = BoardHeight / 2;
        var apple = new SnakeSegment(appleX, appleY);
        board.Set(appleX, appleY, SnakeMark.Apple);

        return new SnakeGameState(board, snakeBody, apple);
    }

    /// <summary>
    /// Simulates one step of the game:
    /// 1. Update the snake's current direction
    /// 2. Move the snake head
    /// 3. Check for collisions and apple consumption
    /// 4. Update the board
    /// </summary>
    private static void SimulateGameStep(SnakeGameState state)
    {
        if (state.IsGameOver)
        {
            return;
        }

        // Update direction if a new one was queued
        if (state.NextDirection != SnakeDirection.None && 
            !IsOppositeDirection(state.CurrentDirection, state.NextDirection))
        {
            state.CurrentDirection = state.NextDirection;
        }

        var head = state.SnakeBody[0];
        var nextHead = MoveHead(head, state.CurrentDirection);

        // Check bounds collision
        if (!state.Board.IsInBounds(nextHead.X, nextHead.Y))
        {
            state.IsGameOver = true;
            state.StepCount++;
            return;
        }

        // Check self-collision (body except tail)
        var board = state.Board;
        if (state.SnakeBody.Count > 1)
        {
            for (int i = 1; i < state.SnakeBody.Count; i++)
            {
                if (nextHead.X == state.SnakeBody[i].X && nextHead.Y == state.SnakeBody[i].Y)
                {
                    state.IsGameOver = true;
                    state.StepCount++;
                    return;
                }
            }
        }

        // Check apple collision
        var appleEaten = state.ApplePosition != null &&
                        nextHead.X == state.ApplePosition.X &&
                        nextHead.Y == state.ApplePosition.Y;

        // Clear the old snake from the board
        board.Set(head.X, head.Y, SnakeMark.Empty);
        for (int i = 1; i < state.SnakeBody.Count; i++)
        {
            board.Set(state.SnakeBody[i].X, state.SnakeBody[i].Y, SnakeMark.Empty);
        }

        // Place the new head
        board.Set(nextHead.X, nextHead.Y, SnakeMark.SnakeHead);

        // Update snake body
        var newBody = state.SnakeBody.Insert(0, nextHead);

        if (!appleEaten && state.SnakeBody.Count > 0)
        {
            // Remove the tail if apple wasn't eaten
            var tail = newBody[^1];
            newBody = newBody.RemoveAt(newBody.Count - 1);
            board.Set(tail.X, tail.Y, SnakeMark.Empty);
        }

        // Update board with new body
        for (int i = 1; i < newBody.Count; i++)
        {
            board.Set(newBody[i].X, newBody[i].Y, SnakeMark.SnakeBody);
        }

        state.SnakeBody = newBody;

        // Handle apple eaten
        if (appleEaten)
        {
            state.ApplesEaten++;
            state.ApplePosition = SpawnNewApple(board, state.SnakeBody);

            if (state.ApplePosition != null)
            {
                board.Set(state.ApplePosition.X, state.ApplePosition.Y, SnakeMark.Apple);
            }
        }

        state.StepCount++;
    }

    /// <summary>
    /// Spawns a new apple at a random empty location on the board.
    /// </summary>
    private static SnakeSegment? SpawnNewApple(SnakeBoard board, ImmutableList<SnakeSegment> snakeBody)
    {
        var emptySpots = new List<SnakeSegment>();

        for (int y = 0; y < board.Height; y++)
        {
            for (int x = 0; x < board.Width; x++)
            {
                if (board.Get(x, y) == SnakeMark.Empty)
                {
                    emptySpots.Add(new SnakeSegment(x, y));
                }
            }
        }

        if (emptySpots.Count == 0)
        {
            return null;
        }

        // Simple pseudo-random selection based on tick count
        var index = (int)(DateTime.UtcNow.Ticks % emptySpots.Count);
        return emptySpots[index];
    }

    /// <summary>
    /// Moves the head in the given direction.
    /// </summary>
    private static SnakeSegment MoveHead(SnakeSegment head, SnakeDirection direction)
    {
        return direction switch
        {
            SnakeDirection.Up => new(head.X, head.Y - 1),
            SnakeDirection.Down => new(head.X, head.Y + 1),
            SnakeDirection.Left => new(head.X - 1, head.Y),
            SnakeDirection.Right => new(head.X + 1, head.Y),
            _ => head
        };
    }

    /// <summary>
    /// Parses a direction command string.
    /// </summary>
    private static SnakeDirection ParseDirection(string command)
    {
        return command?.ToUpperInvariant().Trim() switch
        {
            "UP" => SnakeDirection.Up,
            "DOWN" => SnakeDirection.Down,
            "LEFT" => SnakeDirection.Left,
            "RIGHT" => SnakeDirection.Right,
            _ => SnakeDirection.None
        };
    }

    /// <summary>
    /// Checks if two directions are opposite (e.g., Up vs Down).
    /// Prevents the snake from reversing into itself.
    /// </summary>
    private static bool IsOppositeDirection(SnakeDirection current, SnakeDirection next)
    {
        return (current == SnakeDirection.Up && next == SnakeDirection.Down) ||
               (current == SnakeDirection.Down && next == SnakeDirection.Up) ||
               (current == SnakeDirection.Left && next == SnakeDirection.Right) ||
               (current == SnakeDirection.Right && next == SnakeDirection.Left);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await StopAsync();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
