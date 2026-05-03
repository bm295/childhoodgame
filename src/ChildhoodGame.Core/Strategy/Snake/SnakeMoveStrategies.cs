using System.Collections.Immutable;
using System.Linq;

namespace ChildhoodGame.Core.Strategy.Snake;

/// <summary>
/// Strategy for selecting the optimal move for the snake.
/// </summary>
public interface ISnakeMoveStrategy
{
    /// <summary>
    /// Selects the next direction for the snake to move.
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <returns>The direction the snake should move.</returns>
    SnakeDirection SelectNextDirection(SnakeGameState state);
}

/// <summary>
/// Pathfinding strategy that moves the snake toward the apple using greedy A*-like navigation.
/// Avoids walls and the snake's own body.
/// </summary>
public sealed class SnakeGreedyPathStrategy : ISnakeMoveStrategy
{
    public SnakeDirection SelectNextDirection(SnakeGameState state)
    {
        if (state.IsGameOver || state.SnakeBody.Count == 0)
        {
            return SnakeDirection.Right;
        }

        var head = state.SnakeBody[0];
        if (state.ApplePosition == null)
        {
            return GetSafeFallbackDirection(state, head);
        }

        var directMove = GetDirectMoveTowardsApple(state, head, state.ApplePosition);
        if (directMove != SnakeDirection.None)
        {
            return directMove;
        }

        var path = FindPathToApple(state, head, state.ApplePosition);
        if (path.Count > 0)
        {
            return path[0];
        }

        return GetSafeFallbackDirection(state, head);
    }

    private static IReadOnlyList<SnakeDirection> FindPathToApple(
        SnakeGameState state,
        SnakeSegment head,
        SnakeSegment apple)
    {
        var queue = new Queue<SnakeSegment>();
        var visited = new HashSet<SnakeSegment> { head };
        var parent = new Dictionary<SnakeSegment, (SnakeSegment Previous, SnakeDirection Direction)>();
        var reverseDirection = GetReverseDirection(state.CurrentDirection);

        var snakeBodyObstacles = new HashSet<SnakeSegment>(state.SnakeBody);
        if (state.SnakeBody.Count > 0)
        {
            snakeBodyObstacles.Remove(state.SnakeBody[^1]);
        }

        queue.Enqueue(head);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.Equals(apple))
            {
                return ReconstructPath(parent, current);
            }

