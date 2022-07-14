using PoeShared.Modularity;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Squirrel.Updater;
using Prism.Ioc;
using Prism.Modularity;
using Unity;

namespace PoeShared.Squirrel.Prism;

public sealed class UpdaterModule : DynamicModule
{
    private readonly IUnityContainer container;

    public UpdaterModule(IUnityContainer container)
    {
        Guard.ArgumentNotNull(container, nameof(container));

        this.container = container;
    }

    protected override void RegisterTypesInternal(IContainerRegistry containerRegistry)
    {
        container.AddNewExtensionIfNotExists<UpdaterRegistrations>();
    }

    protected override void OnInitializedInternal(IContainerProvider containerProvider)
    {
        var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
        registrator.RegisterSettingsEditor<UpdateSettingsConfig, UpdateSettingsViewModel>();
    }
}