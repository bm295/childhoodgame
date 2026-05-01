namespace ChildhoodGame.Core.Strategy;

/// <summary>
/// Provides auto-win strategy execution for a specific game type.
/// Each game (TicTacToe, Snake, etc.) implements this to handle its own generic type complexity.
/// </summary>
public interface IGameStrategyProvider
{
    /// <summary>
    /// Gets the name of the game this provider handles (e.g., "TicTacToe", "Snake").
    /// </summary>
    string GameName { get; }

    /// <summary>
    /// Executes the auto-win strategy for this game asynchronously.
    /// </summary>
    /// <param name="package">The game package to execute.</param>
    /// <param name="options">Auto-win execution options (max steps, delay).</param>
    /// <param name="outputHandler">Callback for writing output lines to console.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the win condition was satisfied, false otherwise.</returns>
    Task<bool> ExecuteAutoWinAsync(
        GamePackage package,
        AutoWinOptions options,
        Action<string> outputHandler,
        CancellationToken cancellationToken = default);
}
