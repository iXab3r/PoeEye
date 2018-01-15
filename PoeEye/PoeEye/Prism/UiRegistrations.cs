using PoeEye.PoeTrade.Shell.ViewModels;
using PoeEye.PoeTrade.Updater;
using PoeShared.Audio;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.PoeTrade;
using PoeShared.Prism;
using PoeShared.UI.Models;
using PoeEyeMainConfig = PoeEye.Config.PoeEyeMainConfig;

namespace PoeEye.Prism
{
    using System.Diagnostics;
    using System.Reactive.Concurrency;

    using Config;

    using Microsoft.Practices.Unity;

    using PoeShared.Scaffolding;

    using PoeTrade.Models;
    using PoeTrade.ViewModels;

    using Properties;

    using ReactiveUI;

    using WpfAutoCompleteControls.Editors;
    using IPoeEyeMainConfigProvider = IConfigProvider<PoeEyeMainConfig>;

    internal sealed class UiRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IPoePriceCalculcator, PoePriceCalculcator>()
                .RegisterSingleton<IWhispersNotificationManager, WhispersNotificationManager>()
                .RegisterSingleton(typeof(IPoeEyeMainConfigProvider), typeof(GenericConfigProvider<PoeEyeMainConfig>))
                .RegisterSingleton(typeof(IConfigProvider<>), typeof(GenericConfigProvider<>))
                .RegisterSingleton<IConfigProvider, PoeEyeConfigProviderFromFile>()
                .RegisterSingleton<IPoeCaptchaRegistrator, PoeCaptchaRegistrator>()
                .RegisterSingleton<IPoeApiProvider, PoeApiProvider>()
                .RegisterSingleton<IPoeItemViewModelFactory, PoeItemViewModelFactory>();

            Container
                .RegisterType<IMainWindowViewModel, MainWindowViewModel>()
                .RegisterType<IApplicationUpdaterModel, ApplicationUpdaterModel>()
                .RegisterType<IPoeItemModsViewModel, PoeItemModsViewModel>()
                .RegisterType<IPoeTradeViewModel, PoeTradeViewModel>()
                .RegisterType<IMainWindowTabViewModel, MainWindowTabViewModel>()
                .RegisterType<IPoeModViewModel, PoeModViewModel>()
                .RegisterType<IPoeChatViewModel, PoeChatViewModel>()
                .RegisterType<IPoeTradesListViewModel, PoeTradesListViewModel>()
                .RegisterType<IPoeQueryViewModel, PoeQueryViewModel>()
                .RegisterType<IPoeApiSelectorViewModel, PoeApiSelectorViewModel>()
                .RegisterType<IPoeModsEditorViewModel, PoeModsEditorViewModel>()
                .RegisterType<IPoeModGroupsEditorViewModel, PoeModGroupsEditorViewModel>()
                .RegisterType<IHistoricalTradesViewModel, HistoricalTradesViewModel>()
                .RegisterType<IRecheckPeriodViewModel, RecheckPeriodViewModel>()
                .RegisterType<ISuggestionProvider, FuzzySuggestionProvider>()
                .RegisterType<IReactiveSuggestionProvider, ReactiveSuggestionProvider>();
        }
    }
}