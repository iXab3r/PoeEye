namespace PoeShared.Services;

public interface ISharedResourceLatch : IDisposableReactiveObject
{
    bool IsBusy { get; }
        
    string Name { get; set; }

    IDisposable Rent();
}