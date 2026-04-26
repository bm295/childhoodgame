using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ChildhoodGame.Core;

public interface IDosEmulatorStrategy : IAsyncDisposable
{
    bool IsRunning { get; }

    Task StartAsync(GamePackage gamePackage, CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task SendInputAsync(string inputCommand, CancellationToken cancellationToken = default);
}

public sealed class DosGameRuntime : IGameRuntime, IWindowCaptureRuntime
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

    public Task<GameWindowCapture> CaptureWindowAsync(CancellationToken cancellationToken = default)
    {
        if (strategy is null)
        {
            throw new InvalidOperationException("Runtime has not been started.");
        }

        if (strategy is not IWindowCaptureRuntime captureRuntime)
        {
            throw new NotSupportedException("The active runtime does not expose window capture.");
        }

        return captureRuntime.CaptureWindowAsync(cancellationToken);
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

public sealed class WrapperProcessDosEmulatorStrategy : IDosEmulatorStrategy, IWindowCaptureRuntime
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
            CreateNoWindow = false
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

        if (TryParseWaitCommand(inputCommand, out var delayMilliseconds))
        {
            await Task.Delay(delayMilliseconds, cancellationToken);
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Wrapper input injection is only supported on Windows.");
        }

        var windowHandle = await WaitForMainWindowHandleAsync(process, cancellationToken);
        if (windowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Emulator window was not available for keyboard input.");
        }

        WindowsKeyboardInput.SendCommand(windowHandle, inputCommand);
    }

    public async Task<GameWindowCapture> CaptureWindowAsync(CancellationToken cancellationToken = default)
    {
        if (process is null || process.HasExited)
        {
            throw new InvalidOperationException("Emulator process is not running.");
        }

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Wrapper window capture is only supported on Windows.");
        }

        var windowHandle = await WaitForMainWindowHandleAsync(process, cancellationToken);
        if (windowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Emulator window was not available for capture.");
        }

        return WindowsWindowCapture.CaptureClientArea(windowHandle);
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
        args = args.Replace("{gameRoot}", gamePackage.GameRootPath, StringComparison.OrdinalIgnoreCase);
        args = args.Replace("{exe}", Path.GetFileName(gamePackage.GameExecutablePath), StringComparison.OrdinalIgnoreCase);

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

    private static bool TryParseWaitCommand(string inputCommand, out int delayMilliseconds)
    {
        delayMilliseconds = 0;
        var trimmed = inputCommand.Trim();
        if (!trimmed.StartsWith("WAIT:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(trimmed["WAIT:".Length..], out delayMilliseconds) && delayMilliseconds >= 0;
    }

    private static async Task<IntPtr> WaitForMainWindowHandleAsync(
        Process targetProcess,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (targetProcess.HasExited)
            {
                return IntPtr.Zero;
            }

            targetProcess.Refresh();
            if (targetProcess.MainWindowHandle != IntPtr.Zero)
            {
                return targetProcess.MainWindowHandle;
            }

            await Task.Delay(100, cancellationToken);
        }

        targetProcess.Refresh();
        return targetProcess.MainWindowHandle;
    }
}

internal static class WindowsKeyboardInput
{
    private const int ShowRestore = 9;
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const byte ShiftKey = 0x10;

    public static void SendCommand(IntPtr windowHandle, string command)
    {
        ShowWindow(windowHandle, ShowRestore);
        SetForegroundWindow(windowHandle);
        Thread.Sleep(75);

        var keys = ResolveKeys(command);
        foreach (var key in keys)
        {
            SendKey(key);
            Thread.Sleep(35);
        }
    }

    private static IEnumerable<KeyStroke> ResolveKeys(string command)
    {
        var trimmed = command.Trim();
        if (TryResolveNamedKey(trimmed, out var namedKey))
        {
            yield return namedKey;
            yield break;
        }

        foreach (var character in trimmed)
        {
            var virtualKey = VkKeyScan(character);
            if (virtualKey == -1)
            {
                throw new InvalidOperationException($"Unsupported input command character: '{character}'.");
            }

            var keyCode = (byte)(virtualKey & 0xff);
            var modifiers = (byte)((virtualKey >> 8) & 0xff);
            yield return new KeyStroke(keyCode, (modifiers & 1) == 1);
        }
    }

    private static bool TryResolveNamedKey(string command, out KeyStroke key)
    {
        key = command.ToUpperInvariant() switch
        {
            "ENTER" => new KeyStroke(0x0D),
            "RETURN" => new KeyStroke(0x0D),
            "ESC" => new KeyStroke(0x1B),
            "ESCAPE" => new KeyStroke(0x1B),
            "SPACE" => new KeyStroke(0x20),
            "TAB" => new KeyStroke(0x09),
            "BACKSPACE" => new KeyStroke(0x08),
            _ => default
        };

        return key.VirtualKey != 0;
    }

