using ChildhoodGame.Core;
using ChildhoodGame.Core.Strategy;
using ChildhoodGame.Core.Strategy.TicTacToe;

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
        var difficulty = ParseDifficultyOption(args, "--difficulty", TicTacToeDifficulty.Hard);

        await using var runtime = new MockedTicTacToeRuntime();

        var package = new GamePackage(
            GameRootPath: Environment.CurrentDirectory,
            GameExecutablePath: "MOCK.EXE",
            DosConfigPath: "DOSBOX.CONF",
            RuntimeConfig: new DosRuntimeConfig("embedded", null, null, null),
            SaveStatePath: null,
            LoadStatePath: null);

        await runtime.StartAsync(package);

        var detectors = new IWinConditionDetector<TicTacToeGameState>[]
        {
            new TicTacToePlayerWinConditionDetector()
        };

        var loop = new AutoWinLoop<TicTacToeGameState>(
            runtime,
            new PaidBetaAlwaysWinActionStrategy(difficulty),
            detectors);
        var result = await loop.RunAsync(new AutoWinOptions(maxSteps, delayMs));

        Console.WriteLine($"TICTACTOE AUTOWIN MODE START;DIFFICULTY={(int)difficulty}");
        foreach (var entry in result.Progress)
        {
            var actionsText = entry.Actions.Count == 0 ? "<none>" : string.Join(',', entry.Actions);
            var satisfiedText = entry.SatisfiedConditions.Count == 0 ? "<none>" : string.Join('|', entry.SatisfiedConditions);
            Console.WriteLine(
                $"STEP={entry.Step};RESTARTS={entry.StateAfterActions.RestartCount};BOARD={entry.StateAfterActions.Board};OUTCOME={entry.StateAfterActions.Outcome};ACTIONS={actionsText};CONDITIONS={satisfiedText};WIN={(entry.IsWin ? "yes" : "no")}");
        }

        Console.WriteLine(result.IsWin
            ? $"TICTACTOE AUTOWIN RESULT: WIN in {result.StepsExecuted} step(s)."
            : $"TICTACTOE AUTOWIN RESULT: INCOMPLETE after {result.StepsExecuted} step(s).");

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

    private static TicTacToeDifficulty ParseDifficultyOption(
        string[] args,
        string optionName,
        TicTacToeDifficulty defaultValue)
    {
        var parsed = (int)defaultValue;
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!args[i].Equals(optionName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!int.TryParse(args[i + 1], out parsed))
            {
                parsed = (int)defaultValue;
            }
        }

        return Enum.IsDefined(typeof(TicTacToeDifficulty), parsed)
            ? (TicTacToeDifficulty)parsed
            : defaultValue;
    }
}
