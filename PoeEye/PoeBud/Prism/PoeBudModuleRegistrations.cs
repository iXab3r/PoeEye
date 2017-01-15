using PoeBud.OfficialApi;
using PoeBud.OfficialApi.ProcurementLegacy;
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
                .RegisterSingleton<IPoeBudConfigProvider<IPoeBudConfig>, PoeBudConfigProviderFromFile>()
                .RegisterType<IInputSimulator>(new ContainerControlledLifetimeManager(), new InjectionFactory(x => new InputSimulator()))
                .RegisterSingleton<IPoeWindowsManager, PoeWindowsManager>()
                .RegisterSingleton<IGearTypeAnalyzer, GearTypeAnalyzer>();

            Container
                .RegisterType<HotkeyManagerBase>(new InjectionFactory(x => HotkeyManager.Current))
                .RegisterType<IUserInputBlocker, UserInputBlocker>()
                .RegisterType<ISolutionExecutorViewModel, SolutionExecutorViewModel>()
                .RegisterType<IPoeWindow, PoeWindow>()
                .RegisterType<IPoeClient, PoeClient>()
                .RegisterType<IUserInteractionsManager, UserInteractionsManager>();
        }
    }
}