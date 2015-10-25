namespace PoeEyeUi.Prism
{
    using System.Reactive.Concurrency;

    using Config;

    using MetroModels;

    using Microsoft.Practices.Unity;

    using PoeTrade.Models;
    using PoeTrade.ViewModels;

    using WpfAutoCompleteControls.Editors;

    internal sealed class UiRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<ImagesCache, ImagesCache>(new ContainerControlledLifetimeManager())
                .RegisterType<IPoePriceCalculcator, PoePriceCalculcator>(new ContainerControlledLifetimeManager())
                .RegisterType<IAudioNotificationsManager, AudioNotificationsManager>(new ContainerControlledLifetimeManager())
                .RegisterType<IPoeEyeConfigProvider<IPoeEyeConfig>, PoeEyeConfigProviderFromFile>(new ContainerControlledLifetimeManager())
                .RegisterType<IDialogCoordinator, DialogCoordinator>(new ContainerControlledLifetimeManager());

            Container
                .RegisterType<IScheduler>(WellKnownSchedulers.Ui, new InjectionFactory(x => DispatcherScheduler.Current))
                .RegisterType<IScheduler>(WellKnownSchedulers.Background, new InjectionFactory(x => TaskPoolScheduler.Default));

            Container
                .RegisterType<IPoeTradeViewModel, PoeTradeViewModel>()
                .RegisterType<ISuggestionProvider, FuzzySuggestionProvider>()
                .RegisterType<IPoeEyeConfig>(new InjectionFactory(x => x.Resolve<IPoeEyeConfigProvider<IPoeEyeConfig>>().Load()));
        }
    }
}