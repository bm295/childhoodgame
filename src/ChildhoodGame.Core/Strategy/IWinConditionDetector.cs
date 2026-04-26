namespace ChildhoodGame.Core.Strategy;

public interface IWinConditionDetector
{
    string Name { get; }

    bool IsSatisfied(GameRuntimeState state);
}
