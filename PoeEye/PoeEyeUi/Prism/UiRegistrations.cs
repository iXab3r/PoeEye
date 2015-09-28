namespace PoeEyeUi.Prism
{
    using Microsoft.Practices.Unity;

    using PoeTrade.ViewModels;

    using MainWindowTabViewModel = PoeEyeUi.MainWindowTabViewModel;

    internal sealed class UiRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                    .RegisterType<MainWindowViewModel, MainWindowViewModel>()
                    .RegisterType<MainWindowTabViewModel, MainWindowTabViewModel>()
                    .RegisterType<MainWindowTabViewModel, MainWindowTabViewModel>()
                    .RegisterType<IPoeTradeViewModel, PoeTradeViewModel>();
        }
    }
}