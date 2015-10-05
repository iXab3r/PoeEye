﻿namespace PoeEyeUi.Prism
{
    using Config;

    using MetroModels;

    using Microsoft.Practices.Unity;

    using PoeShared.Common;
    using PoeShared.PoeTrade.Query;

    using PoeTrade.Models;
    using PoeTrade.ViewModels;

    using TypeConverter;

    using WpfControls;

    internal sealed class UiRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container.RegisterType<ItemsCache, ItemsCache>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IDialogCoordinator, DialogCoordinator>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IPoePriceCalculcator, PoePriceCalculcator>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IConverter<IPoeQueryInfo, IPoeQuery>, PoeQueryInfoToQueryConverter>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IAudioNotificationsManager, AudioNotificationsManager>(new ContainerControlledLifetimeManager());

            Container.RegisterType<IPoeEyeConfigProvider<IPoeEyeConfig>, PoeEyeConfigProviderFromFile>(new ContainerControlledLifetimeManager());

            Container
                    .RegisterType<MainWindowViewModel, MainWindowViewModel>()
                    .RegisterType<MainWindowTabViewModel, MainWindowTabViewModel>()
                    .RegisterType<IPoeTradeViewModel, PoeTradeViewModel>();
        }
    }
}