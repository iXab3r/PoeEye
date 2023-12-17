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
    public UpdaterModule()
    {
    }

    protected override void RegisterTypesInternal(IUnityContainer container)
    {
        container.AddNewExtensionIfNotExists<UpdaterRegistrations>();
    }

    protected override void OnInitializedInternal(IUnityContainer container)
    {
        var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
        registrator.RegisterSettingsEditor<UpdateSettingsConfig, UpdateSettingsViewModel>();
    }
}