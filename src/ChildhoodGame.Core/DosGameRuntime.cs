using System.Diagnostics;

namespace ChildhoodGame.Core;

public interface IDosEmulatorStrategy : IAsyncDisposable
{
    bool IsRunning { get; }

    Task StartAsync(GamePackage gamePackage, CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task SendInputAsync(string inputCommand, CancellationToken cancellationToken = default);
}

public sealed class DosGameRuntime : IGameRuntime
{
    private readonly Func<DosRuntimeConfig, IDosEmulatorStrategy> strategyFactory;
    private IDosEmulatorStrategy? strategy;

    public DosGameRuntime(Func<DosRuntimeConfig, IDosEmulatorStrategy>? strategyFactory = null)
    {
        this.strategyFactory = strategyFactory ?? CreateDefaultStrategy;
    }

    public bool IsRunning => strategy?.IsRunning == true;

    public async Task StartAsync(GamePackage gamePackage, CancellationToken cancellationToken = default)
    {
        strategy = strategyFactory(gamePackage.RuntimeConfig);
        await strategy.StartAsync(gamePackage, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (strategy is null)
        {
            return Task.CompletedTask;
        }

        return strategy.StopAsync(cancellationToken);
    }

    public Task SendInputAsync(string inputCommand, CancellationToken cancellationToken = default)
    {
        if (strategy is null)
        {
            throw new InvalidOperationException("Runtime has not been started.");
        }

        return strategy.SendInputAsync(inputCommand, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (strategy is not null)
        {
            await strategy.DisposeAsync();
        }
    }

    private static IDosEmulatorStrategy CreateDefaultStrategy(DosRuntimeConfig config) =>
        config.EmulatorType.Equals("embedded", StringComparison.OrdinalIgnoreCase)
            ? new EmbeddedCoreDosEmulatorStrategy()
            : new WrapperProcessDosEmulatorStrategy(config.EmulatorExecutable!, config.EmulatorArguments);
}

public sealed class WrapperProcessDosEmulatorStrategy : IDosEmulatorStrategy
{
    private readonly string emulatorExecutable;
    private readonly string? emulatorArguments;
    private Process? process;

    public WrapperProcessDosEmulatorStrategy(string emulatorExecutable, string? emulatorArguments)
    {
        this.emulatorExecutable = emulatorExecutable;
        this.emulatorArguments = emulatorArguments;
    }

    public bool IsRunning => process is { HasExited: false };

    public Task StartAsync(GamePackage gamePackage, CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = emulatorExecutable,
            Arguments = BuildArguments(gamePackage),
            WorkingDirectory = gamePackage.GameRootPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start emulator process: {emulatorExecutable}");

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (process is null)
        {
            return;
        }

        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(cancellationToken);
        }
    }

    public async Task SendInputAsync(string inputCommand, CancellationToken cancellationToken = default)
    {
        if (process is null || process.HasExited)
        {
            throw new InvalidOperationException("Emulator process is not running.");
        }

        await process.StandardInput.WriteLineAsync(inputCommand.AsMemory(), cancellationToken);
        await process.StandardInput.FlushAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        process?.Dispose();
    }

    private string BuildArguments(GamePackage gamePackage)
    {
        var args = emulatorArguments ?? string.Empty;
        args = args.Replace("{config}", gamePackage.DosConfigPath, StringComparison.OrdinalIgnoreCase);
        args = args.Replace("{exe}", gamePackage.GameExecutablePath, StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(gamePackage.LoadStatePath))
        {
            args += $" --loadstate \"{gamePackage.LoadStatePath}\"";
        }

        if (!string.IsNullOrWhiteSpace(gamePackage.SaveStatePath))
        {
            args += $" --savestate \"{gamePackage.SaveStatePath}\"";
        }

        return args.Trim();
    }
}

public sealed class EmbeddedCoreDosEmulatorStrategy : IDosEmulatorStrategy
{
    public bool IsRunning { get; private set; }

    public Task StartAsync(GamePackage gamePackage, CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public Task SendInputAsync(string inputCommand, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException("Embedded emulator core is not running.");
        }

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsRunning = false;
        return ValueTask.CompletedTask;
    }
}

public sealed class RuntimeInputController : IInputController
{
    private readonly IGameRuntime runtime;

    public RuntimeInputController(IGameRuntime runtime)
    {
        this.runtime = runtime;
    }

    public Task SendCommandAsync(string command, CancellationToken cancellationToken = default) =>
        runtime.SendInputAsync(command, cancellationToken);
}
