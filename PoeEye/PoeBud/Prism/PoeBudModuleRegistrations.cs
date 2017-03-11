using PoeShared.Scaffolding;

namespace PoeBud.Prism
{
    using System.Reactive.Concurrency;

    using WindowsInput;

    using Config;

    using Microsoft.Practices.Unity;

    using Models;

    using NHotkey;
    using NHotkey.Wpf;

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
                .RegisterType<HotkeyManagerBase>(new InjectionFactory(x => HotkeyManager.Current))
                .RegisterType<IUserInputBlocker, UserInputBlocker>()
                .RegisterType<IPoeStashUpdater, PoeStashUpdater>()
                .RegisterType<ISolutionExecutorViewModel, SolutionExecutorViewModel>()
                .RegisterType<IPoeWindow, PoeWindow>()
                .RegisterType<ISolutionExecutorModel, SolutionExecutorModel>()
                .RegisterType<IUserInteractionsManager, UserInteractionsManager>();
        }
    }
}