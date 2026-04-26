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

    private static string CreateTemporaryGameFolder()
    {
        var path = Path.Combine(Path.GetTempPath(), "ChildhoodGameTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
