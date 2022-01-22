using PoeShared.Squirrel.Updater;
using Unity;
using Unity.Extension;

namespace PoeShared.Squirrel.Prism;

public sealed class UpdaterRegistrations : UnityContainerExtension
{
    protected override void Initialize()
    {
        Container
            .RegisterSingleton<IUpdateSourceProvider, UpdateSourceProviderFromConfig>()
            .RegisterSingleton<IUpdaterWindowDisplayer, UpdaterWindowDisplayer>()
            .RegisterSingleton<ISquirrelEventsHandler, SquirrelEventsHandler>()
            .RegisterSingleton<IApplicationUpdaterViewModel, ApplicationUpdaterViewModel>()
            .RegisterSingleton<IApplicationUpdaterModel, ApplicationUpdaterModel>();

        Container
            .RegisterType<IUpdaterWindowViewModel, UpdaterWindowViewModel>();
    }
}