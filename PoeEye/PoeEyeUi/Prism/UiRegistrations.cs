namespace PoeEyeUi.Prism
{
    using System;
    using System.Diagnostics;
    using System.Reactive.Concurrency;

    using Config;

    using MetroModels;

    using Microsoft.Practices.Unity;

    using PoeTrade.Models;
    using PoeTrade.ViewModels;

    using WpfAutoCompleteControls.Editors;

    internal sealed class UiRegistrations : UnityContainerExtension
    {
        private static readonly string PathOfExileWindowTitle = "Path of Exile";

        protected override void Initialize()
        {
            Container
                .RegisterType<ImagesCache, ImagesCache>(new ContainerControlledLifetimeManager())
                .RegisterType<IPoePriceCalculcator, PoePriceCalculcator>(new ContainerControlledLifetimeManager())
                .RegisterType<IAudioNotificationsManager, AudioNotificationsManager>(new ContainerControlledLifetimeManager())
                .RegisterType<IWhispersNotificationManager, WhispersNotificationManager>(new ContainerControlledLifetimeManager())
                .RegisterType<IPoeEyeConfigProvider, PoeEyeConfigProviderFromFile>(new ContainerControlledLifetimeManager())
                .RegisterType<IDialogCoordinator, DialogCoordinator>(new ContainerControlledLifetimeManager());

            Container
                .RegisterType<IScheduler>(WellKnownSchedulers.Ui, new InjectionFactory(x => DispatcherScheduler.Current))
                .RegisterType<IScheduler>(WellKnownSchedulers.Background, new InjectionFactory(x => TaskPoolScheduler.Default));

            Container
                .RegisterType<IMainWindowViewModel, MainWindowViewModel>(new ContainerControlledLifetimeManager())
                .RegisterType<IPoeCaptchaRegistrator, PoeCaptchaRegistrator>(new ContainerControlledLifetimeManager())
                .RegisterType<IPoeTradeViewModel, PoeTradeViewModel>()
                .RegisterType<IMainWindowTabViewModel, MainWindowTabViewModel>()
                .RegisterType<IPoeModViewModel, PoeModViewModel>()
                .RegisterType<IPoeChatViewModel, PoeChatViewModel>()
                .RegisterType<IPoeModsEditorViewModel, PoeModsEditorViewModel>()
                .RegisterType<IPoeModGroupsEditorViewModel, PoeModGroupsEditorViewModel>()
                .RegisterType<IPoeTradeCaptchaViewModel, PoeTradeCaptchaViewModel>()
                .RegisterType<IHistoricalTradesViewModel, HistoricalTradesViewModel>()
                .RegisterType<IRecheckPeriodViewModel, RecheckPeriodViewModel>()
                .RegisterType<ISuggestionProvider, FuzzySuggestionProvider>();

            Container
                .RegisterType<IWindowTracker, WindowTracker>();

            RegisterMainWindowTracker();
            RegisterPathOfExileWindowTracker();
        }

        private IUnityContainer RegisterMainWindowTracker()
        {
            return Container
                .RegisterType<IWindowTracker, WindowTracker>(
                    WellKnownWindows.Main,
                    new ContainerControlledLifetimeManager(),
                    new InjectionFactory(unity => unity.Resolve<IWindowTracker>(new DependencyOverride<Func<string>>(new Func<string>(GetMainWindowTitle)))));
        }

        private IUnityContainer RegisterPathOfExileWindowTracker()
        {
            return Container
                .RegisterType<IWindowTracker, WindowTracker>(
                    WellKnownWindows.PathOfExile,
                    new ContainerControlledLifetimeManager(),
                    new InjectionFactory(unity => unity.Resolve<IWindowTracker>(new DependencyOverride<Func<string>>(new Func<string>(() => PathOfExileWindowTitle)))));
        }

        private string GetMainWindowTitle()
        {
            return Process.GetCurrentProcess().MainWindowTitle;
        }
    }
}