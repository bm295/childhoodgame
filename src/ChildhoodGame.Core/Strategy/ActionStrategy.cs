namespace ChildhoodGame.Core.Strategy;

public interface IActionSelectionStrategy<TState>
{
    IReadOnlyList<string> SelectActions(TState state);
}
