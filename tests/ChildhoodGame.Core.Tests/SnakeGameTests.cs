using System.Collections.Immutable;
using ChildhoodGame.Core.Strategy.Snake;
using Xunit;

namespace ChildhoodGame.Core.Tests;

public sealed class SnakeGameStateTests
{
    [Fact]
    public void SnakeGameState_InitializesWithCorrectDefaults()
    {
        // Arrange
        var board = new SnakeBoard(20, 20);
        var snakeBody = ImmutableList.Create(
            new SnakeSegment(10, 10),
            new SnakeSegment(9, 10),
            new SnakeSegment(8, 10)
        );
        var apple = new SnakeSegment(15, 15);

        // Act
        var state = new SnakeGameState(board, snakeBody, apple);

        // Assert
        Assert.Equal(3, state.SnakeBody.Count);
        Assert.Equal(apple, state.ApplePosition);
        Assert.Equal(SnakeDirection.Right, state.CurrentDirection);
        Assert.Equal(SnakeDirection.Right, state.NextDirection);
        Assert.False(state.IsGameOver);
        Assert.Equal(0, state.ApplesEaten);
        Assert.Equal(0, state.StepCount);
    }

    [Fact]
    public void SnakeGameState_CanClone()
    {
        // Arrange
        var board = new SnakeBoard(20, 20);
        var snakeBody = ImmutableList.Create(new SnakeSegment(10, 10), new SnakeSegment(9, 10));
        var apple = new SnakeSegment(15, 15);
        var state = new SnakeGameState(board, snakeBody, apple)
        {
            ApplesEaten = 5,
            StepCount = 50
        };

        // Act
        var cloned = state.Clone();

        // Assert
        Assert.Equal(state.SnakeBody.Count, cloned.SnakeBody.Count);
        Assert.Equal(state.ApplesEaten, cloned.ApplesEaten);
        Assert.Equal(state.StepCount, cloned.StepCount);
        Assert.NotSame(state.Board, cloned.Board); // Different board object
    }
}

public sealed class SnakeBoardTests
{
    [Fact]
    public void SnakeBoard_InitializesWithCorrectDimensions()
    {
        // Act
        var board = new SnakeBoard(20, 15);

        // Assert
        Assert.Equal(20, board.Width);
        Assert.Equal(15, board.Height);
    }

    [Fact]
    public void SnakeBoard_GetAndSetMarks()
    {
        // Arrange
        var board = new SnakeBoard(10, 10);

        // Act
        board.Set(5, 5, SnakeMark.Apple);
        var mark = board.Get(5, 5);

        // Assert
        Assert.Equal(SnakeMark.Apple, mark);
    }

    [Fact]
    public void SnakeBoard_IsInBounds_ReturnsCorrectValues()
    {
        // Arrange
        var board = new SnakeBoard(10, 10);

        // Assert
        Assert.True(board.IsInBounds(0, 0));
        Assert.True(board.IsInBounds(9, 9));
        Assert.False(board.IsInBounds(-1, 0));
        Assert.False(board.IsInBounds(10, 0));
        Assert.False(board.IsInBounds(0, -1));
        Assert.False(board.IsInBounds(0, 10));
    }

    [Fact]
    public void SnakeBoard_Clear_RemovesAllMarks()
    {
        // Arrange
        var board = new SnakeBoard(5, 5);
        board.Set(0, 0, SnakeMark.Apple);
        board.Set(2, 2, SnakeMark.SnakeHead);
        board.Set(4, 4, SnakeMark.SnakeBody);

        // Act
        board.Clear();

        // Assert
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                Assert.Equal(SnakeMark.Empty, board.Get(x, y));
            }
        }
    }

    [Fact]
    public void SnakeBoard_Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new SnakeBoard(5, 5);
        original.Set(0, 0, SnakeMark.Apple);

        // Act
        var clone = original.Clone();
        clone.Set(0, 0, SnakeMark.Empty);
        clone.Set(1, 1, SnakeMark.SnakeHead);

        // Assert
        Assert.Equal(SnakeMark.Apple, original.Get(0, 0)); // Original unchanged
        Assert.Equal(SnakeMark.Empty, clone.Get(0, 0));
        Assert.Equal(SnakeMark.SnakeHead, clone.Get(1, 1));
    }
}

