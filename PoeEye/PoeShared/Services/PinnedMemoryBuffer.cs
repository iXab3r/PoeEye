using System.Runtime.InteropServices;

namespace PoeShared.Services;

public readonly struct PinnedMemoryBuffer : IDisposable
{
    public CompositeDisposable Anchors { get; } = new();
    
    public IntPtr Pointer { get; }
    
    public PinnedMemoryBuffer(IMemoryPool memoryPool, int bufferSize)
    {
        var resultData = memoryPool.Rent(bufferSize);
        
        var handle = GCHandle.Alloc(resultData, GCHandleType.Pinned);
        Pointer = handle.AddrOfPinnedObject();
        Disposable.Create(() => handle.Free()).AddTo(Anchors);
        
        Disposable.Create(() => memoryPool.Return(resultData)).AddTo(Anchors);
    }

    public void Dispose()
    {
        Anchors.Dispose();
    }
}