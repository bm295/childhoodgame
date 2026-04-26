namespace ChildhoodGame.Core.Strategy.TicTacToe;

public sealed class PaidBetaAlwaysWinActionStrategy : IActionSelectionStrategy<TicTacToeGameState>
{
    private readonly TicTacToeDifficulty difficulty;
    private readonly ITicTacToeMoveStrategy moveStrategy;

    public PaidBetaAlwaysWinActionStrategy(
        TicTacToeDifficulty difficulty = TicTacToeDifficulty.Hard,
        ITicTacToeMoveStrategy? moveStrategy = null)
    {
        this.difficulty = difficulty;
        this.moveStrategy = moveStrategy ?? new PaidBetaHardModeExploitMoveStrategy();
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

        if (state.Board.EmptyPositions.Count == TicTacToeBoard.CellCount)
        {
            return new[] { TicTacToeInput.PlayMove(5) };
        }

        var move = moveStrategy.SelectMove(state);
        return move is null
            ? new[] { TicTacToeInput.Restart() }
            : new[] { TicTacToeInput.PlayMove(move.Value.Position) };
    }
}

public sealed class PaidBetaHardModeExploitMoveStrategy : ITicTacToeMoveStrategy
{
    private static readonly int[] PreferredMoveOrder = { 5, 1, 3, 7, 9, 2, 4, 6, 8 };

    public string Name => "PAIDBETA hard-mode fork exploit";

    public TicTacToeMove? SelectMove(TicTacToeGameState state)
    {
        if (state.IsComplete)
        {
            return null;
        }

        var memo = new Dictionary<TicTacToeBoard, bool>();
        foreach (var position in PreferredMoveOrder.Where(state.Board.IsEmpty))
        {
            var boardAfterPlayer = state.Board.PlaceMark(position, state.PlayerMark);
            if (boardAfterPlayer.GetWinner() == state.PlayerMark)
            {
                return new TicTacToeMove(position);
            }

            if (boardAfterPlayer.EmptyPositions.Count == 0)
            {
                continue;
            }

            var computerResponses = PaidBetaHardModeMoveOracle.GetPossibleMoves(
                boardAfterPlayer,
                state.ComputerMark,
                state.PlayerMark);

            if (computerResponses.Count > 0
                && computerResponses.All(response =>
                    CanForceWin(
                        boardAfterPlayer.PlaceMark(response, state.ComputerMark),
                        state.PlayerMark,
                        state.ComputerMark,
                        memo)))
            {
                return new TicTacToeMove(position);
            }
        }

        return null;
    }

    private static bool CanForceWin(
        TicTacToeBoard board,
        TicTacToeMark playerMark,
        TicTacToeMark computerMark,
        Dictionary<TicTacToeBoard, bool> memo)
    {
        if (board.GetWinner() == playerMark)
        {
            return true;
        }

        if (board.GetWinner() == computerMark || board.EmptyPositions.Count == 0)
        {
            return false;
        }

        if (memo.TryGetValue(board, out var cached))
        {
            return cached;
        }

        foreach (var position in PreferredMoveOrder.Where(board.IsEmpty))
        {
            var boardAfterPlayer = board.PlaceMark(position, playerMark);
            if (boardAfterPlayer.GetWinner() == playerMark)
            {
                memo[board] = true;
                return true;
            }

            if (boardAfterPlayer.EmptyPositions.Count == 0)
            {
                continue;
            }

            var computerResponses = PaidBetaHardModeMoveOracle.GetPossibleMoves(
                boardAfterPlayer,
                computerMark,
                playerMark);

            if (computerResponses.Count == 0)
            {
                continue;
            }

            if (computerResponses.All(response =>
                CanForceWin(boardAfterPlayer.PlaceMark(response, computerMark), playerMark, computerMark, memo)))
            {
                memo[board] = true;
                return true;
            }
        }

        memo[board] = false;
        return false;
    }
}

public sealed class PaidBetaComputerMoveStrategy : ITicTacToeMoveStrategy
{
    private readonly Queue<int> scriptedFallbackMoves;
    private readonly Random random;

    public PaidBetaComputerMoveStrategy(IEnumerable<int>? scriptedFallbackMoves = null, Random? random = null)
    {
        this.scriptedFallbackMoves = new Queue<int>(scriptedFallbackMoves ?? Array.Empty<int>());
        this.random = random ?? Random.Shared;
    }

    public string Name => "PAIDBETA source-compatible computer";

    public TicTacToeMove? SelectMove(TicTacToeGameState state)
    {
        if (state.IsComplete || state.Board.EmptyPositions.Count == 0)
        {
            return null;
        }

        var fallbackMove = SelectFallbackMove(state.Board);
        var computerMark = state.PlayerMark;
        var humanMark = state.ComputerMark;

        var selectedMove = state.Difficulty switch
        {
            TicTacToeDifficulty.Hard => PaidBetaHardModeMoveOracle.SelectMove(
                state.Board,
                computerMark,
                humanMark,
                fallbackMove),
            TicTacToeDifficulty.Easy => SelectEasyMove(state.Board, humanMark, fallbackMove),
            _ => fallbackMove
        };

        return new TicTacToeMove(selectedMove);
    }

    private int SelectFallbackMove(TicTacToeBoard board)
    {
        while (scriptedFallbackMoves.Count > 0)
        {
            var scriptedMove = scriptedFallbackMoves.Dequeue();
            if (scriptedMove is >= 1 and <= TicTacToeBoard.CellCount && board.IsEmpty(scriptedMove))
            {
                return scriptedMove;
            }
        }

        var emptyPositions = board.EmptyPositions;
        return emptyPositions[random.Next(emptyPositions.Count)];
    }