public sealed class SnakeScreenReaderTests
{
    [Fact]
    public void SnakeScreenReader_DetectsAppleAndHeadDirection()
    {
        const int captureWidth = 40;
        const int captureHeight = 40;
        const int boardWidth = 4;
        const int boardHeight = 4;
        var backgroundColor = (255 << 24) | (20 << 16) | (150 << 8) | 20;
        var headColor = (255 << 24) | (220 << 16) | (220 << 8) | 20;
        var bodyColor = headColor;
        var appleColor = (255 << 24) | (220 << 16) | (30 << 8) | 30;
        var pixels = Enumerable.Repeat(backgroundColor, captureWidth * captureHeight).ToArray();

        void FillCell(int cellX, int cellY, int color)
        {
            var cellWidth = captureWidth / boardWidth;
            var cellHeight = captureHeight / boardHeight;
            for (var y = cellY * cellHeight; y < (cellY + 1) * cellHeight; y++)
            {
                for (var x = cellX * cellWidth; x < (cellX + 1) * cellWidth; x++)
                {
                    pixels[y * captureWidth + x] = color;
                }
            }
        }

        FillCell(1, 1, headColor);
        FillCell(0, 1, bodyColor);
        FillCell(2, 1, appleColor);

        var capture = new GameWindowCapture(captureWidth, captureHeight, pixels);
        var state = new SnakeScreenReader().ReadGameState(capture, boardWidth, boardHeight);

        Assert.Equal(new SnakeSegment(1, 1), state.SnakeBody[0]);
        Assert.Equal(new SnakeSegment(2, 1), state.ApplePosition);
        Assert.Equal(SnakeDirection.Right, state.CurrentDirection);
    }
}

public sealed class MockedSnakeRuntimeTests
{
    [Fact]
    public async Task MockedSnakeRuntime_StartAndStop()
    {
        // Arrange
        var runtime = new MockedSnakeRuntime();
        var package = new GamePackage(
            GameRootPath: Environment.CurrentDirectory,
            GameExecutablePath: "MOCK.EXE",
            DosConfigPath: "DOSBOX.CONF",
            RuntimeConfig: new DosRuntimeConfig("embedded", null, null, null),
            SaveStatePath: null,
            LoadStatePath: null);

        // Act
        Assert.False(runtime.IsRunning);
        await runtime.StartAsync(package);
        Assert.True(runtime.IsRunning);
        await runtime.StopAsync();
        Assert.False(runtime.IsRunning);
    }

    [Fact]
    public async Task MockedSnakeRuntime_ReadStateAsync_ReturnsValidState()
    {
        // Arrange
        var runtime = new MockedSnakeRuntime();
        var package = new GamePackage(
            GameRootPath: Environment.CurrentDirectory,
            GameExecutablePath: "MOCK.EXE",
            DosConfigPath: "DOSBOX.CONF",
            RuntimeConfig: new DosRuntimeConfig("embedded", null, null, null),
            SaveStatePath: null,
            LoadStatePath: null);

        await runtime.StartAsync(package);

        // Act
        var state = await runtime.ReadStateAsync();

        // Assert
        Assert.NotNull(state);
        Assert.True(state.SnakeBody.Count >= 3, "Snake should have at least 3 segments");
        Assert.NotNull(state.ApplePosition);
        Assert.False(state.IsGameOver);
        Assert.Equal(0, state.ApplesEaten);

        await runtime.StopAsync();
    }

    [Fact]
    public async Task MockedSnakeRuntime_SendInput_UpdatesDirection()
    {
        // Arrange
        var runtime = new MockedSnakeRuntime();
        var package = new GamePackage(
            GameRootPath: Environment.CurrentDirectory,
            GameExecutablePath: "MOCK.EXE",
            DosConfigPath: "DOSBOX.CONF",
            RuntimeConfig: new DosRuntimeConfig("embedded", null, null, null),
            SaveStatePath: null,
            LoadStatePath: null);

        await runtime.StartAsync(package);
        var initialState = await runtime.ReadStateAsync();

        // Act - Move left
        await runtime.SendInputAsync("LEFT");
        var stateAfterMove = await runtime.ReadStateAsync();

        // Assert
        Assert.True(stateAfterMove.StepCount > initialState.StepCount);
        // Snake should have moved (position changed or state updated)

        await runtime.StopAsync();
    }

