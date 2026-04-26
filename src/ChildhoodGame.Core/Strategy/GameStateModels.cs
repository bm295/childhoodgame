namespace ChildhoodGame.Core.Strategy;

public interface IGameStateRuntime<TState> : IGameRuntime
{
    Task<TState> ReadStateAsync(CancellationToken cancellationToken = default);
}
