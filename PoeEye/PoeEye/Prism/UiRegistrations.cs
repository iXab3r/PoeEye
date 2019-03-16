using PoeEye.Config;
using PoeEye.PoeTrade.Models;
using PoeEye.PoeTrade.ViewModels;
using PoeEye.Shell.ViewModels;
using PoeEye.Updates;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using Unity;
using Unity.Extension;
using WpfAutoCompleteControls.Editors;
using IPoeEyeMainConfigProvider = PoeShared.Modularity.IConfigProvider<PoeEye.Config.PoeEyeMainConfig>;

namespace PoeEye.Prism
{
    internal sealed class UiRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IPoePriceCalculcator, PoePriceCalculator>()
                .RegisterSingleton(typeof(IPoeEyeMainConfigProvider), typeof(GenericConfigProvider<PoeEyeMainConfig>))
                .RegisterSingleton<IConfigProvider, PoeEyeConfigProviderFromFile>()
                .RegisterSingleton<IPoeCaptchaRegistrator, PoeCaptchaRegistrator>()
                .RegisterSingleton<IPoeApiProvider, PoeApiProvider>()
                .RegisterSingleton<IPoeItemViewModelFactory, PoeItemViewModelFactory>()
                .RegisterSingleton<IMainWindowViewModel, MainWindowViewModel>();

            Container
                .RegisterType<IPoeAdvancedTradesListViewModel, PoeAdvancedTradesListViewModel>()
                .RegisterType<IPoeSummaryTabViewModel, PoeSummaryTabViewModel>()
                .RegisterType<IPoeItemTypeSelectorViewModel, PoeItemTypeSelectorViewModel>()
                .RegisterType<IApplicationUpdaterModel, ApplicationUpdaterModel>()
                .RegisterType<IPoeItemModsViewModel, PoeItemModsViewModel>()
                .RegisterType<IPoeTradeViewModel, PoeTradeViewModel>()
                .RegisterType<IMainWindowTabViewModel, MainWindowTabViewModel>()
                .RegisterType<IPoeTradeQuickFilter, PoeTradeQuickFilter>()
                .RegisterType<IPoeModViewModel, PoeModViewModel>()
                .RegisterType<IPoeChatViewModel, PoeChatViewModel>()
                .RegisterType<IPoeTradesListViewModel, PoeTradesListViewModel>()
                .RegisterType<IPoeQueryViewModel, PoeQueryViewModel>()
                .RegisterType<IPoeApiSelectorViewModel, PoeApiSelectorViewModel>()
                .RegisterType<IPoeModsEditorViewModel, PoeModsEditorViewModel>()
                .RegisterType<IPoeModGroupsEditorViewModel, PoeModGroupsEditorViewModel>()
                .RegisterType<IRecheckPeriodViewModel, RecheckPeriodViewModel>()
                .RegisterType<ISuggestionProvider, FuzzySuggestionProvider>()
                .RegisterType<IReactiveSuggestionProvider, ReactiveSuggestionProvider>();
        }
    }
}