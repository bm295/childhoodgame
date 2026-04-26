namespace ChildhoodGame.Core.Strategy;

public interface IWinConditionDetector<TState>
{
    string Name { get; }

    bool IsSatisfied(TState state);
}
