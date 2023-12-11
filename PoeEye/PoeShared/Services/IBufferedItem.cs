namespace PoeShared.Services;

public interface IBufferedItem
{
    string Id { get; }
    void HandleState(BufferedItemState state);
}