namespace PoeShared.Services;

public interface IBufferedItemsProcessor : IDisposableReactiveObject
{
    TimeSpan BufferPeriod { get; set; }
    
    uint Capacity { get; set; }
    
    void Add<T>(BufferedItemState state, T item) where T : IBufferedItemId;
    
    void Flush(bool immediateFlush);
}