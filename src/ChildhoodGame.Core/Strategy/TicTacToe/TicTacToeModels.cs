namespace ChildhoodGame.Core.Strategy.TicTacToe;

public enum TicTacToeMark
{
    Empty,
    O,
    X
}

public enum TicTacToeDifficulty
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}

public enum TicTacToeOutcome
{
    InProgress,
    PlayerWin,
    ComputerWin,
    Draw
}

public readonly record struct TicTacToeMove(int Position);

public sealed record TicTacToeGameState(
    TicTacToeBoard Board,
    TicTacToeMark PlayerMark,
    TicTacToeMark ComputerMark,
    bool DifficultySelected,
    TicTacToeDifficulty Difficulty,
    int RestartCount = 0)
{
    public static TicTacToeGameState New(TicTacToeDifficulty difficulty = TicTacToeDifficulty.Easy) =>
        new(new TicTacToeBoard(), TicTacToeMark.O, TicTacToeMark.X, false, difficulty);

    public TicTacToeOutcome Outcome => Board.GetOutcome(PlayerMark, ComputerMark);

    public bool IsComplete => Outcome != TicTacToeOutcome.InProgress;
}

public sealed class TicTacToeBoard : IEquatable<TicTacToeBoard>
{
    public const int CellCount = 9;

    private static readonly int[][] WinningLines =
    {
        new[] { 1, 2, 3 },
        new[] { 4, 5, 6 },
        new[] { 7, 8, 9 },
        new[] { 1, 4, 7 },
        new[] { 2, 5, 8 },
        new[] { 3, 6, 9 },
        new[] { 1, 5, 9 },
        new[] { 3, 5, 7 }
    };

    private readonly TicTacToeMark[] cells;

    public TicTacToeBoard()
        : this(Enumerable.Repeat(TicTacToeMark.Empty, CellCount))
    {
    }

    public TicTacToeBoard(IEnumerable<TicTacToeMark> cells)
    {
        var copy = cells.ToArray();
        if (copy.Length != CellCount)
        {
            throw new ArgumentException($"A TicTacToe board must contain {CellCount} cells.", nameof(cells));
        }

        this.cells = copy;
    }

    public IReadOnlyList<TicTacToeMark> Cells => Array.AsReadOnly(cells);

    public TicTacToeMark this[int position] => cells[ToIndex(position)];

    public IReadOnlyList<int> EmptyPositions =>
        Enumerable.Range(1, CellCount).Where(IsEmpty).ToArray();

    public bool IsEmpty(int position) => this[position] == TicTacToeMark.Empty;

    public TicTacToeBoard PlaceMark(int position, TicTacToeMark mark)
    {
        if (mark == TicTacToeMark.Empty)
        {
            throw new ArgumentException("A move must place O or X.", nameof(mark));
        }

        var index = ToIndex(position);
        if (cells[index] != TicTacToeMark.Empty)
        {
            throw new InvalidOperationException($"Board position {position} is already occupied.");
        }

        var next = cells.ToArray();
        next[index] = mark;
        return new TicTacToeBoard(next);
    }

    public TicTacToeMark? GetWinner()
    {
        foreach (var line in WinningLines)
        {
            var first = this[line[0]];
            if (first == TicTacToeMark.Empty)
            {
                continue;
            }

            if (this[line[1]] == first && this[line[2]] == first)
            {
                return first;
            }
        }

        return null;
    }

    public TicTacToeOutcome GetOutcome(TicTacToeMark playerMark, TicTacToeMark computerMark)
    {
        var winner = GetWinner();
        if (winner == playerMark)
        {
            return TicTacToeOutcome.PlayerWin;
        }

        if (winner == computerMark)
        {
            return TicTacToeOutcome.ComputerWin;
        }

        return EmptyPositions.Count == 0 ? TicTacToeOutcome.Draw : TicTacToeOutcome.InProgress;
    }

    public override string ToString() =>
        string.Concat(cells.Select(cell => cell switch
        {
            TicTacToeMark.O => "O",
            TicTacToeMark.X => "X",
            _ => "."
        }));

    public bool Equals(TicTacToeBoard? other) =>
        other is not null && cells.SequenceEqual(other.cells);

    public override bool Equals(object? obj) => Equals(obj as TicTacToeBoard);

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        foreach (var cell in cells)
        {
            hashCode.Add(cell);
        }

        return hashCode.ToHashCode();
    }

    private static int ToIndex(int position)
    {
        if (position is < 1 or > CellCount)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Board positions are numbered 1 through 9.");
        }

        return position - 1;
    }
}