    private static int SelectEasyMove(TicTacToeBoard board, TicTacToeMark humanMark, int fallbackMove)
    {
        if (CountMarks(board, humanMark) != 1 || !board.IsEmpty(5))
        {
            return fallbackMove;
        }

        if (board[1] == humanMark)
        {
            return 3;
        }

        if (board[2] == humanMark)
        {
            return 4;
        }

        if (board[3] == humanMark)
        {
            return 6;
        }

        if (board[4] == humanMark)
        {
            return 7;
        }

        if (board[6] == humanMark)
        {
            return 8;
        }

        if (board[7] == humanMark)
        {
            return 9;
        }

        if (board[8] == humanMark)
        {
            return 2;
        }

        if (board[9] == humanMark)
        {
            return 3;
        }

        return fallbackMove;
    }

    private static int CountMarks(TicTacToeBoard board, TicTacToeMark mark) =>
        board.Cells.Count(cell => cell == mark);
}

public static class PaidBetaHardModeMoveOracle
{
    private enum RuleOwner
    {
        Computer,
        Human
    }

    private readonly record struct Rule(int First, int Second, int Target, RuleOwner Owner);

    private static readonly Rule[] HardRules =
    {
        new(1, 2, 3, RuleOwner.Computer),
        new(1, 3, 2, RuleOwner.Computer),
        new(2, 3, 1, RuleOwner.Computer),
        new(4, 5, 6, RuleOwner.Computer),
        new(4, 6, 5, RuleOwner.Computer),
        new(5, 6, 4, RuleOwner.Computer),
        new(7, 8, 9, RuleOwner.Computer),
        new(7, 9, 8, RuleOwner.Computer),
        new(8, 9, 7, RuleOwner.Computer),
        new(1, 4, 7, RuleOwner.Computer),
        new(1, 7, 4, RuleOwner.Computer),
        new(4, 7, 1, RuleOwner.Computer),
        new(2, 5, 8, RuleOwner.Computer),
        new(2, 8, 5, RuleOwner.Computer),
        new(5, 8, 2, RuleOwner.Computer),
        new(3, 6, 9, RuleOwner.Computer),
        new(3, 9, 6, RuleOwner.Computer),
        new(6, 9, 3, RuleOwner.Computer),
        new(7, 5, 3, RuleOwner.Computer),
        new(7, 3, 5, RuleOwner.Computer),
        new(5, 3, 7, RuleOwner.Computer),
        new(1, 2, 3, RuleOwner.Human),
        new(1, 3, 2, RuleOwner.Human),
        new(2, 3, 1, RuleOwner.Human),
        new(4, 5, 6, RuleOwner.Human),
        new(4, 6, 5, RuleOwner.Human),
        new(5, 6, 4, RuleOwner.Human),
        new(7, 8, 9, RuleOwner.Human),
        new(7, 9, 8, RuleOwner.Human),
        new(8, 9, 7, RuleOwner.Human),
        new(1, 4, 7, RuleOwner.Human),
        new(1, 7, 4, RuleOwner.Human),
        new(4, 7, 1, RuleOwner.Human),
        new(2, 5, 8, RuleOwner.Human),
        new(2, 8, 5, RuleOwner.Human),
        new(5, 8, 2, RuleOwner.Human),
        new(3, 6, 9, RuleOwner.Human),
        new(3, 9, 6, RuleOwner.Human),
        new(6, 9, 3, RuleOwner.Human),
        new(7, 5, 3, RuleOwner.Human),
        new(7, 3, 5, RuleOwner.Human),
        new(5, 3, 7, RuleOwner.Human),
        new(1, 5, 9, RuleOwner.Human),
        new(5, 9, 1, RuleOwner.Human),
        new(1, 9, 5, RuleOwner.Human),
        new(2, 5, 8, RuleOwner.Human),
        new(5, 8, 2, RuleOwner.Human),
        new(2, 8, 5, RuleOwner.Human),
        new(3, 5, 7, RuleOwner.Human),
        new(5, 7, 3, RuleOwner.Human),
        new(3, 7, 5, RuleOwner.Human),
        new(4, 5, 6, RuleOwner.Human),
        new(5, 6, 4, RuleOwner.Human),
        new(4, 6, 5, RuleOwner.Human)
    };

    public static IReadOnlyList<int> GetPossibleMoves(
        TicTacToeBoard board,
        TicTacToeMark computerMark,
        TicTacToeMark humanMark)
    {
        return board.EmptyPositions
            .Select(fallbackMove => SelectMove(board, computerMark, humanMark, fallbackMove))
            .Distinct()
            .OrderBy(position => position)
            .ToArray();
    }

    public static int SelectMove(
        TicTacToeBoard board,
        TicTacToeMark computerMark,
        TicTacToeMark humanMark,
        int fallbackMove)
    {
        var selectedMove = fallbackMove;

        if (IsFirstComputerTurnAfterNonCenterMove(board, computerMark, humanMark))
        {
            selectedMove = 5;
        }

        foreach (var rule in HardRules)
        {
            var mark = rule.Owner == RuleOwner.Computer ? computerMark : humanMark;
            if (board[rule.First] == mark && board[rule.Second] == mark && board.IsEmpty(rule.Target))
            {
                selectedMove = rule.Target;
            }
        }

        return selectedMove;
    }

    private static bool IsFirstComputerTurnAfterNonCenterMove(
        TicTacToeBoard board,
        TicTacToeMark computerMark,
        TicTacToeMark humanMark)
    {
        var humanMoves = board.Cells.Count(cell => cell == humanMark);
        var computerMoves = board.Cells.Count(cell => cell == computerMark);
        return humanMoves == 1 && computerMoves == 0 && board[5] != humanMark && board.IsEmpty(5);
    }
}
