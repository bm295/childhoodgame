using System.Text.Json;

namespace ChildhoodGame.Core;

public sealed class DosGameLoader : IGameLoader
{
    private const string DefaultEmulatorType = "wrapper";
    private const string DefaultEmulatorExecutable = "dosbox";
    private const string DefaultEmulatorArguments = "-conf \"{config}\" -c \"mount c {gameRoot}\" -c \"c:\" -c \"{exe}\"";

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

        var dosConfigPath = FindFileByExtension(gameRootPath, ".conf", "DOSBOX.CONF");
        if (dosConfigPath is null)
        {
            errors.Add($"Game folder must contain at least one .conf file: {gameRootPath}");
        }

        var configPath = FindFileByExtension(gameRootPath, ".json", DosRuntimeConfig.ConfigFileName);
        if (configPath is null)
        {
            errors.Add($"Game folder must contain at least one .json file: {gameRootPath}");
        }

        DosRuntimeConfig? config = null;
        if (configPath is not null)
        {
            config = TryReadRuntimeConfig(configPath);
        }

        var executablePath = FindSingleFileByExtension(gameRootPath, ".exe", errors);

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

        if (errors.Count > 0 || configPath is null || dosConfigPath is null || executablePath is null)
        {
            return new GameLoadResult(false, null, errors);
        }

        config = NormalizeRuntimeConfig(config);

        var package = new GamePackage(
            GameRootPath: gameRootPath,
            GameExecutablePath: executablePath,
            DosConfigPath: dosConfigPath,
            RuntimeConfig: config,
            SaveStatePath: options.SaveStatePath is null ? null : Path.GetFullPath(options.SaveStatePath),
            LoadStatePath: options.LoadStatePath is null ? null : Path.GetFullPath(options.LoadStatePath));

        return new GameLoadResult(true, package, Array.Empty<string>());
    }

    private static string? FindFileByExtension(string directoryPath, string extension, string? preferredFileName = null)
    {
        var matches = Directory.EnumerateFiles(directoryPath)
            .Where(path => Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (matches.Length == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(preferredFileName))
        {
            var preferredMatch = matches.FirstOrDefault(path =>
                Path.GetFileName(path).Equals(preferredFileName, StringComparison.OrdinalIgnoreCase));
            if (preferredMatch is not null)
            {
                return preferredMatch;
            }
        }

        return matches[0];
    }

    private static string? FindSingleFileByExtension(string directoryPath, string extension, List<string> errors)
    {
        var matches = Directory.EnumerateFiles(directoryPath)
            .Where(path => Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (matches.Length == 1)
        {
            return matches[0];
        }

        if (matches.Length == 0)
        {
            errors.Add($"Game folder must contain exactly one {extension} file: {directoryPath}");
        }
        else
        {
            errors.Add($"Game folder must contain only one {extension} file, but found {matches.Length}: {directoryPath}");
        }

        return null;
    }

    private static DosRuntimeConfig? TryReadRuntimeConfig(string configPath)
    {
        try
        {
            var raw = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<DosRuntimeConfig>(raw, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static DosRuntimeConfig NormalizeRuntimeConfig(DosRuntimeConfig? config)
    {
        config ??= new DosRuntimeConfig(null!, null, null, null);

        var emulatorType = string.IsNullOrWhiteSpace(config.EmulatorType)
            ? DefaultEmulatorType
            : config.EmulatorType;

        var emulatorExecutable = config.EmulatorExecutable;
        if (emulatorType.Equals(DefaultEmulatorType, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(emulatorExecutable))
        {
            emulatorExecutable = DefaultEmulatorExecutable;
        }

        var emulatorArguments = string.IsNullOrWhiteSpace(config.EmulatorArguments)
            ? DefaultEmulatorArguments
            : config.EmulatorArguments;

        return config with
        {
            EmulatorType = emulatorType,
            EmulatorExecutable = emulatorExecutable,
            EmulatorArguments = emulatorArguments
        };
    }
}
