namespace PoeShared.Services;

public interface ISharedResource : IDisposable
{
    long RefCount { get; }
        
    IDisposable RentReadLock();

    IDisposable RentWriteLock();
    
    bool IsDisposed { get; }

    bool TryRent();

    void AddResource(IDisposable resource);

    void AddResource(Action disposeAction);
}