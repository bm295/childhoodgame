using System.Collections.Immutable;
using System.Linq;

namespace ChildhoodGame.Core.Strategy.Snake;

/// <summary>
/// Reads the snake game state from captured window pixels.
/// </summary>
public sealed class SnakeScreenReader
{
    // Classic Snake DOS game uses a green background
    private const uint RedApple = 0xFFFF0000;        // Red apple
    private const uint YellowSnake = 0xFFFFFF00;     // Yellow snake

    /// <summary>
    /// Reads the current snake game state from the captured window.
    /// </summary>
    public SnakeGameState ReadGameState(GameWindowCapture capture, int boardWidth, int boardHeight)
    {
        var boardBounds = DetectBoardBounds(capture);
        var cellWidth = boardBounds.Width / (double)boardWidth;
        var cellHeight = boardBounds.Height / (double)boardHeight;

        var board = new SnakeBoard(boardWidth, boardHeight);
        var snakeCells = new List<SnakeSegment>();
        SnakeSegment? apple = null;

        for (var y = 0; y < boardHeight; y++)
        {
            for (var x = 0; x < boardWidth; x++)
            {
                var left = boardBounds.X + (int)Math.Floor(x * cellWidth + cellWidth * 0.1);
                var top = boardBounds.Y + (int)Math.Floor(y * cellHeight + cellHeight * 0.1);
                var right = boardBounds.X + (int)Math.Ceiling((x + 1) * cellWidth - cellWidth * 0.1);
                var bottom = boardBounds.Y + (int)Math.Ceiling((y + 1) * cellHeight - cellHeight * 0.1);

                var appleCount = 0;
                var snakeCount = 0;
                var wallCount = 0;
                var sampleCount = 0;

                for (var sampleY = top; sampleY < bottom; sampleY += Math.Max(1, (bottom - top) / 3))
                {
                    for (var sampleX = left; sampleX < right; sampleX += Math.Max(1, (right - left) / 3))
                    {
                        var pixel = capture.GetPixelArgb(sampleX, sampleY);
                        if (IsApple(pixel))
                        {
                            appleCount++;
                        }
                        else if (IsSnake(pixel))
                        {
                            snakeCount++;
                        }
                        else if (IsWallPixel(pixel))
                        {
                            wallCount++;
                        }

                        sampleCount++;
                    }
                }

                if (appleCount >= 2)
                {
                    apple = new SnakeSegment(x, y);
                    board.Set(x, y, SnakeMark.Apple);
                }
                else if (snakeCount > sampleCount / 3)
                {
                    snakeCells.Add(new SnakeSegment(x, y));
                }
                else if (wallCount > sampleCount / 2 || IsBoardEdgeCell(x, y, boardWidth, boardHeight) && wallCount > 0)
                {
                    board.Set(x, y, SnakeMark.Wall);
                }
            }
        }

        var orderedSnakeBody = OrderSnakeBody(snakeCells, apple, boardWidth, boardHeight);
        for (var index = 0; index < orderedSnakeBody.Count; index++)
        {
            var cell = orderedSnakeBody[index];
            board.Set(cell.X, cell.Y, index == 0 ? SnakeMark.SnakeHead : SnakeMark.SnakeBody);
        }

        if (orderedSnakeBody.Count == 0)
        {
            orderedSnakeBody = ImmutableList.Create(new SnakeSegment(boardWidth / 2, boardHeight / 2));
            board.Set(orderedSnakeBody[0].X, orderedSnakeBody[0].Y, SnakeMark.SnakeHead);
        }

        var state = new SnakeGameState(board, orderedSnakeBody, apple)
        {
            CurrentDirection = GetCurrentDirection(orderedSnakeBody),
            NextDirection = GetCurrentDirection(orderedSnakeBody)
        };

        return state;
    }

    private static (int X, int Y, int Width, int Height) DetectBoardBounds(GameWindowCapture capture)
    {
        var width = capture.Width;
        var height = capture.Height;
        var left = width;
        var right = 0;
        var top = height;
        var bottom = 0;

        for (var y = 0; y < height; y++)
        {
            var rowCount = 0;
            for (var x = 0; x < width; x++)
            {
                var pixel = capture.GetPixelArgb(x, y);
                if (IsBoardPixel(pixel))
                {
                    rowCount++;
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                    top = Math.Min(top, y);
                    bottom = Math.Max(bottom, y);
                }
            }

            if (rowCount > width * 0.25 && y > height * 0.1 && y < height * 0.9)
            {
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
            }
        }

        if (right <= left || bottom <= top)
        {
            return (0, 0, width, height);
        }

        var cropLeft = Math.Max(0, left - 4);
        var cropTop = Math.Max(0, top - 4);
        var cropRight = Math.Min(width, right + 4);
        var cropBottom = Math.Min(height, bottom + 4);

        return (cropLeft, cropTop, cropRight - cropLeft, cropBottom - cropTop);
    }

    private static bool IsBoardPixel(int pixel)
    {
        return IsEmptyBackground(pixel) || IsSnake(pixel) || IsApple(pixel) || IsWallPixel(pixel);
    }