    [Fact]
    public async Task MockedSnakeRuntime_InvalidDirection_IgnoresInput()
    {
        // Arrange
        var runtime = new MockedSnakeRuntime();
        var package = new GamePackage(
            GameRootPath: Environment.CurrentDirectory,
            GameExecutablePath: "MOCK.EXE",
            DosConfigPath: "DOSBOX.CONF",
            RuntimeConfig: new DosRuntimeConfig("embedded", null, null, null),
            SaveStatePath: null,
            LoadStatePath: null);

        await runtime.StartAsync(package);

        // Act - Send invalid input
        await runtime.SendInputAsync("INVALID");
        var state = await runtime.ReadStateAsync();

        // Assert - Should not crash, game continues
        Assert.NotNull(state);
        Assert.False(state.IsGameOver);

        await runtime.StopAsync();
    }
}

public sealed class SnakeWinConditionDetectorTests
{
    [Fact]
    public void SnakeWinConditionDetector_NotSatisfiedWhenApplesLessThanRequired()
    {
        // Arrange
        var detector = new SnakeWinConditionDetector(applesRequired: 10);
        var board = new SnakeBoard(20, 20);
        var snakeBody = ImmutableList.Create(new SnakeSegment(10, 10));
        var state = new SnakeGameState(board, snakeBody)
        {
            ApplesEaten = 5,
            IsGameOver = false
        };

        // Act
        var isSatisfied = detector.IsSatisfied(state);

        // Assert
        Assert.False(isSatisfied);
    }

    [Fact]
    public void SnakeWinConditionDetector_SatisfiedWhenApplesReached()
    {
        // Arrange
        var detector = new SnakeWinConditionDetector(applesRequired: 10);
        var board = new SnakeBoard(20, 20);
        var snakeBody = ImmutableList.Create(new SnakeSegment(10, 10));
        var state = new SnakeGameState(board, snakeBody)
        {
            ApplesEaten = 10,
            IsGameOver = false
        };

        // Act
        var isSatisfied = detector.IsSatisfied(state);

        // Assert
        Assert.True(isSatisfied);
    }

    [Fact]
    public void SnakeWinConditionDetector_NotSatisfiedWhenGameOver()
    {
        // Arrange
        var detector = new SnakeWinConditionDetector(applesRequired: 10);
        var board = new SnakeBoard(20, 20);
        var snakeBody = ImmutableList.Create(new SnakeSegment(10, 10));
        var state = new SnakeGameState(board, snakeBody)
        {
            ApplesEaten = 10,
            IsGameOver = true // Game over despite reaching target
        };

        // Act
        var isSatisfied = detector.IsSatisfied(state);

        // Assert
        Assert.False(isSatisfied);
    }

    [Fact]
    public void SnakeWinConditionDetector_HasCorrectName()
    {
        // Arrange & Act
        var detector = new SnakeWinConditionDetector();

        // Assert
        Assert.Equal("SnakeWinCondition", detector.Name);
    }
}

public sealed class SnakeGreedyPathStrategyTests
{
    [Fact]
    public void SnakeGreedyPathStrategy_SelectsDirectionTowardApple()
    {
        // Arrange
        var board = new SnakeBoard(20, 20);
        var strategy = new SnakeGreedyPathStrategy();

        // Snake at (5, 5), apple at (10, 5) - should move right
        var snakeBody = ImmutableList.Create(
            new SnakeSegment(5, 5),
            new SnakeSegment(4, 5),
            new SnakeSegment(3, 5)
        );
        var state = new SnakeGameState(board, snakeBody, new SnakeSegment(10, 5));

        // Place snake and apple on board for collision detection
        board.Set(5, 5, SnakeMark.SnakeHead);
        board.Set(4, 5, SnakeMark.SnakeBody);
        board.Set(3, 5, SnakeMark.SnakeBody);
        board.Set(10, 5, SnakeMark.Apple);

        // Act
        var direction = strategy.SelectNextDirection(state);

        // Assert
        Assert.Equal(SnakeDirection.Right, direction);
    }

