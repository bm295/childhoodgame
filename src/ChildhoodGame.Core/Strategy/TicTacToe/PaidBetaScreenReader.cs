namespace ChildhoodGame.Core.Strategy.TicTacToe;

public sealed class PaidBetaScreenReader
{
    private const int LogicalWidth = 640;
    private const int LogicalHeight = 480;
    private const int ActivePixelThreshold = 35;

    private static readonly IReadOnlyDictionary<int, (int X, int Y)> CellCenters =
        new Dictionary<int, (int X, int Y)>
        {
            [1] = (50, 150),
            [2] = (150, 150),
            [3] = (250, 150),
            [4] = (50, 250),
            [5] = (150, 250),
            [6] = (250, 250),
            [7] = (50, 350),
            [8] = (150, 350),
            [9] = (250, 350)
        };

    public TicTacToeGameState ReadState(
        GameWindowCapture capture,
        TicTacToeDifficulty difficulty = TicTacToeDifficulty.Hard)
    {
        var cells = new TicTacToeMark[TicTacToeBoard.CellCount];
        foreach (var cell in CellCenters)
        {
            cells[cell.Key - 1] = DetectMark(capture, cell.Value.X, cell.Value.Y);
        }

        return new TicTacToeGameState(
            new TicTacToeBoard(cells),
            TicTacToeMark.O,
            TicTacToeMark.X,
            DifficultySelected: true,
            Difficulty: difficulty);
    }

    private static TicTacToeMark DetectMark(GameWindowCapture capture, int logicalCenterX, int logicalCenterY)
    {
        var xScore = CountXPatternPixels(capture, logicalCenterX, logicalCenterY);
        var oScore = CountOPatternPixels(capture, logicalCenterX, logicalCenterY);

        if (xScore >= 26 && xScore > oScore * 2)
        {
            return TicTacToeMark.X;
        }

        if (oScore >= 10)
        {
            return TicTacToeMark.O;
        }

        return TicTacToeMark.Empty;
    }

    private static int CountXPatternPixels(GameWindowCapture capture, int logicalCenterX, int logicalCenterY)
    {
        var score = 0;
        for (var offset = -24; offset <= 24; offset += 2)
        {
            if (HasActivePixelNear(capture, logicalCenterX + offset, logicalCenterY + offset, radius: 2))
            {
                score++;
            }

            if (HasActivePixelNear(capture, logicalCenterX + offset, logicalCenterY - offset, radius: 2))
            {
                score++;
            }
        }

        return score;
    }

    private static int CountOPatternPixels(GameWindowCapture capture, int logicalCenterX, int logicalCenterY)
    {
        var score = 0;
        for (var angle = 0; angle < 360; angle += 12)
        {
            var radians = Math.PI * angle / 180.0;
            var x = logicalCenterX + (int)Math.Round(Math.Cos(radians) * 25);
            var y = logicalCenterY + (int)Math.Round(Math.Sin(radians) * 25);
            if (HasActivePixelNear(capture, x, y, radius: 2))
            {
                score++;
            }
        }

        return score;
    }

    private static bool HasActivePixelNear(GameWindowCapture capture, int logicalX, int logicalY, int radius)
    {
        var x = ScaleX(capture, logicalX);
        var y = ScaleY(capture, logicalY);
        var scaledRadius = Math.Max(1, ScaleRadius(capture, radius));

        for (var dy = -scaledRadius; dy <= scaledRadius; dy++)
        {
            for (var dx = -scaledRadius; dx <= scaledRadius; dx++)
            {
                if (IsActivePixel(capture.GetPixelArgb(x + dx, y + dy)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int ScaleX(GameWindowCapture capture, int logicalX) =>
        Math.Clamp((int)Math.Round(logicalX * capture.Width / (double)LogicalWidth), 0, capture.Width - 1);

    private static int ScaleY(GameWindowCapture capture, int logicalY) =>
        Math.Clamp((int)Math.Round(logicalY * capture.Height / (double)LogicalHeight), 0, capture.Height - 1);

    private static int ScaleRadius(GameWindowCapture capture, int logicalRadius) =>
        (int)Math.Round(logicalRadius * Math.Min(capture.Width / (double)LogicalWidth, capture.Height / (double)LogicalHeight));

    private static bool IsActivePixel(int argb)
    {
        var red = (argb >> 16) & 0xff;
        var green = (argb >> 8) & 0xff;
        var blue = argb & 0xff;
        return Math.Max(red, Math.Max(green, blue)) >= ActivePixelThreshold;
    }
}
