namespace PoeShared.Scaffolding;

public abstract record DisposableReactiveRecord : ReactiveRecord, IDisposable
{
    public CompositeDisposable Anchors { get; } = new();

    public virtual void Dispose()
    {
        Anchors.Dispose();
        GC.SuppressFinalize(this);
    }
}