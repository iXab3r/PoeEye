namespace PoeEyeUi.Prism
{
    using MetroModels;

    using Microsoft.Practices.Unity;

    using PoeShared.Common;

    using PoeTrade.Models;
    using PoeTrade.ViewModels;

    using WpfControls;

    internal sealed class UiRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container.RegisterType<ItemsCache, ItemsCache>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IDialogCoordinator, DialogCoordinator>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IPoePriceCalculcator, PoePriceCalculcator>(new ContainerControlledLifetimeManager());

            Container.RegisterType<IAudioNotificationsManager, AudioNotificationsManager>(new ContainerControlledLifetimeManager());
            
            Container
                    .RegisterType<MainWindowViewModel, MainWindowViewModel>()
                    .RegisterType<MainWindowTabViewModel, MainWindowTabViewModel>()
                    .RegisterType<IPoeTradeViewModel, PoeTradeViewModel>();
        }
    }
}