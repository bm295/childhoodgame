namespace ChildhoodGame.Core.Strategy.TicTacToe;

public interface ITicTacToeMoveStrategy
{
    string Name { get; }

    TicTacToeMove? SelectMove(TicTacToeGameState state);
}

public sealed class TicTacToeAutoWinActionStrategy : IActionSelectionStrategy<TicTacToeGameState>
{
    private readonly TicTacToeDifficulty difficulty;
    private readonly ITicTacToeMoveStrategy moveStrategy;

    public TicTacToeAutoWinActionStrategy(
        TicTacToeDifficulty difficulty,
        ITicTacToeMoveStrategy? moveStrategy = null)
    {
        this.difficulty = difficulty;
        this.moveStrategy = moveStrategy ?? new OptimalTicTacToeMoveStrategy();
    }

    public IReadOnlyList<string> SelectActions(TicTacToeGameState state)
    {
        if (!state.DifficultySelected)
        {
            return new[] { TicTacToeInput.SelectDifficulty(difficulty) };
        }

        if (state.IsComplete)
        {
            return Array.Empty<string>();
        }

        var nextMove = moveStrategy.SelectMove(state);
        return nextMove is null
            ? Array.Empty<string>()
            : new[] { TicTacToeInput.PlayMove(nextMove.Value.Position) };
    }
}

public sealed class OptimalTicTacToeMoveStrategy : ITicTacToeMoveStrategy
{
    private static readonly int[] PreferredMoveOrder = { 5, 1, 3, 7, 9, 2, 4, 6, 8 };

    public string Name => "Minimax optimal solver";

    public TicTacToeMove? SelectMove(TicTacToeGameState state)
    {
        if (state.IsComplete)
        {
            return null;
        }

        var bestScore = int.MinValue;
        int? bestPosition = null;

        foreach (var position in OrderedEmptyPositions(state.Board))
        {
            var board = state.Board.PlaceMark(position, state.PlayerMark);
            var score = ScoreBoard(
                board,
                state.ComputerMark,
                state.PlayerMark,
                state.ComputerMark,
                depth: 1);

            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = position;
            }
        }

        return bestPosition is null ? null : new TicTacToeMove(bestPosition.Value);
    }

    private static int ScoreBoard(
        TicTacToeBoard board,
        TicTacToeMark turn,
        TicTacToeMark maximizingMark,
        TicTacToeMark minimizingMark,
        int depth)
    {
        var winner = board.GetWinner();
        if (winner == maximizingMark)
        {
            return 10 - depth;
        }

        if (winner == minimizingMark)
        {
            return depth - 10;
        }

        if (board.EmptyPositions.Count == 0)
        {
            return 0;
        }

        var nextTurn = turn == maximizingMark ? minimizingMark : maximizingMark;
        var scores = OrderedEmptyPositions(board)
            .Select(position => ScoreBoard(board.PlaceMark(position, turn), nextTurn, maximizingMark, minimizingMark, depth + 1));

        return turn == maximizingMark ? scores.Max() : scores.Min();
    }

    private static IEnumerable<int> OrderedEmptyPositions(TicTacToeBoard board) =>
        PreferredMoveOrder.Where(board.IsEmpty);
}

public sealed class TacticalTicTacToeMoveStrategy : ITicTacToeMoveStrategy
{
    public string Name => "Win-block tactical solver";

    public TicTacToeMove? SelectMove(TicTacToeGameState state)
    {
        if (state.IsComplete)
        {
            return null;
        }

        return FindCompletingMove(state.Board, state.PlayerMark)
            ?? FindCompletingMove(state.Board, state.ComputerMark)
            ?? new FirstAvailableTicTacToeMoveStrategy().SelectMove(state);
    }

    private static TicTacToeMove? FindCompletingMove(TicTacToeBoard board, TicTacToeMark mark)
    {
        foreach (var position in board.EmptyPositions)
        {
            if (board.PlaceMark(position, mark).GetWinner() == mark)
            {
                return new TicTacToeMove(position);
            }
        }

        return null;
    }
}

public sealed class FirstAvailableTicTacToeMoveStrategy : ITicTacToeMoveStrategy
{
    public string Name => "First available square";

    public TicTacToeMove? SelectMove(TicTacToeGameState state)
    {
        if (state.IsComplete)
        {
            return null;
        }

        var position = state.Board.EmptyPositions.FirstOrDefault();
        return position == 0 ? null : new TicTacToeMove(position);
    }
}
