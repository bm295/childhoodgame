namespace ChildhoodGame.Core.Strategy.TicTacToe;

public sealed class TicTacToePlayerWinConditionDetector : IWinConditionDetector<TicTacToeGameState>
{
    public string Name => "Player O wins";

    public bool IsSatisfied(TicTacToeGameState state) =>
        state.Board.GetWinner() == state.PlayerMark;
}
