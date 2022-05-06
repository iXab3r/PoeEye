using PoeShared.Modularity;

namespace PoeShared.Scaffolding;

public abstract class ReactiveObjectWithProperties<TProperties> : DisposableReactiveObject where TProperties : IPoeEyeConfigVersioned, new()
{
    protected ReactiveObjectWithProperties(TProperties properties)
    {
        InitialProperties = properties;
    }

    protected TProperties InitialProperties { get; }
}