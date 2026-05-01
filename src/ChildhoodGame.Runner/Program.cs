using ChildhoodGame.Core;
using ChildhoodGame.Core.Strategy;

namespace ChildhoodGame.Runner;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Any(arg => arg.Equals("--autowin", StringComparison.OrdinalIgnoreCase)))
        {
            RunAutoWinModeAsync(args).GetAwaiter().GetResult();
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new GameLauncherForm());
    }

    private static async Task RunAutoWinModeAsync(string[] args)
    {
        var maxSteps = ParseOption(args, "--steps", defaultValue: 100);
        var delayMs = ParseOption(args, "--delay-ms", defaultValue: 0);

        var package = new GamePackage(
            GameRootPath: Environment.CurrentDirectory,
            GameExecutablePath: "MOCK.EXE",
            DosConfigPath: "DOSBOX.CONF",
            RuntimeConfig: new DosRuntimeConfig("embedded", null, null, null),
            SaveStatePath: null,
            LoadStatePath: null);

        // Detect the game type from the folder name
        var gameName = GameStrategyRegistry.DetectGameNameFromPath(package.GameRootPath);
        var registry = new GameStrategyRegistry();

        try
        {
            var provider = registry.GetProvider(gameName);
            var options = new AutoWinOptions(maxSteps, delayMs);
            var isWin = await provider.ExecuteAutoWinAsync(
                package,
                options,
                Console.WriteLine,
                CancellationToken.None);

            Environment.ExitCode = isWin ? 0 : 1;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Environment.ExitCode = -1;
        }
    }

    private static int ParseOption(string[] args, string optionName, int defaultValue)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!args[i].Equals(optionName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(args[i + 1], out var parsed) && parsed > 0)
            {
                return parsed;
            }
        }

        return defaultValue;
    }
}
