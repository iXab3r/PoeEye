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
    
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        if (!isRegistered.Set())
        {
            return;
        }
        
        Log.Info("Registering types");
        RegisterTypesInternal(containerRegistry.GetContainer());
        Log.Info("Registered types");
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