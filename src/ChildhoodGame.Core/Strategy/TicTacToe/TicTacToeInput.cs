using System.Globalization;

namespace ChildhoodGame.Core.Strategy.TicTacToe;

public static class TicTacToeInput
{
    public const string RestartCommand = "RESTART";

    public static string Restart() => RestartCommand;

    public static string SelectDifficulty(TicTacToeDifficulty difficulty) =>
        ((int)difficulty).ToString(CultureInfo.InvariantCulture);

    public static string PlayMove(int position) =>
        position.ToString(CultureInfo.InvariantCulture);

    public static bool TryParseDifficulty(string inputCommand, out TicTacToeDifficulty difficulty)
    {
        difficulty = TicTacToeDifficulty.Easy;
        if (!int.TryParse(inputCommand, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        if (!Enum.IsDefined(typeof(TicTacToeDifficulty), value))
        {
            return false;
        }

        difficulty = (TicTacToeDifficulty)value;
        return true;
    }

    public static bool TryParseMove(string inputCommand, out int position)
    {
        if (!int.TryParse(inputCommand, NumberStyles.Integer, CultureInfo.InvariantCulture, out position))
        {
            return false;
        }

        return position is >= 1 and <= TicTacToeBoard.CellCount;
    }
}
