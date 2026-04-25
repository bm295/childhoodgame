# ChildhoodGame (.NET)

This repository is a C#/.NET solution that validates and launches DOS games through a configurable runtime.

## Projects

- `src/ChildhoodGame.Runner`: Console host with CLI options for validation and runtime launch.
- `src/ChildhoodGame.Core`: Loader/runtime abstractions and DOS emulator strategy integration.

## SDK and framework version

- Target framework: **.NET 10 (`net10.0`)**.
- Language version: **C# 10** via explicit `<LangVersion>10.0</LangVersion>`.
- `global.json` pins SDK version **10.0.103** with `rollForward: latestFeature`.

## CLI usage

```bash
dotnet run --project src/ChildhoodGame.Runner/ChildhoodGame.Runner.csproj -- \
  --game-path /path/to/game \
  --run \
  --save-state /path/to/save.sav \
  --load-state /path/to/load.sav
```

### Supported options

- `--game-path` (required): root folder containing game assets.
- `--run` (optional): starts the runtime after validation; if omitted, validation-only mode is used.
- `--save-state` (optional): path where save-state should be written by emulator wrapper.
- `--load-state` (optional): path for a save-state to load at startup.

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
