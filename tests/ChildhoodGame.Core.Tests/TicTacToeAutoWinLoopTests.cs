using ChildhoodGame.Core;
using ChildhoodGame.Core.Strategy;
using ChildhoodGame.Core.Strategy.TicTacToe;
using Xunit;

namespace ChildhoodGame.Core.Tests;

public sealed class AutoWinLoopTests
{
    [Fact]
    public async Task MockedTicTacToeRuntime_AppliesDifficultyAndMoves_WithComputerResponse()
    {
        var computer = new PaidBetaComputerMoveStrategy(new[] { 1 });
        await using var runtime = new MockedTicTacToeRuntime(computerStrategyFactory: _ => computer);
        await runtime.StartAsync(BuildPackage());

        await runtime.SendInputAsync("2");
        await runtime.SendInputAsync("5");

        var state = await runtime.ReadStateAsync();

        Assert.True(state.DifficultySelected);
        Assert.Equal(TicTacToeDifficulty.Hard, state.Difficulty);
        Assert.Equal(TicTacToeMark.O, state.Board[5]);
        Assert.Equal(TicTacToeMark.X, state.Board[1]);
    }

    [Fact]
    public void OptimalTicTacToeStrategy_TakesWinningMove_WhenAvailable()
    {
        var board = new TicTacToeBoard(new[]
        {
            TicTacToeMark.O, TicTacToeMark.O, TicTacToeMark.Empty,
            TicTacToeMark.X, TicTacToeMark.X, TicTacToeMark.Empty,
            TicTacToeMark.Empty, TicTacToeMark.Empty, TicTacToeMark.Empty
        });
        var state = new TicTacToeGameState(
            board,
            TicTacToeMark.O,
            TicTacToeMark.X,
            DifficultySelected: true,
            Difficulty: TicTacToeDifficulty.Easy);

        var move = new OptimalTicTacToeMoveStrategy().SelectMove(state);

        Assert.Equal(3, move?.Position);
    }

    [Fact]
    public async Task AutoWinLoop_RestartsUntilPaidBetaHardBranchCanBeForcedToWin()
    {
        var computer = new PaidBetaComputerMoveStrategy(new[] { 1, 2 });
        await using var runtime = new MockedTicTacToeRuntime(computerStrategyFactory: _ => computer);
        await runtime.StartAsync(BuildPackage());

        var loop = new AutoWinLoop<TicTacToeGameState>(
            runtime,
            new PaidBetaAlwaysWinActionStrategy(),
            new[] { new TicTacToePlayerWinConditionDetector() });
        var result = await loop.RunAsync(new AutoWinOptions(MaxSteps: 20));

        Assert.True(result.IsWin);
        Assert.Contains(result.Progress, progress => progress.Actions.Contains(TicTacToeInput.RestartCommand));
        Assert.Equal(1, result.Progress.Last().StateAfterActions.RestartCount);
        Assert.Equal(TicTacToeMark.O, result.Progress.Last().StateAfterActions.Board.GetWinner());
    }

    [Fact]
    public void PaidBetaHardModeMoveOracle_CentersAgainstFirstNonCenterMove()
    {
        var board = new TicTacToeBoard().PlaceMark(1, TicTacToeMark.O);

        var moves = PaidBetaHardModeMoveOracle.GetPossibleMoves(
            board,
            TicTacToeMark.X,
            TicTacToeMark.O);

        Assert.Equal(new[] { 5 }, moves);
    }

    [Fact]
    public void PaidBetaHardModeExploitMoveStrategy_UsesForkBranchAfterComputerSideMove()
    {
        var board = new TicTacToeBoard()
            .PlaceMark(5, TicTacToeMark.O)
            .PlaceMark(2, TicTacToeMark.X);
        var state = new TicTacToeGameState(
            board,
            TicTacToeMark.O,
            TicTacToeMark.X,
            DifficultySelected: true,
            Difficulty: TicTacToeDifficulty.Hard);

        var move = new PaidBetaHardModeExploitMoveStrategy().SelectMove(state);

        Assert.Equal(1, move?.Position);
    }

    private static GamePackage BuildPackage() =>
        new(
            GameRootPath: "/tmp",
            GameExecutablePath: "MOCK.EXE",
            DosConfigPath: "DOSBOX.CONF",
            RuntimeConfig: new DosRuntimeConfig("embedded", null, null, null),
            SaveStatePath: null,
            LoadStatePath: null);
}
