using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace ChildhoodGame.Core.Strategy.Snake;

/// <summary>
/// Represents what is at a position on the Snake game board.
/// </summary>
public enum SnakeMark
{
    Empty,
    SnakeHead,
    SnakeBody,
    Apple,
    Wall
}

/// <summary>
/// Represents the direction the snake is moving.
/// </summary>
public enum SnakeDirection
{
    Up,
    Down,
    Left,
    Right,
    None
}

/// <summary>
/// Represents the 2D Snake game board.
/// </summary>
public sealed class SnakeBoard
{
    private readonly SnakeMark[,] _grid;

    public int Width { get; }
    public int Height { get; }

    public SnakeBoard(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException("Board dimensions must be positive.");
        }

        Width = width;
        Height = height;
        _grid = new SnakeMark[height, width];
    }

    /// <summary>
    /// Creates a copy of this board.
    /// </summary>
    public SnakeBoard Clone()
    {
        var clone = new SnakeBoard(Width, Height);
        Array.Copy(_grid, clone._grid, _grid.Length);
        return clone;
    }

    /// <summary>
    /// Gets or sets the mark at the specified position.
    /// </summary>
    public SnakeMark Get(int x, int y)
    {
        if (!IsInBounds(x, y))
        {
            throw new ArgumentOutOfRangeException($"Position ({x}, {y}) is out of bounds.");
        }

        return _grid[y, x];
    }

    /// <summary>
    /// Sets the mark at the specified position.
    /// </summary>
    public void Set(int x, int y, SnakeMark mark)
    {
        if (!IsInBounds(x, y))
        {
            throw new ArgumentOutOfRangeException($"Position ({x}, {y}) is out of bounds.");
        }

        _grid[y, x] = mark;
    }

    /// <summary>
    /// Clears the board to all empty cells.
    /// </summary>
    public void Clear()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _grid[y, x] = SnakeMark.Empty;
            }
        }
    }

    /// <summary>
    /// Checks if a position is within board bounds.
    /// </summary>
    public bool IsInBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    /// <summary>
    /// Returns a string representation of the board for debugging.
    /// </summary>
    public override string ToString()
    {
        var lines = new System.Text.StringBuilder();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                lines.Append(GetMarkChar(_grid[y, x]));
            }

            if (y < Height - 1)
            {
                lines.AppendLine();
            }
        }

        return lines.ToString();
    }

    private static char GetMarkChar(SnakeMark mark) => mark switch
    {
        SnakeMark.Empty => '.',
        SnakeMark.SnakeHead => 'H',
        SnakeMark.SnakeBody => 'S',
        SnakeMark.Apple => 'A',
        SnakeMark.Wall => '#',
        _ => '?'
    };
}

/// <summary>
/// Represents a position on the snake (head or body segment).
/// </summary>
public sealed record SnakeSegment(int X, int Y);

/// <summary>
/// Represents the complete game state for Snake.
/// </summary>
public sealed class SnakeGameState
{
    public SnakeBoard Board { get; private set; }

    /// <summary>
    /// The snake's body, from head (index 0) to tail (last index).
    /// </summary>
    public ImmutableList<SnakeSegment> SnakeBody { get; set; }

    /// <summary>
    /// Current position of the apple.
    /// </summary>
    public SnakeSegment? ApplePosition { get; set; }

    /// <summary>
    /// The direction the snake is moving.
    /// </summary>
    public SnakeDirection CurrentDirection { get; set; }

    /// <summary>
    /// The next direction the snake will move (may differ from CurrentDirection if not yet processed).
    /// </summary>
    public SnakeDirection NextDirection { get; set; }

    /// <summary>
    /// Whether the game is over (snake died).
    /// </summary>
    public bool IsGameOver { get; set; }

    /// <summary>
    /// Number of apples eaten.
    /// </summary>
    public int ApplesEaten { get; set; }

    /// <summary>
    /// Number of steps taken.
    /// </summary>
    public int StepCount { get; set; }

    public SnakeGameState(SnakeBoard board, ImmutableList<SnakeSegment> snakeBody, SnakeSegment? applePosition = null)
    {
        Board = board ?? throw new ArgumentNullException(nameof(board));
        SnakeBody = snakeBody ?? throw new ArgumentNullException(nameof(snakeBody));
        ApplePosition = applePosition;
        CurrentDirection = SnakeDirection.Right;
        NextDirection = SnakeDirection.Right;
        IsGameOver = false;
        ApplesEaten = 0;
        StepCount = 0;
    }

    /// <summary>
    /// Creates a copy of this game state.
    /// </summary>
    public SnakeGameState Clone()
    {
        return new SnakeGameState(Board.Clone(), SnakeBody, ApplePosition)
        {
            CurrentDirection = CurrentDirection,
            NextDirection = NextDirection,
            IsGameOver = IsGameOver,
            ApplesEaten = ApplesEaten,
            StepCount = StepCount
        };
    }

    /// <summary>
    /// Returns a string representation of the current game state for debugging.
    /// </summary>
    public override string ToString()
    {
        return $"SnakeGameState(Length={SnakeBody.Count}, Apples={ApplesEaten}, Steps={StepCount}, GameOver={IsGameOver}, Dir={CurrentDirection})";
    }
}
