# ChildhoodGame (.NET)

This repository is now a C#/.NET solution for running a DOS-game host and game-session orchestration logic.

## Projects

- `src/ChildhoodGame.Runner`: Console/desktop host entry point that starts a game session.
- `src/ChildhoodGame.Core`: Domain orchestration and win-condition evaluation logic.

## SDK and framework version

- Target framework: **.NET 8 (`net8.0`)**.
- `global.json` pins SDK version **8.0.100** with `rollForward: latestFeature`.

> Why .NET 8? The current toolchain in this environment does not include a .NET SDK (`dotnet --list-sdks` reports `dotnet: command not found`), so `.NET 14` could not be validated. The solution is pinned to the nearest broadly supported SDK baseline (`8.0.100`) to keep the project buildable in standard .NET setups.

## Prerequisites

1. Install the .NET 8 SDK (8.0.100 or newer 8.0 feature band).
2. Verify installation:

```bash
dotnet --info
```

## Restore and build

From repo root:

```bash
dotnet restore ChildhoodGame.sln
dotnet build ChildhoodGame.sln -c Release
```

## Run

Use the runner project:

```bash
dotnet run --project src/ChildhoodGame.Runner/ChildhoodGame.Runner.csproj
```

Expected output includes launch text and whether the win condition was met.

## No Node.js runtime required

Node/Angular build files were removed from the root workflow, and all executable entry points are now in C# (`Program.cs` under `ChildhoodGame.Runner`).
