namespace PoeShared.Services;

public interface IBufferedItem : IBufferedItemId
{
    void HandleState(BufferedItemState state);
}

public interface IBufferedItemId
{
    string Id { get; }
}

public interface IBufferedItemAsync : IBufferedItemId
{
    Task HandleStateAsync(BufferedItemState state);
}