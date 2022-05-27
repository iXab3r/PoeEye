using System.ComponentModel;

namespace PoeShared.Scaffolding;

internal interface INpcEventInvoker
{
    void Add(PropertyChangedEventHandler eventHandler);
    void Remove(PropertyChangedEventHandler eventHandler);
    void Raise(string propertyName);
}