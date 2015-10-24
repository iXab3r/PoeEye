namespace PoeEyeUi.Prism
{
    using System.Reactive.Concurrency;

    using Config;

    using MetroModels;

    using Microsoft.Practices.Unity;

    using PoeEye.PoeTrade;

    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    using PoeTrade.Models;
    using PoeTrade.ViewModels;

    using TypeConverter;

    using WpfAutoCompleteControls.Editors;

    internal sealed class UiRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container.RegisterType<ImagesCache, ImagesCache>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IDialogCoordinator, DialogCoordinator>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IPoePriceCalculcator, PoePriceCalculcator>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IAudioNotificationsManager, AudioNotificationsManager>(new ContainerControlledLifetimeManager());
            Container.RegisterType<ISuggestionProvider, GenericSuggestionProvider>();

            Container.RegisterType<IScheduler>(WellKnownSchedulers.Ui, new InjectionFactory(x => DispatcherScheduler.Current));
            Container.RegisterType<IScheduler>(WellKnownSchedulers.Background, new InjectionFactory(x => TaskPoolScheduler.Default));

            Container.RegisterType<IPoeEyeConfigProvider<IPoeEyeConfig>, PoeEyeConfigProviderFromFile>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IPoeEyeConfig>(new InjectionFactory(x => x.Resolve<IPoeEyeConfigProvider<IPoeEyeConfig>>().Load()));

            Container
                    .RegisterType<MainWindowViewModel, MainWindowViewModel>()
                    .RegisterType<MainWindowTabViewModel, MainWindowTabViewModel>()
                    .RegisterType<IPoeTradeViewModel, PoeTradeViewModel>();
        }
    }
}