    private static void SendKey(KeyStroke key)
    {
        if (key.Shift)
        {
            SendVirtualKey(ShiftKey, keyUp: false);
        }

        SendVirtualKey(key.VirtualKey, keyUp: false);
        SendVirtualKey(key.VirtualKey, keyUp: true);

        if (key.Shift)
        {
            SendVirtualKey(ShiftKey, keyUp: true);
        }
    }

    private static void SendVirtualKey(byte virtualKey, bool keyUp)
    {
        var flags = keyUp ? KeyEventKeyUp : 0;
        var inputs = new[]
        {
            new Input
            {
                Type = InputKeyboard,
                Union = new InputUnion
                {
                    KeyboardInput = new KeyboardInput
                    {
                        VirtualKey = virtualKey,
                        Flags = flags
                    }
                }
            }
        };

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sent != inputs.Length)
        {
            throw new InvalidOperationException($"Failed to send keyboard input. Win32 error: {Marshal.GetLastWin32Error()}");
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr windowHandle);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr windowHandle, int commandShow);

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char character);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

    private readonly record struct KeyStroke(byte VirtualKey, bool Shift = false);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput MouseInput;

        [FieldOffset(0)]
        public KeyboardInput KeyboardInput;

        [FieldOffset(0)]
        public HardwareInput HardwareInput;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        public uint Message;
        public ushort ParamLow;
        public ushort ParamHigh;
    }
}

internal static class WindowsWindowCapture
{
    private const int Srccopy = 0x00CC0020;
    private const uint DibRgbColors = 0;
    private const ushort BitmapInfoHeaderSize = 40;
    private const ushort BitsPerPixel = 32;
    private const uint BiRgb = 0;

    public static GameWindowCapture CaptureClientArea(IntPtr windowHandle)
    {
        if (!GetClientRect(windowHandle, out var clientRect))
        {
            throw new InvalidOperationException($"Failed to read emulator client bounds. Win32 error: {Marshal.GetLastWin32Error()}");
        }

        var width = clientRect.Right - clientRect.Left;
        var height = clientRect.Bottom - clientRect.Top;
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("Emulator client area is empty.");
        }

        var windowDeviceContext = GetDC(windowHandle);
        if (windowDeviceContext == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to read emulator device context. Win32 error: {Marshal.GetLastWin32Error()}");
        }

        var memoryDeviceContext = IntPtr.Zero;
        var bitmap = IntPtr.Zero;
        var previousObject = IntPtr.Zero;

        try
        {
            memoryDeviceContext = CreateCompatibleDC(windowDeviceContext);
            bitmap = CreateCompatibleBitmap(windowDeviceContext, width, height);
            previousObject = SelectObject(memoryDeviceContext, bitmap);

            if (!BitBlt(memoryDeviceContext, 0, 0, width, height, windowDeviceContext, 0, 0, Srccopy))
            {
                throw new InvalidOperationException($"Failed to copy emulator window pixels. Win32 error: {Marshal.GetLastWin32Error()}");
            }

            var pixels = new int[width * height];
            var bitmapInfo = new BitmapInfo
            {
                Header = new BitmapInfoHeader
                {
                    Size = BitmapInfoHeaderSize,
                    Width = width,
                    Height = -height,
                    Planes = 1,
                    BitCount = BitsPerPixel,
                    Compression = BiRgb
                }
            };

            var lines = GetDIBits(
                memoryDeviceContext,
                bitmap,
                0,
                (uint)height,
                pixels,
                ref bitmapInfo,
                DibRgbColors);
            if (lines == 0)
            {
                throw new InvalidOperationException($"Failed to read emulator window pixels. Win32 error: {Marshal.GetLastWin32Error()}");
            }

            return new GameWindowCapture(width, height, pixels);
        }
        finally
        {
            if (previousObject != IntPtr.Zero)
            {
                SelectObject(memoryDeviceContext, previousObject);
            }

            if (bitmap != IntPtr.Zero)
            {
                DeleteObject(bitmap);
            }

            if (memoryDeviceContext != IntPtr.Zero)
            {
                DeleteDC(memoryDeviceContext);
            }

            ReleaseDC(windowHandle, windowDeviceContext);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr windowHandle, out Rect rectangle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDC(IntPtr windowHandle);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr windowHandle, IntPtr deviceContext);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr deviceContext);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr deviceContext, int width, int height);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr deviceContext, IntPtr gdiObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool BitBlt(
        IntPtr destinationDeviceContext,
        int x,
        int y,
        int width,
        int height,
        IntPtr sourceDeviceContext,
        int sourceX,
        int sourceY,
        int rasterOperation);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern int GetDIBits(
        IntPtr deviceContext,
        IntPtr bitmap,
        uint startScan,
        uint scanLines,
        [Out] int[] bits,
        ref BitmapInfo bitmapInfo,
        uint usage);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr gdiObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr deviceContext);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;
        public uint Colors;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
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
