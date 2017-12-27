using PoeShared.Scaffolding;

namespace PoeBud.Prism
{
    using System.Reactive.Concurrency;

    using WindowsInput;

    using Config;

    using Microsoft.Practices.Unity;

    using Models;

    using ViewModels;

    internal sealed class PoeBudModuleRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IInputSimulator>(
                    new ContainerControlledLifetimeManager(), new InjectionFactory(x => new InputSimulator()))
                .RegisterSingleton<IPoeWindowManager, PoeWindowManager>()
                .RegisterSingleton<IUiOverlaysProvider, UiOverlaysProvider>();

            Container
                .RegisterType<IDefaultStashUpdaterStrategy, UpdaterDefaultProcessAllStrategy>()
                .RegisterType<IUserInputBlocker, UserInputBlocker>()
                .RegisterType<IPoeStashUpdater, PoeStashUpdater>()
                .RegisterType<ISolutionExecutorViewModel, SolutionExecutorViewModel>()
                .RegisterType<IPoeWindow, PoeWindow>()
                .RegisterType<ISolutionExecutorModel, SolutionExecutorModel>()
                .RegisterType<IUserInteractionsManager, UserInteractionsManager>();
        }
    }
}