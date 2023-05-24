using System.Runtime.InteropServices;

namespace PoeShared.Services;

public readonly struct PinnedMemoryBuffer : IDisposable
{
    public CompositeDisposable Anchors { get; } = new();
    
    public IntPtr Pointer { get; }
    
    public PinnedMemoryBuffer(IMemoryPool memoryPool, int bufferSize)
    {
        var resultData = memoryPool.Rent(bufferSize);
        var resultDataAnchor = Disposable.Create(() => memoryPool.Return(resultData));
        
        var handle = GCHandle.Alloc(resultData, GCHandleType.Pinned);
        Pointer = handle.AddrOfPinnedObject();
                
        Anchors.Add(Disposable.Create(() => handle.Free()));
        Anchors.Add(resultDataAnchor);
    }

    public void Dispose()
    {
        Anchors.Dispose();
    }
}