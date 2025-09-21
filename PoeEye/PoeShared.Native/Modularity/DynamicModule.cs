using PoeShared.Scaffolding;
using Prism.Ioc;
using Prism.Unity;
using Unity;

namespace PoeShared.Modularity;

public abstract class DynamicModule : DisposableReactiveObjectWithLogger, IDynamicModule
{
    private AtomicFlag isRegistered = new();

    protected DynamicModule()
    {
        Log.Info("Module constructed");
    }

    public void RegisterTypes(IUnityContainer container)
    {
        if (!isRegistered.Set())
        {
            return;
        }

        Log.Info("Registering types");
        RegisterTypesInternal(container);
        Log.Info("Registered types");
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        RegisterTypes(containerRegistry.GetContainer());
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        Log.Info("Initializing module");
        OnInitializedInternal(containerProvider.GetContainer());
        Log.Info("Initialized module");
    }

    protected virtual void RegisterTypesInternal(IUnityContainer container)
    {
    }

    protected virtual void OnInitializedInternal(IUnityContainer container)
    {
    }
}