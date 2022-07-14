using PoeShared.Scaffolding;
using Prism.Ioc;

namespace PoeShared.Modularity;

public abstract class DynamicModule : DisposableReactiveObjectWithLogger, IDynamicModule
{
    protected DynamicModule()
    {
        Log.Info("Module constructed");
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        Log.Info(() => "Registering types");
        RegisterTypesInternal(containerRegistry);
        Log.Info(() => "Registered types");
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        Log.Info(() => "Initializing module");
        OnInitializedInternal(containerProvider);
        Log.Info(() => "Initialized module");
    }

    protected abstract void RegisterTypesInternal(IContainerRegistry containerRegistry);

    protected abstract void OnInitializedInternal(IContainerProvider containerProvider);
}