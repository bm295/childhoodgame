using ChildhoodGame.Core;

var orchestrator = new GameOrchestrator();

var request = new GameSessionRequest(
    DosGameTitle: "Commander Keen",
    MaxTurns: 5,
    TurnsRequiredToWin: 3);

var result = orchestrator.Run(request);

Console.WriteLine($"Launching DOS host for: {request.DosGameTitle}");
Console.WriteLine(result.Summary);
Console.WriteLine(result.IsWin ? "Win condition met." : "Win condition not met.");
