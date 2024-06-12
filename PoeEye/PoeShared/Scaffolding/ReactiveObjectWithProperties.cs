using PoeShared.Modularity;

namespace PoeShared.Scaffolding;

public abstract class ReactiveObjectWithProperties<TProperties> : DisposableReactiveObject where TProperties : IPoeEyeConfigVersioned
{
    protected ReactiveObjectWithProperties(TProperties properties)
    {
    }
}