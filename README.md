# ChildhoodGame (.NET)

This repository is now a C#/.NET solution for running a DOS-game host and game-session orchestration logic.

## Projects

- `src/ChildhoodGame.Runner`: Console/desktop host entry point that starts a game session.
- `src/ChildhoodGame.Core`: Domain orchestration and win-condition evaluation logic.

## SDK and framework version

- Target framework: **.NET 10 (`net10.0`)**.
- Language version: **C# 10** via explicit `<LangVersion>10.0</LangVersion>`.
- `global.json` pins SDK version **10.0.103** with `rollForward: latestFeature`.

> Note: `.NET 14` is not a current released target framework. The latest stable .NET target listed by Microsoft Learn is `.NET 10` (`net10.0`), so this repository is upgraded to `.NET 10` while keeping the language level pinned to `C# 10`.

## Prerequisites

1. Install the .NET 10 SDK (10.0.103 or newer compatible 10.0 feature band).
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
