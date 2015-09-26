namespace PoeEyeUi.Prism
{
    using Microsoft.Practices.Unity;

    using PoeTrade.ViewModels;

    internal sealed class UiRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                    .RegisterType<MainWindowViewModel, MainWindowViewModel>()
                    .RegisterType<MainWindowTabViewModel, MainWindowTabViewModel>()
                    .RegisterType<TradesListViewModel, TradesListViewModel>()
                    .RegisterType<IPoeTradeViewModel, PoeTradeViewModel>();
        }
    }
}