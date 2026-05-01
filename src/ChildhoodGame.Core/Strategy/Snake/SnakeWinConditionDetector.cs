namespace ChildhoodGame.Core.Strategy.Snake;

/// <summary>
/// Detects when the Snake game reaches a win condition.
/// Win conditions can be: snake reaches a certain length, eats a certain number of apples, or achieves a score.
/// </summary>
public sealed class SnakeWinConditionDetector : IWinConditionDetector<SnakeGameState>
{
    /// <summary>
    /// Default apple count to win (can be configured).
    /// </summary>
    private const int DefaultApplesRequired = 10;

    public string Name => "SnakeWinCondition";

    private readonly int _applesRequired;

    public SnakeWinConditionDetector(int applesRequired = DefaultApplesRequired)
    {
        if (applesRequired <= 0)
        {
            throw new ArgumentException("Apples required must be positive.", nameof(applesRequired));
        }

        _applesRequired = applesRequired;
    }

    /// <summary>
    /// Checks if the win condition is satisfied.
    /// The snake wins by eating the required number of apples without dying.
    /// </summary>
    public bool IsSatisfied(SnakeGameState state)
    {
        return !state.IsGameOver && state.ApplesEaten >= _applesRequired;
    }
}
