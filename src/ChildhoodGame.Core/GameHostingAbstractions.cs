using System.Text.Json.Serialization;

namespace ChildhoodGame.Core;

public interface IGameLoader
{
    GameLoadResult Load(GameLaunchOptions options);
}

public interface IGameRuntime : IAsyncDisposable
{
    bool IsRunning { get; }

    Task StartAsync(GamePackage gamePackage, CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task SendInputAsync(string inputCommand, CancellationToken cancellationToken = default);
}

public interface IInputController
{
    Task SendCommandAsync(string command, CancellationToken cancellationToken = default);
}

public sealed record GameLaunchOptions(
    string GamePath,
    bool Run,
    string? SaveStatePath = null,
    string? LoadStatePath = null);

public sealed record GameLoadResult(
    bool IsValid,
    GamePackage? GamePackage,
    IReadOnlyList<string> Errors);

public sealed record GamePackage(
    string GameRootPath,
    string GameExecutablePath,
    string DosConfigPath,
    DosRuntimeConfig RuntimeConfig,
    string? SaveStatePath,
    string? LoadStatePath);

public sealed record DosRuntimeConfig(
    [property: JsonPropertyName("emulatorType")] string EmulatorType,
    [property: JsonPropertyName("emulatorExecutable")] string? EmulatorExecutable,
    [property: JsonPropertyName("emulatorArguments")] string? EmulatorArguments,
    [property: JsonPropertyName("startupInput")] string[]? StartupInput,
    [property: JsonPropertyName("requiredExecutable")] string? RequiredExecutable)
{
    public const string ConfigFileName = "game.config.json";
}