            foreach (var direction in GetOrderedDirections(current, apple))
            {
                if (current.Equals(head) && direction == reverseDirection)
                {
                    continue;
                }

                var next = GetNextPosition(current, direction);
                if (!state.Board.IsInBounds(next.X, next.Y) || visited.Contains(next))
                {
                    continue;
                }

                if (snakeBodyObstacles.Contains(next))
                {
                    continue;
                }

                if (state.Board.Get(next.X, next.Y) == SnakeMark.Wall)
                {
                    continue;
                }

                visited.Add(next);
                parent[next] = (current, direction);
                queue.Enqueue(next);
            }
        }

        return Array.Empty<SnakeDirection>();
    }

    private static IReadOnlyList<SnakeDirection> ReconstructPath(
        Dictionary<SnakeSegment, (SnakeSegment Previous, SnakeDirection Direction)> parent,
        SnakeSegment target)
    {
        var path = new List<SnakeDirection>();
        var current = target;

        while (parent.TryGetValue(current, out var step))
        {
            path.Insert(0, step.Direction);
            current = step.Previous;
        }

        return path;
    }

    private static IEnumerable<SnakeDirection> GetOrderedDirections(SnakeSegment head, SnakeSegment apple)
    {
        var dx = apple.X - head.X;
        var dy = apple.Y - head.Y;
        var directions = new List<SnakeDirection>();

        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            if (dx > 0) directions.Add(SnakeDirection.Right);
            if (dx < 0) directions.Add(SnakeDirection.Left);
            if (dy > 0) directions.Add(SnakeDirection.Down);
            if (dy < 0) directions.Add(SnakeDirection.Up);
        }
        else
        {
            if (dy > 0) directions.Add(SnakeDirection.Down);
            if (dy < 0) directions.Add(SnakeDirection.Up);
            if (dx > 0) directions.Add(SnakeDirection.Right);
            if (dx < 0) directions.Add(SnakeDirection.Left);
        }

        foreach (var direction in Enum.GetValues<SnakeDirection>())
        {
            if (direction != SnakeDirection.None && !directions.Contains(direction))
            {
                directions.Add(direction);
            }
        }

        return directions;
    }

    private static SnakeDirection GetSafeFallbackDirection(SnakeGameState state, SnakeSegment head)
    {
        var currentDir = state.CurrentDirection;
        var reverseDir = GetReverseDirection(currentDir);
        var directions = new[] { currentDir, SnakeDirection.Up, SnakeDirection.Down, SnakeDirection.Left, SnakeDirection.Right };

        foreach (var direction in directions)
        {
            if (direction == SnakeDirection.None || direction == reverseDir)
            {
                continue;
            }

            if (!CausesCollision(state, head, direction))
            {
                return direction;
            }
        }

        return currentDir == SnakeDirection.None ? SnakeDirection.Right : currentDir;
    }

    private static SnakeDirection GetReverseDirection(SnakeDirection currentDirection) => currentDirection switch
    {
        SnakeDirection.Up => SnakeDirection.Down,
        SnakeDirection.Down => SnakeDirection.Up,
        SnakeDirection.Left => SnakeDirection.Right,
        SnakeDirection.Right => SnakeDirection.Left,
        _ => SnakeDirection.None
    };

    private static bool CausesCollision(SnakeGameState state, SnakeSegment head, SnakeDirection direction)
    {
        var nextPos = GetNextPosition(head, direction);

        if (!state.Board.IsInBounds(nextPos.X, nextPos.Y))
        {
            return true;
        }

        var cellContent = state.Board.Get(nextPos.X, nextPos.Y);
        if (cellContent == SnakeMark.Wall)
        {
            return true;
        }

        if (cellContent == SnakeMark.SnakeBody &&
            !(nextPos.X == state.SnakeBody[^1].X && nextPos.Y == state.SnakeBody[^1].Y))
        {
            return true;
        }

        return false;
    }

    private static SnakeDirection GetDirectMoveTowardsApple(SnakeGameState state, SnakeSegment head, SnakeSegment apple)
    {
        var dx = apple.X - head.X;
        var dy = apple.Y - head.Y;
        var currentDir = state.CurrentDirection;
        var currentDistance = GetManhattanDistance(head, apple);

        if (state.SnakeBody.Count > 1 &&
            currentDir != SnakeDirection.None &&
            currentDir != GetReverseDirection(currentDir) &&
            !CausesCollision(state, head, currentDir))
        {
            var nextDistance = GetManhattanDistance(GetNextPosition(head, currentDir), apple);
            if (nextDistance < currentDistance)
            {
                return currentDir;
            }
        }

        var preferredDirections = new List<SnakeDirection>();
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            if (dx > 0) preferredDirections.Add(SnakeDirection.Right);
            if (dx < 0) preferredDirections.Add(SnakeDirection.Left);
            if (dy > 0) preferredDirections.Add(SnakeDirection.Down);
            if (dy < 0) preferredDirections.Add(SnakeDirection.Up);
        }
        else
        {
            if (dy > 0) preferredDirections.Add(SnakeDirection.Down);
            if (dy < 0) preferredDirections.Add(SnakeDirection.Up);
            if (dx > 0) preferredDirections.Add(SnakeDirection.Right);
            if (dx < 0) preferredDirections.Add(SnakeDirection.Left);
        }

        foreach (var direction in preferredDirections.Distinct())
        {
            if (state.SnakeBody.Count > 1 && direction == GetReverseDirection(currentDir))
            {
                continue;
            }

            if (direction == currentDir)
            {
                continue;
            }

            if (CausesCollision(state, head, direction))
            {
                continue;
            }

            var nextDistance = GetManhattanDistance(GetNextPosition(head, direction), apple);
            if (nextDistance < currentDistance)
            {
                return direction;
            }
        }

        return SnakeDirection.None;
    }

    private static SnakeSegment GetNextPosition(SnakeSegment head, SnakeDirection direction)
    {
        return direction switch
        {
            SnakeDirection.Up => new(head.X, head.Y - 1),
            SnakeDirection.Down => new(head.X, head.Y + 1),
            SnakeDirection.Left => new(head.X - 1, head.Y),
            SnakeDirection.Right => new(head.X + 1, head.Y),
            _ => head
        };
    }

    private static int GetManhattanDistance(SnakeSegment a, SnakeSegment b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }
}