    [Fact]
    public void SnakeGreedyPathStrategy_ContinuesForwardWhenAppleIsAheadAndOblique()
    {
        // Arrange
        var board = new SnakeBoard(20, 20);
        var strategy = new SnakeGreedyPathStrategy();

        // Snake is moving up with head at (5, 5), tail below.
        var snakeBody = ImmutableList.Create(
            new SnakeSegment(5, 5),
            new SnakeSegment(5, 6),
            new SnakeSegment(5, 7)
        );
        var apple = new SnakeSegment(7, 3);
        var state = new SnakeGameState(board, snakeBody, apple)
        {
            CurrentDirection = SnakeDirection.Up,
            NextDirection = SnakeDirection.Up
        };

        board.Set(5, 5, SnakeMark.SnakeHead);
        board.Set(5, 6, SnakeMark.SnakeBody);
        board.Set(5, 7, SnakeMark.SnakeBody);
        board.Set(7, 3, SnakeMark.Apple);

        // Act
        var direction = strategy.SelectNextDirection(state);

        // Assert
        Assert.Equal(SnakeDirection.Up, direction);
    }

    [Fact]
    public void SnakeGreedyPathStrategy_MovesVerticallyWhenHorizontalBlocked()
    {
        // Arrange
        var board = new SnakeBoard(20, 20);
        var strategy = new SnakeGreedyPathStrategy();

        // Snake at (5, 5), apple at (10, 10)
        // Block right with wall
        var snakeBody = ImmutableList.Create(
            new SnakeSegment(5, 5),
            new SnakeSegment(4, 5),
            new SnakeSegment(3, 5)
        );
        var state = new SnakeGameState(board, snakeBody, new SnakeSegment(10, 10));

        board.Set(5, 5, SnakeMark.SnakeHead);
        board.Set(4, 5, SnakeMark.SnakeBody);
        board.Set(3, 5, SnakeMark.SnakeBody);
        board.Set(6, 5, SnakeMark.Wall); // Block right movement
        board.Set(10, 10, SnakeMark.Apple);

        // Act
        var direction = strategy.SelectNextDirection(state);

        // Assert - Should move down or up toward apple, not right (blocked)
        Assert.True(direction == SnakeDirection.Down || direction == SnakeDirection.Up);
        Assert.NotEqual(SnakeDirection.Right, direction);
    }

    [Fact]
    public void SnakeGreedyPathStrategy_ReturnsDefaultWhenGameOver()
    {
        // Arrange
        var board = new SnakeBoard(20, 20);
        var strategy = new SnakeGreedyPathStrategy();
        var snakeBody = ImmutableList.Create(new SnakeSegment(5, 5));
        var state = new SnakeGameState(board, snakeBody, new SnakeSegment(10, 10))
        {
            IsGameOver = true
        };

        // Act
        var direction = strategy.SelectNextDirection(state);

        // Assert
        Assert.Equal(SnakeDirection.Right, direction); // Default
    }
}

public sealed class SnakeActionStrategyTests
{
    [Fact]
    public void SnakeActionStrategy_ConvertsDirectionToCommand()
    {
        // Arrange
        var moveStrategy = new SnakeGreedyPathStrategy();
        var actionStrategy = new SnakeActionStrategy(moveStrategy);
        var board = new SnakeBoard(20, 20);
        var snakeBody = ImmutableList.Create(
            new SnakeSegment(5, 5),
            new SnakeSegment(4, 5)
        );
        var state = new SnakeGameState(board, snakeBody, new SnakeSegment(10, 5));

        board.Set(5, 5, SnakeMark.SnakeHead);
        board.Set(4, 5, SnakeMark.SnakeBody);
        board.Set(10, 5, SnakeMark.Apple);

        // Act
        var actions = actionStrategy.SelectActions(state);

        // Assert
        Assert.True(actions.Count > 0);
        Assert.True(actions.Contains("UP") || actions.Contains("DOWN") || 
                     actions.Contains("LEFT") || actions.Contains("RIGHT"));
    }

    [Fact]
    public void SnakeActionStrategy_ReturnsEmptyWhenGameOver()
    {
        // Arrange
        var moveStrategy = new SnakeGreedyPathStrategy();
        var actionStrategy = new SnakeActionStrategy(moveStrategy);
        var board = new SnakeBoard(20, 20);
        var snakeBody = ImmutableList.Create(new SnakeSegment(5, 5));
        var state = new SnakeGameState(board, snakeBody, new SnakeSegment(10, 10))
        {
            IsGameOver = true
        };

        // Act
        var actions = actionStrategy.SelectActions(state);

        // Assert
        Assert.Empty(actions);
    }
}
