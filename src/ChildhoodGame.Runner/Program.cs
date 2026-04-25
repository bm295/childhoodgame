using ChildhoodGame.Core;

var logger = new ConsoleGameLogger();

var parsed = CliOptions.Parse(args);
if (!parsed.IsValid)
{
    foreach (var error in parsed.Errors)
    {
        logger.Error(error);
    }

    CliOptions.PrintUsage();
    return 1;
}

var launchOptions = parsed.Options!;
var loader = new DosGameLoader();
var loadResult = loader.Load(launchOptions);

if (!loadResult.IsValid || loadResult.GamePackage is null)
{
    logger.Error("Game launch validation failed.");
    foreach (var error in loadResult.Errors)
    {
        logger.Error(error);
    }

    return 2;
}

logger.Info($"Validated game package at '{loadResult.GamePackage.GameRootPath}'.");

if (!launchOptions.Run)
{
    logger.Info("Validation complete. Use --run to launch the game runtime.");
    return 0;
}

try
{
    await using var runtime = new DosGameRuntime();
    var inputController = new RuntimeInputController(runtime);

    logger.Info("Starting DOS runtime.");
    await runtime.StartAsync(loadResult.GamePackage);

    var startupInput = loadResult.GamePackage.RuntimeConfig.StartupInput;
    if (startupInput is { Length: > 0 })
    {
        foreach (var command in startupInput)
        {
            await inputController.SendCommandAsync(command);
            logger.Info($"Startup input sent: {command}");
        }
    }

    logger.Info("Runtime started successfully. Press ENTER to stop.");
    Console.ReadLine();

    await runtime.StopAsync();
    logger.Info("Runtime stopped cleanly.");
    return 0;
}
catch (Exception ex)
{
    logger.Error($"Runtime crash detected: {ex.Message}");
    logger.Error(ex.ToString());
    return 3;
}

internal sealed class CliOptions
{
    public GameLaunchOptions? Options { get; init; }

    public bool IsValid => Errors.Count == 0 && Options is not null;

    public List<string> Errors { get; } = new();

    public static CliOptions Parse(string[] args)
    {
        var result = new CliOptions();

        string? gamePath = null;
        bool run = false;
        string? saveState = null;
        string? loadState = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--game-path":
                    if (!TryReadValue(args, ref i, out gamePath))
                    {
                        result.Errors.Add("--game-path requires a value.");
                    }

                    break;
                case "--run":
                    run = true;
                    break;
                case "--save-state":
                    if (!TryReadValue(args, ref i, out saveState))
                    {
                        result.Errors.Add("--save-state requires a value.");
                    }

                    break;
                case "--load-state":
                    if (!TryReadValue(args, ref i, out loadState))
                    {
                        result.Errors.Add("--load-state requires a value.");
                    }

                    break;
                case "--help":
                case "-h":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
                default:
                    result.Errors.Add($"Unknown argument: {args[i]}");
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(gamePath))
        {
            result.Errors.Add("--game-path is required.");
        }

        if (result.Errors.Count == 0)
        {
            result.Options = new GameLaunchOptions(gamePath!, run, saveState, loadState);
        }

        return result;
    }

    public static void PrintUsage()
    {
        Console.WriteLine("Usage: ChildhoodGame.Runner --game-path <path> [--run] [--save-state <path>] [--load-state <path>]");
    }

    private static bool TryReadValue(string[] args, ref int index, out string? value)
    {
        if (index + 1 >= args.Length)
        {
            value = null;
            return false;
        }

        value = args[++index];
        return true;
    }
}

internal sealed class ConsoleGameLogger
{
    public void Info(string message) => Write("INFO", message);

    public void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        Console.Error.WriteLine($"{DateTimeOffset.UtcNow:O} [{level}] {message}");
    }
}
