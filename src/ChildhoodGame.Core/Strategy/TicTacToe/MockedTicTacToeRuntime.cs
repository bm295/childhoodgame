namespace ChildhoodGame.Core.Strategy.TicTacToe;

public sealed class MockedTicTacToeRuntime : IGameStateRuntime<TicTacToeGameState>
{
    private readonly Func<TicTacToeDifficulty, ITicTacToeMoveStrategy> computerStrategyFactory;
    private TicTacToeGameState state;

    public MockedTicTacToeRuntime(
        TicTacToeGameState? initialState = null,
        Func<TicTacToeDifficulty, ITicTacToeMoveStrategy>? computerStrategyFactory = null)
    {
        state = initialState ?? TicTacToeGameState.New();
        this.computerStrategyFactory = computerStrategyFactory ?? CreateComputerStrategy;
    }

    public bool IsRunning { get; private set; }

    public Task StartAsync(GamePackage gamePackage, CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public Task SendInputAsync(string inputCommand, CancellationToken cancellationToken = default)
    {
        EnsureRunning();

        if (inputCommand.Equals(TicTacToeInput.RestartCommand, StringComparison.OrdinalIgnoreCase))
        {
            state = TicTacToeGameState.New(state.Difficulty) with
            {
                RestartCount = state.RestartCount + 1
            };
            return Task.CompletedTask;
        }

        if (!state.DifficultySelected)
        {
            if (!TicTacToeInput.TryParseDifficulty(inputCommand, out var difficulty))
            {
                throw new InvalidOperationException("The first TicTacToe input must choose difficulty 0, 1, or 2.");
            }

            state = state with
            {
                DifficultySelected = true,
                Difficulty = difficulty
            };
            return Task.CompletedTask;
        }

        if (state.IsComplete)
        {
            return Task.CompletedTask;
        }

        if (!TicTacToeInput.TryParseMove(inputCommand, out var position))
        {
            throw new InvalidOperationException("TicTacToe moves must be board positions 1 through 9.");
        }

        var boardAfterPlayerMove = state.Board.PlaceMark(position, state.PlayerMark);
        state = state with { Board = boardAfterPlayerMove };

        if (!state.IsComplete)
        {
            ApplyComputerMove();
        }

        return Task.CompletedTask;
    }

    public Task<TicTacToeGameState> ReadStateAsync(CancellationToken cancellationToken = default)
    {
        EnsureRunning();
        return Task.FromResult(state);
    }

    public ValueTask DisposeAsync()
    {
        IsRunning = false;
        return ValueTask.CompletedTask;
    }

    private static ITicTacToeMoveStrategy CreateComputerStrategy(TicTacToeDifficulty difficulty) =>
        difficulty switch
        {
            TicTacToeDifficulty.Hard => new PaidBetaComputerMoveStrategy(),
            TicTacToeDifficulty.Medium => new PaidBetaComputerMoveStrategy(),
            _ => new PaidBetaComputerMoveStrategy()
        };

    private void ApplyComputerMove()
    {
        var computerState = state with
        {
            PlayerMark = state.ComputerMark,
            ComputerMark = state.PlayerMark
        };

        var move = computerStrategyFactory(state.Difficulty).SelectMove(computerState);
        if (move is null)
        {
            return;
        }

        state = state with
        {
            Board = state.Board.PlaceMark(move.Value.Position, state.ComputerMark)
        };
    }

    private void EnsureRunning()
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException("Runtime has not been started.");
        }
    }
}
