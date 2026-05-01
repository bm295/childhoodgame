namespace ChildhoodGame.Core.Strategy.Snake;

/// <summary>
/// Action selection strategy for Snake that uses a move strategy to determine input commands.
/// </summary>
public sealed class SnakeActionStrategy : IActionSelectionStrategy<SnakeGameState>
{
    private readonly ISnakeMoveStrategy _moveStrategy;

    public SnakeActionStrategy(ISnakeMoveStrategy moveStrategy)
    {
        _moveStrategy = moveStrategy ?? throw new ArgumentNullException(nameof(moveStrategy));
    }

    public IReadOnlyList<string> SelectActions(SnakeGameState state)
    {
        if (state.IsGameOver)
        {
            return Array.Empty<string>();
        }

        var direction = _moveStrategy.SelectNextDirection(state);
        var command = EncodeDirection(direction);

        return string.IsNullOrEmpty(command) ? Array.Empty<string>() : new[] { command };
    }

    /// <summary>
    /// Encodes a snake direction as a command string (e.g., "UP", "DOWN", "LEFT", "RIGHT").
    /// </summary>
    private static string EncodeDirection(SnakeDirection direction)
    {
        return direction switch
        {
            SnakeDirection.Up => "UP",
            SnakeDirection.Down => "DOWN",
            SnakeDirection.Left => "LEFT",
            SnakeDirection.Right => "RIGHT",
            SnakeDirection.None => "",
            _ => ""
        };
    }
}
