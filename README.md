# ChildhoodGame (.NET)

This repository is a C#/.NET solution that validates and launches DOS games through a simple Windows desktop launcher.

## Projects

- `src/ChildhoodGame.Runner`: Windows launcher for browsing a game folder, validating the package, and starting the runtime.
- `src/ChildhoodGame.Core`: Loader/runtime abstractions and DOS emulator strategy integration.

## SDK and framework version

- Target framework: **.NET 10 (`net10.0`)** for core libraries and **.NET 10 Windows (`net10.0-windows`)** for the desktop launcher.
- Language version: **C# 10** via explicit `<LangVersion>10.0</LangVersion>`.
- `global.json` pins SDK version **10.0.103** with `rollForward: latestFeature`.

## Launch the UI

```bash
dotnet run --project src/ChildhoodGame.Runner/ChildhoodGame.Runner.csproj
```

The launcher opens a desktop window where you can:

- browse for the game folder
- optionally choose save-state and load-state files
- validate the package before launch
- start and stop the runtime from the same window

You can also drag and drop the target game folder onto the launcher window.

## Auto-win simulation mode

The runner also supports a deterministic automation loop:

```bash
dotnet run --project src/ChildhoodGame.Runner/ChildhoodGame.Runner.csproj -- --autowin --steps 20 --delay-ms 0
```

`--autowin` runs a strategy-driven state loop that repeatedly:

- reads game state from runtime
- selects next action(s)
- applies input commands
- evaluates win conditions (score, level, objective flags)

The console output is stable and step-based so it can be used in CI logs.

## Game folder requirements

The loader validates that the selected folder contains:

- one `.conf` file
- one `.exe` file
- one `.json` file

When conventional names are present, the loader prefers `DOSBOX.CONF` and `game.config.json`.
Otherwise, it uses the first matching file alphabetically. If compatible runtime settings are missing,
the launcher defaults to wrapper mode with `dosbox`.

`game.config.json` example:

A ready-to-use sample file is available at `samples/game.config.json`.

```json
{
  "emulatorType": "wrapper",
  "emulatorExecutable": "C:\\Program Files (x86)\\DOSBox-0.74-3\\DOSBox.exe",
  "emulatorArguments": "-conf \"{config}\" -c \"mount c {gameRoot}\" -c \"c:\" -c \"{exe}\"",
  "startupInput": ["ENTER"]
}
```

Use `emulatorType: "embedded"` for embedded-core mode.

In `emulatorArguments`, `{config}` is replaced with the selected `.conf` file path, `{gameRoot}` is replaced with the selected game folder, and `{exe}` is replaced with the executable file name, such as `GAME.EXE`.
