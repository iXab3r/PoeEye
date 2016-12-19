﻿using PoeShared.Prism;

namespace PoeEye.Prism
{
    using System.Diagnostics;
    using System.Reactive.Concurrency;

    using Config;

    using MetroModels;

    using Microsoft.Practices.Unity;

    using PoeShared.Scaffolding;

    using PoeTrade.Models;
    using PoeTrade.ViewModels;

    using Properties;

    using ReactiveUI;

    using WpfAutoCompleteControls.Editors;

    internal sealed class UiRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IImagesCacheService, ImagesCacheService>()
                .RegisterSingleton<IPoePriceCalculcator, PoePriceCalculcator>()
                .RegisterSingleton<IAudioNotificationsManager, AudioNotificationsManager>()
                .RegisterSingleton<IWhispersNotificationManager, WhispersNotificationManager>()
                .RegisterSingleton<IPoeEyeConfigProvider, PoeEyeConfigProviderFromFile>()
                .RegisterSingleton<IPoeCaptchaRegistrator, PoeCaptchaRegistrator>()
                .RegisterSingleton<IPoeApiProvider, PoeApiProvider>()
                .RegisterSingleton<IDialogCoordinator, DialogCoordinator>();

            Container
                .RegisterType<IScheduler>(WellKnownSchedulers.Ui, new InjectionFactory(x => RxApp.MainThreadScheduler))
                .RegisterType<IScheduler>(WellKnownSchedulers.Background, new InjectionFactory(x => RxApp.TaskpoolScheduler));

            Container
                .RegisterType<IMainWindowViewModel, MainWindowViewModel>()
                .RegisterType<IPoeTradeViewModel, PoeTradeViewModel>()
                .RegisterType<IMainWindowTabViewModel, MainWindowTabViewModel>()
                .RegisterType<IPoeModViewModel, PoeModViewModel>()
                .RegisterType<IPoeChatViewModel, PoeChatViewModel>()
                .RegisterType<IPoeTradesListViewModel, PoeTradesListViewModel>()
                .RegisterType<IPoeQueryViewModel, PoeQueryViewModel>()
                .RegisterType<IAudioNotificationSelectorViewModel, AudioNotificationSelectorViewModel>()
                .RegisterType<IPoeApiSelectorViewModel, PoeApiSelectorViewModel>()
                .RegisterType<IPoeModsEditorViewModel, PoeModsEditorViewModel>()
                .RegisterType<IPoeModGroupsEditorViewModel, PoeModGroupsEditorViewModel>()
                .RegisterType<IHistoricalTradesViewModel, HistoricalTradesViewModel>()
                .RegisterType<IRecheckPeriodViewModel, RecheckPeriodViewModel>()
                .RegisterType<ISuggestionProvider, FuzzySuggestionProvider>();

            Container.RegisterWindowTracker(WellKnownWindows.Main, () => Process.GetCurrentProcess().MainWindowTitle);
            Container.RegisterWindowTracker(WellKnownWindows.PathOfExile, () => Settings.Default.PathOfExileWindowName);
        }
    }
}