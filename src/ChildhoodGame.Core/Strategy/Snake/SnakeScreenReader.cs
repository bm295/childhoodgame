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

                var stepX = Math.Max(1, (right - left) / 5);
                var stepY = Math.Max(1, (bottom - top) / 5);

                for (var sampleY = top; sampleY < bottom; sampleY += stepY)
                {
                    for (var sampleX = left; sampleX < right; sampleX += stepX)
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

                if (appleCount >= 1)
                {
                    apple = new SnakeSegment(x, y);
                    board.Set(x, y, SnakeMark.Apple);
                }
                else if (snakeCount >= sampleCount * 0.5)
                {
                    snakeCells.Add(new SnakeSegment(x, y));
                }
                else if (wallCount >= sampleCount * 0.5 || (IsBoardEdgeCell(x, y, boardWidth, boardHeight) && wallCount > 0))
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

        if (apple == null)
        {
            apple = DetectApplePosition(capture, boardBounds, boardWidth, boardHeight);
            if (apple != null)
            {
                board.Set(apple.X, apple.Y, SnakeMark.Apple);
            }
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

        var isYellowSnake = r > 150 && g > 150 && b < 130 && Math.Abs(r - g) < 80;
        var isGreenSnake = g > 150 && r > 100 && b < 120 && g > r - 40;
        return isYellowSnake || isGreenSnake;
    }

    private static bool IsApple(int pixel)
    {
        var r = (pixel >> 16) & 0xFF;
        var g = (pixel >> 8) & 0xFF;
        var b = pixel & 0xFF;

        // Allow reddish or yellowish colors, but not green
        bool isRed = r > 120 && r > g + 25 && r > b + 25 && g < 180 && b < 180;
        bool isYellow = g > 120 && r > 100 && g > r + 10 && g > b + 25 && b < 120;
        return isRed || isYellow;
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

        return brightness < 110 || (r < 110 && g < 110 && b < 110);
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

    private static SnakeSegment? DetectApplePosition(
        GameWindowCapture capture,
        (int X, int Y, int Width, int Height) boardBounds,
        int boardWidth,
        int boardHeight)
    {
        var appleCounts = new int[boardHeight, boardWidth];
        var applePixels = 0;

        var stepX = Math.Max(1, boardBounds.Width / (boardWidth * 4));
        var stepY = Math.Max(1, boardBounds.Height / (boardHeight * 4));

        for (var sampleY = boardBounds.Y; sampleY < boardBounds.Y + boardBounds.Height; sampleY += stepY)
        {
            for (var sampleX = boardBounds.X; sampleX < boardBounds.X + boardBounds.Width; sampleX += stepX)
            {
                var pixel = capture.GetPixelArgb(sampleX, sampleY);
                if (!IsApple(pixel))
                {
                    continue;
                }

                applePixels++;
                var relativeX = sampleX - boardBounds.X;
                var relativeY = sampleY - boardBounds.Y;
                var cellX = Math.Min(boardWidth - 1, Math.Max(0, relativeX * boardWidth / Math.Max(1, boardBounds.Width)));
                var cellY = Math.Min(boardHeight - 1, Math.Max(0, relativeY * boardHeight / Math.Max(1, boardBounds.Height)));
                appleCounts[cellY, cellX]++;
            }
        }

        if (applePixels == 0)
        {
            return null;
        }

        var bestCell = (x: 0, y: 0, count: 0);
        for (var y = 0; y < boardHeight; y++)
        {
            for (var x = 0; x < boardWidth; x++)
            {
                if (appleCounts[y, x] > bestCell.count)
                {
                    bestCell = (x, y, appleCounts[y, x]);
                }
            }
        }

        if (bestCell.count == 0)
        {
            return null;
        }

        return new SnakeSegment(bestCell.x, bestCell.y);
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
