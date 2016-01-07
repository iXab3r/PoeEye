namespace PoeEyeUi.Prism
{
    using System;
    using System.Diagnostics;
    using System.Reactive.Concurrency;
    using System.Windows;
    using System.Windows.Interop;

    using Config;

    using MetroModels;

    using Microsoft.Practices.Unity;

    using PoeTrade.Models;
    using PoeTrade.ViewModels;

    using WpfAutoCompleteControls.Editors;

    internal sealed class UiRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<ImagesCache, ImagesCache>(new ContainerControlledLifetimeManager())
                .RegisterType<IPoePriceCalculcator, PoePriceCalculcator>(new ContainerControlledLifetimeManager())
                .RegisterType<IAudioNotificationsManager, AudioNotificationsManager>(new ContainerControlledLifetimeManager())
                .RegisterType<IPoeEyeConfigProvider<IPoeEyeConfig>, PoeEyeConfigProviderFromFile>(new ContainerControlledLifetimeManager())
                .RegisterType<IDialogCoordinator, DialogCoordinator>(new ContainerControlledLifetimeManager());

            Container
                .RegisterType<IScheduler>(WellKnownSchedulers.Ui, new InjectionFactory(x => DispatcherScheduler.Current))
                .RegisterType<IScheduler>(WellKnownSchedulers.Background, new InjectionFactory(x => TaskPoolScheduler.Default));

            Container
                .RegisterType<IMainWindowViewModel, MainWindowViewModel>(new ContainerControlledLifetimeManager())
                .RegisterType<IPoeCaptchaRegistrator, PoeCaptchaRegistrator>(new ContainerControlledLifetimeManager())
                .RegisterType<IPoeTradeViewModel, PoeTradeViewModel>()
                .RegisterType<IPoeModViewModel, PoeModViewModel>()
                .RegisterType<IPoeModsEditorViewModel, PoeModsEditorViewModel>()
                .RegisterType<IPoeModGroupsEditorViewModel, PoeModGroupsEditorViewModel>()
                .RegisterType<IPoeTradeCaptchaViewModel, PoeTradeCaptchaViewModel>()
                .RegisterType<IHistoricalTradesViewModel, HistoricalTradesViewModel>()
                .RegisterType<IRecheckPeriodViewModel, RecheckPeriodViewModel>()
                .RegisterType<ISuggestionProvider, FuzzySuggestionProvider>()
                .RegisterType<IPoeEyeConfig>(new InjectionFactory(x => x.Resolve<IPoeEyeConfigProvider<IPoeEyeConfig>>().Load()));

            Container
                .RegisterType<IWindowTracker, WindowTracker>();

            RegisterMainWindowTracker();
        }

        private void RegisterMainWindowTracker()
        {
            Container
                .RegisterType<IWindowTracker, WindowTracker>(
                    WellKnownWindows.Main, 
                    new ContainerControlledLifetimeManager(), 
                    new InjectionFactory(unity => unity.Resolve<IWindowTracker>(new DependencyOverride<Func<string>>(new Func<string>(GetMainWindowTitle)))));
        }

        private string GetMainWindowTitle()
        {
            return Process.GetCurrentProcess().MainWindowTitle;
        }
    }
}