    private static bool IsSnake(int pixel)
    {
        var r = (pixel >> 16) & 0xFF;
        var g = (pixel >> 8) & 0xFF;
        var b = pixel & 0xFF;

        // Snake is usually yellow in the DOS version, but support greenish variants too.
        var isYellowSnake = r > 160 && g > 160 && b < 120;
        var isGreenSnake = g > 160 && r > 120 && b < 120;
        return isYellowSnake || isGreenSnake;
    }

    private static bool IsApple(int pixel)
    {
        var r = (pixel >> 16) & 0xFF;
        var g = (pixel >> 8) & 0xFF;
        var b = pixel & 0xFF;

        return r > 150 && r > g + 20 && r > b + 20 && g < 140 && b < 140;
    }

    private static bool IsWallPixel(int pixel)
    {
        if (IsApple(pixel) || IsSnake(pixel) || IsEmptyBackground(pixel))
        {
            return false;
        }

        var r = (pixel >> 16) & 0xFF;
        var g = (pixel >> 8) & 0xFF;
        var b = pixel & 0xFF;
        var brightness = (r + g + b) / 3;

        return brightness < 120 || (r < 120 && g < 120 && b < 120);
    }

    private static bool IsEmptyBackground(int pixel)
    {
        var r = (pixel >> 16) & 0xFF;
        var g = (pixel >> 8) & 0xFF;
        var b = pixel & 0xFF;

        return g > 120 && r < 140 && b < 120 && g > r + 20 && g > b + 20;
    }

    private static bool IsBoardEdgeCell(int x, int y, int width, int height)
    {
        return x == 0 || y == 0 || x == width - 1 || y == height - 1;
    }

    private static ImmutableList<SnakeSegment> OrderSnakeBody(
        IReadOnlyList<SnakeSegment> snakeCells,
        SnakeSegment? apple,
        int boardWidth,
        int boardHeight)
    {
        if (snakeCells.Count <= 1)
        {
            return ImmutableList.CreateRange(snakeCells);
        }

        var adjacency = snakeCells.ToDictionary(cell => cell, cell => new List<SnakeSegment>());
        foreach (var cell in snakeCells)
        {
            foreach (var neighbor in GetNeighbors(cell))
            {
                if (snakeCells.Contains(neighbor))
                {
                    adjacency[cell].Add(neighbor);
                }
            }
        }

        var endpoints = adjacency.Where(pair => pair.Value.Count == 1)
            .Select(pair => pair.Key)
            .ToList();

        var head = endpoints.Count switch
        {
            0 => snakeCells[0],
            1 => endpoints[0],
            _ => ChooseHead(endpoints, apple, boardWidth, boardHeight)
        };

        var ordered = new List<SnakeSegment> { head };
        var previous = head;

        while (ordered.Count < snakeCells.Count)
        {
            var next = adjacency[previous].FirstOrDefault(cell => !ordered.Contains(cell));
            if (next == null)
            {
                break;
            }

            ordered.Add(next);
            previous = next;
        }

        return ordered.Count == snakeCells.Count
            ? ImmutableList.CreateRange(ordered)
            : ImmutableList.CreateRange(snakeCells);
    }

    private static SnakeSegment ChooseHead(
        IReadOnlyList<SnakeSegment> endpoints,
        SnakeSegment? apple,
        int boardWidth,
        int boardHeight)
    {
        if (apple != null)
        {
            return endpoints.OrderBy(endpoint => GetManhattanDistance(endpoint, apple)).First();
        }

        return endpoints
            .OrderByDescending(endpoint => CountFreeNeighbors(endpoint, boardWidth, boardHeight, endpoints))
            .First();
    }

    private static int CountFreeNeighbors(
        SnakeSegment segment,
        int boardWidth,
        int boardHeight,
        IReadOnlyCollection<SnakeSegment> snakeCells)
    {
        return GetNeighbors(segment).Count(neighbor =>
            neighbor.X >= 0 && neighbor.X < boardWidth &&
            neighbor.Y >= 0 && neighbor.Y < boardHeight &&
            !snakeCells.Contains(neighbor));
    }

    private static IEnumerable<SnakeSegment> GetNeighbors(SnakeSegment segment)
    {
        yield return new SnakeSegment(segment.X - 1, segment.Y);
        yield return new SnakeSegment(segment.X + 1, segment.Y);
        yield return new SnakeSegment(segment.X, segment.Y - 1);
        yield return new SnakeSegment(segment.X, segment.Y + 1);
    }

    private static int GetManhattanDistance(SnakeSegment a, SnakeSegment b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    private static SnakeDirection GetCurrentDirection(ImmutableList<SnakeSegment> orderedSnake)
    {
        if (orderedSnake.Count < 2)
        {
            return SnakeDirection.Right;
        }

        var head = orderedSnake[0];
        var next = orderedSnake[1];
        return head.X > next.X ? SnakeDirection.Right :
               head.X < next.X ? SnakeDirection.Left :
               head.Y > next.Y ? SnakeDirection.Down :
               head.Y < next.Y ? SnakeDirection.Up :
               SnakeDirection.Right;
    }
}
