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
        var maxSteps = ParseOption(args, "--steps", defaultValue: 20);
        var delayMs = ParseOption(args, "--delay-ms", defaultValue: 0);

        await using var runtime = new MockedGameStateRuntime(
            new GameRuntimeState(
                Score: 0,
                Level: 1,
                ObjectiveFlags: new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)));

        var package = new GamePackage(
            GameRootPath: Environment.CurrentDirectory,
            GameExecutablePath: "MOCK.EXE",
            DosConfigPath: "DOSBOX.CONF",
            RuntimeConfig: new DosRuntimeConfig("embedded", null, null, null),
            SaveStatePath: null,
            LoadStatePath: null);

        await runtime.StartAsync(package);

        var detectors = new IWinConditionDetector[]
        {
            new ScoreWinConditionDetector(50),
            new LevelWinConditionDetector(3),
            new ObjectiveFlagWinConditionDetector(DeterministicAutoWinActionStrategy.BossKeyObjective)
        };

        var loop = new AutoWinLoop(runtime, new DeterministicAutoWinActionStrategy(), detectors);
        var result = await loop.RunAsync(new AutoWinOptions(maxSteps, delayMs));

        Console.WriteLine("AUTOWIN MODE START");
        foreach (var entry in result.Progress)
        {
            var actionsText = entry.Actions.Count == 0 ? "<none>" : string.Join(',', entry.Actions);
            var satisfiedText = entry.SatisfiedConditions.Count == 0 ? "<none>" : string.Join('|', entry.SatisfiedConditions);
            Console.WriteLine(
                $"STEP={entry.Step};STATE={entry.StateAfterActions.Score}/{entry.StateAfterActions.Level};ACTIONS={actionsText};CONDITIONS={satisfiedText};WIN={(entry.IsWin ? "yes" : "no")}");
        }

        Console.WriteLine(result.IsWin
            ? $"AUTOWIN RESULT: WIN in {result.StepsExecuted} step(s)."
            : $"AUTOWIN RESULT: INCOMPLETE after {result.StepsExecuted} step(s).");

        await runtime.StopAsync();
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
