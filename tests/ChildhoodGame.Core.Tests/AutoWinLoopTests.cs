using ChildhoodGame.Core;
using ChildhoodGame.Core.Strategy;

namespace ChildhoodGame.Core.Tests;

public sealed class AutoWinLoopTests
{
    [Fact]
    public async Task MockedRuntime_TransitionsState_FromAppliedInputs()
    {
        await using var runtime = new MockedGameStateRuntime(new GameRuntimeState(0, 1, new Dictionary<string, bool>()));
        await runtime.StartAsync(BuildPackage());

        await runtime.SendInputAsync("GAIN_SCORE");
        await runtime.SendInputAsync("ADVANCE_LEVEL");
        await runtime.SendInputAsync("COMPLETE_OBJECTIVE:boss_key");

        var state = await runtime.ReadStateAsync();

        Assert.Equal(10, state.Score);
        Assert.Equal(2, state.Level);
        Assert.True(state.TryGetObjectiveFlag("boss_key", out var completed) && completed);
    }

    [Fact]
    public async Task AutoWinLoop_ReachesWin_WhenAllDetectorsSatisfied()
    {
        await using var runtime = new MockedGameStateRuntime(new GameRuntimeState(0, 1, new Dictionary<string, bool>()));
        await runtime.StartAsync(BuildPackage());

        var detectors = new IWinConditionDetector[]
        {
            new ScoreWinConditionDetector(50),
            new LevelWinConditionDetector(3),
            new ObjectiveFlagWinConditionDetector(DeterministicAutoWinActionStrategy.BossKeyObjective)
        };

        var loop = new AutoWinLoop(runtime, new DeterministicAutoWinActionStrategy(), detectors);
        var result = await loop.RunAsync(new AutoWinOptions(MaxSteps: 20));

        Assert.True(result.IsWin);
        Assert.Equal(8, result.StepsExecuted);
        Assert.True(result.Progress.Last().IsWin);
    }

    private static GamePackage BuildPackage() =>
        new(
            GameRootPath: "/tmp",
            GameExecutablePath: "MOCK.EXE",
            DosConfigPath: "DOSBOX.CONF",
            RuntimeConfig: new DosRuntimeConfig("embedded", null, null, null),
            SaveStatePath: null,
            LoadStatePath: null);
}
