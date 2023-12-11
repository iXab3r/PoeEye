namespace PoeShared.Services;

public interface IBufferedItemsProcessor : IDisposableReactiveObject
{
    TimeSpan BufferPeriod { get; set; }
    
    uint Capacity { get; set; }
    
    void Add(BufferedItemState state, IBufferedItem item);
    
    void Flush(bool immediateFlush);
}