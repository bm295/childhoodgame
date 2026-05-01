using System.Collections.Immutable;
using ChildhoodGame.Core.Strategy.Snake;
using ChildhoodGame.Core.Strategy.TicTacToe;

namespace ChildhoodGame.Core.Strategy;

/// <summary>
/// Registry of available game strategy providers.
/// Maps game names (detected from folder names) to their corresponding strategy providers.
/// </summary>
public sealed class GameStrategyRegistry
{
    private readonly ImmutableDictionary<string, Func<IGameStrategyProvider>> _providers;

    /// <summary>
    /// Initializes the registry with all available game strategies.
    /// </summary>
    public GameStrategyRegistry()
    {
        _providers = ImmutableDictionary.CreateRange(new Dictionary<string, Func<IGameStrategyProvider>>
        {
            { "tictactoe", () => new TicTacToeStrategyProvider() },
            { "snake", () => new SnakeStrategyProvider() },
        });
    }

    /// <summary>
    /// Gets the strategy provider for the specified game name.
    /// Game name is extracted from the game package's root path and matched case-insensitively.
    /// </summary>
    /// <param name="gameName">The name of the game (e.g., "TicTacToe", "Snake").</param>
    /// <returns>The strategy provider for the game.</returns>
    /// <exception cref="ArgumentException">Thrown if the game name is not registered.</exception>
    public IGameStrategyProvider GetProvider(string gameName)
    {
        if (string.IsNullOrWhiteSpace(gameName))
        {
            throw new ArgumentException("Game name cannot be null or whitespace.", nameof(gameName));
        }

        var normalizedName = gameName.Trim().ToLowerInvariant();

        if (!_providers.TryGetValue(normalizedName, out var factory))
        {
            var availableGames = string.Join(", ", _providers.Keys.Select(k => $"\"{k}\""));
            throw new ArgumentException(
                $"Unknown game type: \"{gameName}\". Available games: {availableGames}",
                nameof(gameName));
        }

        return factory();
    }

    /// <summary>
    /// Detects the game name from a game package's root path.
    /// Uses the folder name as the game identifier (e.g., "C:\Games\TicTacToe" → "TicTacToe").
    /// </summary>
    /// <param name="gamePackage">The game package with a root path.</param>
    /// <returns>The detected game name.</returns>
    public static string DetectGameName(GamePackage gamePackage)
    {
        return DetectGameNameFromPath(gamePackage.GameRootPath);
    }

    /// <summary>
    /// Detects the game name from a file system path.
    /// Uses the leaf folder name as the game identifier.
    /// </summary>
    /// <param name="gameRootPath">The root path to the game folder.</param>
    /// <returns>The detected game name.</returns>
    public static string DetectGameNameFromPath(string gameRootPath)
    {
        if (string.IsNullOrWhiteSpace(gameRootPath))
        {
            throw new ArgumentException("Game root path cannot be null or whitespace.", nameof(gameRootPath));
        }

        var folderName = Path.GetFileName(gameRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        
        if (string.IsNullOrWhiteSpace(folderName))
        {
            throw new ArgumentException("Could not extract game name from path: " + gameRootPath, nameof(gameRootPath));
        }

        return folderName;
    }
}
