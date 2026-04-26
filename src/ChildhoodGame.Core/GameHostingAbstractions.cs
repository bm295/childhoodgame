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

public interface IWindowCaptureRuntime
{
    Task<GameWindowCapture> CaptureWindowAsync(CancellationToken cancellationToken = default);
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
    [property: JsonPropertyName("startupInput")] string[]? StartupInput)
{
    public const string ConfigFileName = "game.config.json";
}

public sealed record GameWindowCapture(int Width, int Height, int[] ArgbPixels)
{
    public int GetPixelArgb(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            return 0;
        }

        return ArgbPixels[(y * Width) + x];
    }
}
