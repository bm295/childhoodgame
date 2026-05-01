using System.Collections.Immutable;

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
        if (state.IsGameOver || state.ApplePosition == null || state.SnakeBody.Count == 0)
        {
            return SnakeDirection.Right; // Default fallback
        }

        var head = state.SnakeBody[0];
        var apple = state.ApplePosition;

        // Calculate the direction that moves closer to the apple
        var dx = Math.Sign(apple.X - head.X);
        var dy = Math.Sign(apple.Y - head.Y);

        // Prefer the direction that reduces distance most
        // If we can move horizontally or vertically, prioritize the one that reduces distance more
        var directions = GetCandidateDirections(state, head, dx, dy)
            .OrderByDescending(dir => CalculateDirectionPriority(dir, head, apple, state))
            .ToList();

        return directions.FirstOrDefault(SnakeDirection.Right);
    }

    /// <summary>
    /// Returns candidate directions that don't cause immediate collision.
    /// Attempts to move toward the apple, with fallback directions.
    /// </summary>
    private static IEnumerable<SnakeDirection> GetCandidateDirections(
        SnakeGameState state, 
        SnakeSegment head,
        int appleXDirection,
        int appleYDirection)
    {
        var candidates = new List<SnakeDirection>();

        // Primary direction preferences: horizontal or vertical toward apple
        if (appleXDirection != 0)
        {
            var xDir = appleXDirection > 0 ? SnakeDirection.Right : SnakeDirection.Left;
            if (!CausesCollision(state, head, xDir))
            {
                candidates.Add(xDir);
            }
        }

        if (appleYDirection != 0)
        {
            var yDir = appleYDirection > 0 ? SnakeDirection.Down : SnakeDirection.Up;
            if (!CausesCollision(state, head, yDir))
            {
                candidates.Add(yDir);
            }
        }

        // Fallback: try all other safe directions
        var allDirections = new[] { SnakeDirection.Up, SnakeDirection.Down, SnakeDirection.Left, SnakeDirection.Right };
        foreach (var dir in allDirections)
        {
            if (!candidates.Contains(dir) && !CausesCollision(state, head, dir))
            {
                candidates.Add(dir);
            }
        }

        return candidates.Count > 0 ? candidates : new[] { SnakeDirection.Right };
    }

    /// <summary>
    /// Calculates a priority score for a direction based on distance to apple and safety.
    /// Higher scores are better.
    /// </summary>
    private static double CalculateDirectionPriority(
        SnakeDirection direction,
        SnakeSegment head,
        SnakeSegment apple,
        SnakeGameState state)
    {
        var nextPos = GetNextPosition(head, direction);
        
        // Distance reduction score (negative means moving away)
        var currentDistance = Math.Abs(head.X - apple.X) + Math.Abs(head.Y - apple.Y);
        var nextDistance = Math.Abs(nextPos.X - apple.X) + Math.Abs(nextPos.Y - apple.Y);
        var distanceImprovement = currentDistance - nextDistance;

        // Prefer directions that move toward the apple
        return distanceImprovement * 10.0;
    }

    /// <summary>
    /// Checks if moving in a direction would cause the snake to collide with a wall or itself.
    /// </summary>
    private static bool CausesCollision(SnakeGameState state, SnakeSegment head, SnakeDirection direction)
    {
        var nextPos = GetNextPosition(head, direction);

        // Check bounds (walls)
        if (!state.Board.IsInBounds(nextPos.X, nextPos.Y))
        {
            return true;
        }

        // Check collision with board obstacles (walls)
        var board = state.Board;
        var cellContent = board.Get(nextPos.X, nextPos.Y);
        if (cellContent == SnakeMark.Wall)
        {
            return true;
        }

        // Check collision with body (except the tail, which will move away)
        if (cellContent == SnakeMark.SnakeBody && 
            !(nextPos.X == state.SnakeBody[^1].X && nextPos.Y == state.SnakeBody[^1].Y))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculates the next position if the snake moves in the given direction.
    /// </summary>
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
}
