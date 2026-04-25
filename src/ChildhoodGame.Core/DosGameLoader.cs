using System.Text.Json;

namespace ChildhoodGame.Core;

public sealed class DosGameLoader : IGameLoader
{
    private static readonly string[] RequiredAssets =
    [
        "DOSBOX.CONF"
    ];

    public GameLoadResult Load(GameLaunchOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.GamePath))
        {
            errors.Add("A game path is required.");
            return new GameLoadResult(false, null, errors);
        }

        var gameRootPath = Path.GetFullPath(options.GamePath);
        if (!Directory.Exists(gameRootPath))
        {
            errors.Add($"Game path does not exist: {gameRootPath}");
            return new GameLoadResult(false, null, errors);
        }

        foreach (var asset in RequiredAssets)
        {
            var assetPath = Path.Combine(gameRootPath, asset);
            if (!File.Exists(assetPath))
            {
                errors.Add($"Missing required DOS asset: {assetPath}");
            }
        }

        var configPath = Path.Combine(gameRootPath, DosRuntimeConfig.ConfigFileName);
        DosRuntimeConfig? config = null;

        if (!File.Exists(configPath))
        {
            errors.Add($"Missing runtime config file: {configPath}");
        }
        else
        {
            try
            {
                var raw = File.ReadAllText(configPath);
                config = JsonSerializer.Deserialize<DosRuntimeConfig>(raw, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config is null)
                {
                    errors.Add($"Runtime config could not be parsed: {configPath}");
                }
                else if (string.IsNullOrWhiteSpace(config.EmulatorType))
                {
                    errors.Add("Runtime config must include emulatorType.");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to parse runtime config '{configPath}': {ex.Message}");
            }
        }

        if (config is not null && config.EmulatorType.Equals("wrapper", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(config.EmulatorExecutable))
            {
                errors.Add("Runtime config must include emulatorExecutable when emulatorType is 'wrapper'.");
            }
        }

        var requiredExecutable = config?.RequiredExecutable ?? "GAME.EXE";
        var executablePath = Path.Combine(gameRootPath, requiredExecutable);
        if (!File.Exists(executablePath))
        {
            errors.Add($"Missing required executable: {executablePath}");
        }

        if (!string.IsNullOrWhiteSpace(options.LoadStatePath))
        {
            var loadStatePath = Path.GetFullPath(options.LoadStatePath);
            if (!File.Exists(loadStatePath))
            {
                errors.Add($"Load-state file does not exist: {loadStatePath}");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.SaveStatePath))
        {
            var saveStatePath = Path.GetFullPath(options.SaveStatePath);
            var parentDirectory = Path.GetDirectoryName(saveStatePath);
            if (string.IsNullOrWhiteSpace(parentDirectory) || !Directory.Exists(parentDirectory))
            {
                errors.Add($"Save-state directory does not exist: {parentDirectory}");
            }
        }

        if (errors.Count > 0 || config is null)
        {
            return new GameLoadResult(false, null, errors);
        }

        var package = new GamePackage(
            GameRootPath: gameRootPath,
            GameExecutablePath: executablePath,
            DosConfigPath: Path.Combine(gameRootPath, "DOSBOX.CONF"),
            RuntimeConfig: config,
            SaveStatePath: options.SaveStatePath is null ? null : Path.GetFullPath(options.SaveStatePath),
            LoadStatePath: options.LoadStatePath is null ? null : Path.GetFullPath(options.LoadStatePath));

        return new GameLoadResult(true, package, Array.Empty<string>());
    }
}
