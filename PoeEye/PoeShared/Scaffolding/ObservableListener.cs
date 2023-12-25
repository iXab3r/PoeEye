namespace PoeShared.Scaffolding;

public sealed class ObservableListener<T> : DisposableReactiveObject
{
    public ObservableListener(IObservable<T> observable)
    {
        observable.Subscribe(x => Value = x).AddTo(Anchors);
    }
    
    public T Value { get; private set; }

    ~ObservableListener()
    {
        
    }
}