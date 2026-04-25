# ChildhoodGame (.NET)

This repository is a C#/.NET solution that validates and launches DOS games through a simple Windows desktop launcher.

## Projects

- `src/ChildhoodGame.Runner`: Windows launcher for browsing a game file, validating the package, and starting the runtime.
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

- browse for any file inside the game folder
- inspect the resolved game root folder
- optionally choose save-state and load-state files
- validate the package before launch
- start and stop the runtime from the same window

You can also drag and drop a file from the target game folder onto the launcher window.

## Game folder requirements

The loader validates:

- `DOSBOX.CONF`
- `game.config.json`
- executable specified by `requiredExecutable` (defaults to `GAME.EXE`)

`game.config.json` example:

```json
{
  "emulatorType": "wrapper",
  "emulatorExecutable": "dosbox",
  "emulatorArguments": "-conf \"{config}\" -c \"mount c .\" -c \"c:\" -c \"{exe}\"",
  "startupInput": ["ENTER"],
  "requiredExecutable": "GAME.EXE"
}
```

Use `emulatorType: "embedded"` for embedded-core mode.
