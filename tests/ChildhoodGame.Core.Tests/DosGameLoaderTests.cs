using ChildhoodGame.Core;
using Xunit;

namespace ChildhoodGame.Core.Tests;

public sealed class DosGameLoaderTests
{
    [Fact]
    public void Load_AcceptsGameFolder_WithOneExeAndOneJsonOnly()
    {
        var gameFolder = CreateTemporaryGameFolder();
        try
        {
            File.WriteAllText(Path.Combine(gameFolder, "PAIDBETA.EXE"), string.Empty);
            File.WriteAllText(
                Path.Combine(gameFolder, DosRuntimeConfig.ConfigFileName),
                "{\n"
                + "  \"emulatorType\": \"wrapper\",\n"
                + "  \"emulatorExecutable\": \"dosbox\",\n"
                + "  \"emulatorArguments\": \"-conf \\\"C:\\\\Users\\\\T14\\\\AppData\\\\Local\\\\DOSBox\\\\dosbox-0.74-3.conf\\\" -c \\\"mount c {gameRoot}\\\" -c \\\"c:\\\" -c \\\"{exe}\\\"\"\n"
                + "}\n");

            var result = new DosGameLoader().Load(new GameLaunchOptions(gameFolder, Run: false));

            Assert.True(result.IsValid);
            Assert.NotNull(result.GamePackage);
            Assert.Equal(
                @"C:\Users\T14\AppData\Local\DOSBox\dosbox-0.74-3.conf",
                result.GamePackage.DosConfigPath);
        }
        finally
        {
            Directory.Delete(gameFolder, recursive: true);
        }
    }

    [Fact]
    public void Load_LoadsConfigFromApplicationDirectory_WhenNotInGameFolder()
    {
        var rootFolder = CreateTemporaryGameFolder();
        var gameFolder = Path.Combine(rootFolder, "snake");
        Directory.CreateDirectory(gameFolder);
        
        try
        {
            // Create game executable in game folder
            File.WriteAllText(Path.Combine(gameFolder, "SNAKE.EXE"), string.Empty);
            
            // Create config in application directory (simulating the Runner project directory)
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            File.WriteAllText(
                Path.Combine(appDirectory, "game.config.json"),
                "{\n"
                + "  \"emulatorType\": \"wrapper\",\n"
                + "  \"emulatorExecutable\": \"C:\\\\Program Files (x86)\\\\DOSBox-0.74-3\\\\DOSBox.exe\",\n"
                + "  \"emulatorArguments\": \"-conf \\\"C:\\\\Users\\\\T14\\\\AppData\\\\Local\\\\DOSBox\\\\dosbox-0.74-3.conf\\\" -c \\\"mount c {gameRoot}\\\" -c \\\"c:\\\" -c \\\"{exe}\\\"\"\n"
                + "}\n");

            var result = new DosGameLoader().Load(new GameLaunchOptions(gameFolder, Run: false));

            Assert.True(result.IsValid);
            Assert.NotNull(result.GamePackage);
            Assert.Equal(
                "C:\\Program Files (x86)\\DOSBox-0.74-3\\DOSBox.exe",
                result.GamePackage.RuntimeConfig.EmulatorExecutable);
        }
        finally
        {
            Directory.Delete(rootFolder, recursive: true);
            // Clean up the test config file
            var testConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game.config.json");
            if (File.Exists(testConfigPath))
            {
                File.Delete(testConfigPath);
            }
        }
    }

    private static string CreateTemporaryGameFolder()
    {
        var path = Path.Combine(Path.GetTempPath(), "ChildhoodGameTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
