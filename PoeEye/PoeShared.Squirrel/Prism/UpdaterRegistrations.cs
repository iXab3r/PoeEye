using PoeShared.Services;
using PoeShared.Squirrel.Core;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Updater;
using Unity;
using Unity.Extension;

namespace PoeShared.Squirrel.Prism;

public sealed class UpdaterRegistrations : UnityContainerExtension
{
    protected override void Initialize()
    {
        Container
            .RegisterSingleton<UpdateSourceProviderFromConfig>(types: System.Type.EmptyTypes)
            .RegisterSingleton<IUpdateSourceProvider>(x => x.Resolve<UpdateSourceProviderFromConfig>())
            .RegisterSingleton<ApplicationUpdateHandlerRegistrator>(types: System.Type.EmptyTypes)
            .RegisterSingleton<IApplicationUpdateHandlerRegistrator>(x => x.Resolve<ApplicationUpdateHandlerRegistrator>())
            .RegisterSingleton<ApplicationUpdateInstallationService>(types: System.Type.EmptyTypes)
            .RegisterSingleton<ApplicationZipUpdateService>(types: System.Type.EmptyTypes)
            .RegisterSingleton<IApplicationZipUpdateService>(x => x.Resolve<ApplicationZipUpdateService>())
            .RegisterSingleton<IUpdaterWindowDisplayer, UpdaterWindowDisplayer>()
            .RegisterSingleton<ISquirrelEventsHandler, SquirrelEventsHandler>()
            .RegisterSingleton<IApplicationUpdaterViewModel, ApplicationUpdaterViewModel>()
            .RegisterSingleton<IApplicationUpdaterModel, ApplicationUpdaterModel>();

        Container
            .RegisterType<IUpdaterWindowViewModel, UpdaterWindowViewModel>();
    }
}
