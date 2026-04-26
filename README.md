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

## PAIDBETA auto-win simulation mode

The runner also supports a PAIDBETA-aware TicTacToe automation loop:

```bash
dotnet run --project src/ChildhoodGame.Runner/ChildhoodGame.Runner.csproj -- --autowin --steps 100 --delay-ms 0
```

`--difficulty` maps to the DOS prompt: `0` easy, `1` medium, `2` hard. Auto-win defaults to hard mode because the PAIDBETA source has an exploitable hard-mode fork bug.

`--autowin` runs a Strategy-pattern state loop that repeatedly:

- reads game state from runtime
- selects next action(s) through an `IActionSelectionStrategy<TState>`
- applies input commands
- evaluates win conditions through `IWinConditionDetector<TState>`

The concrete PAIDBETA strategy models the C source's hard-mode computer logic:

- play center first
- if the computer's random first move is a side, force a fork and win
- if the computer's random first move is a corner, restart and retry because that branch is draw-only with best play

The console output is stable and step-based so it can be used in CI logs.

## Game folder requirements

The loader validates that the selected folder contains:

- one `.exe` file
- one `.json` file

When conventional names are present, the loader prefers `game.config.json`.
Otherwise, it uses the first matching JSON file alphabetically. If compatible runtime settings are missing,
the launcher defaults to wrapper mode with `dosbox` and `C:\Users\T14\AppData\Local\DOSBox\dosbox-0.74-3.conf`.

`game.config.json` example:

A ready-to-use sample file is available at `samples/game.config.json`.

```json
{
  "emulatorType": "wrapper",
  "emulatorExecutable": "C:\\Program Files (x86)\\DOSBox-0.74-3\\DOSBox.exe",
  "emulatorArguments": "-conf \"C:\\Users\\T14\\AppData\\Local\\DOSBox\\dosbox-0.74-3.conf\" -c \"mount c {gameRoot}\" -c \"c:\" -c \"{exe}\""
}
```

Use `emulatorType: "embedded"` for embedded-core mode.

In `emulatorArguments`, `{gameRoot}` is replaced with the selected game folder, and `{exe}` is replaced with the executable file name, such as `GAME.EXE`.
For `PAIDBETA.EXE`, the launcher controls input directly: it chooses hard mode, reads the board from the DOSBox window, and sends strategy moves in response to the computer's observed move.
