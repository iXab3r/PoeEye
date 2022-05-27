using System.ComponentModel;

namespace PoeShared.Scaffolding;

internal sealed class DefaultNpcEventInvoker : INpcEventInvoker
{
    private PropertyChangedEventHandler propertyChanged;
    private readonly object owner;

    public DefaultNpcEventInvoker(object owner)
    {
        this.owner = owner;
    }

    public void Add(PropertyChangedEventHandler eventHandler)
    {
        var current = propertyChanged;
        PropertyChangedEventHandler snapshot;
        do
        {
            snapshot = current;
            var modified = (PropertyChangedEventHandler)Delegate.Combine(snapshot, eventHandler);
            current = Interlocked.CompareExchange(ref propertyChanged, modified, snapshot);
        }
        while (current != snapshot);
    }

    public void Remove(PropertyChangedEventHandler eventHandler)
    {
        var current = propertyChanged;
        PropertyChangedEventHandler snapshot;
        do
        {
            snapshot = current;
            var modified = (PropertyChangedEventHandler)Delegate.Remove(snapshot, eventHandler);
            current = Interlocked.CompareExchange(ref propertyChanged, modified, snapshot);
        }
        while (current != snapshot);
    }

    public void Raise(string propertyName)
    {
        propertyChanged?.Invoke(owner, new PropertyChangedEventArgs(propertyName));
    }
}