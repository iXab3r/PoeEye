namespace PoeEyeUi.Prism
{
    using Microsoft.Practices.Unity;

    using PoeTrade.Models;
    using PoeTrade.ViewModels;

    internal sealed class UiRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container.RegisterType<ItemsCache, ItemsCache>(new ContainerControlledLifetimeManager());

            Container
                    .RegisterType<MainWindowViewModel, MainWindowViewModel>()
                    .RegisterType<MainWindowTabViewModel, MainWindowTabViewModel>()
                    .RegisterType<IPoeTradeViewModel, PoeTradeViewModel>();
        }
    }
}