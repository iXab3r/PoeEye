using System.Collections.Specialized;
using System.Diagnostics;
using System.Reactive.Concurrency;
using Gma.System.MouseKeyHook;
using PoeShared.Audio;
using PoeShared.Communications;
using PoeShared.Communications.Chromium;
using PoeShared.Converters;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.PoeDatabase.PoeNinja;
using PoeShared.PoeTrade.Query;
using PoeShared.StashApi;
using PoeShared.StashApi.DataTypes;
using PoeShared.StashApi.ProcurementLegacy;
using PoeShared.UI.Models;
using PoeShared.UI.ViewModels;
using ProxyProvider;
using ReactiveUI;

namespace PoeShared.Prism
{
    using System.Collections.Generic;

    using Common;

    using Microsoft.Practices.Unity;

    using PoeDatabase;

    using PoeTrade;

    using Scaffolding;

    using TypeConverter;

    public sealed class CommonRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IClock, Clock>()
                .RegisterSingleton<IPoeEyeModulesRegistrator, PoeEyeModulesRegistrator>()
                .RegisterSingleton<IPoeEyeModulesEnumerator, PoeEyeModulesRegistrator>()
                .RegisterSingleton<IEqualityComparer<IPoeItem>, PoeItemEqualityComparer>()
                .RegisterSingleton<IConverter<NameValueCollection, string>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton<IConverter<NameValueCollection, IEnumerable<KeyValuePair<string, string>>>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton<IProxyProvider, GenericProxyProvider>(new InjectionFactory(unity => new GenericProxyProvider()))
                .RegisterSingleton<IRandomNumberGenerator, RandomNumberGenerator>()
                .RegisterSingleton<IImagesCacheService, ImagesCacheService>()
                .RegisterSingleton<IGearTypeAnalyzer, GearTypeAnalyzer>()
                .RegisterSingleton<IItemTypeAnalyzer, GearTypeAnalyzer>()
                .RegisterSingleton<IChromiumBrowserFactory, ChromiumBrowserFactory>()
                .RegisterSingleton<IChromiumBootstrapper, ChromiumBootstrapper>()
                .RegisterSingleton<IConverter<IStashItem, IPoeItem>, PoeStashItemToPoeItem>()
                .RegisterSingleton<IConverter<IStashItem, PoeItem>, PoeStashItemToPoeItem>()
                .RegisterSingleton<IConverter<string, PoePrice>, StringToPoePriceConverter>()
                .RegisterSingleton<IKeyboardEventsSource>(new InjectionFactory(x => x.Resolve<KeyboardEventsSource>(new DependencyOverride(typeof(IKeyboardMouseEvents), Hook.GlobalEvents()))))
                .RegisterSingleton<IAudioNotificationsManager, AudioNotificationsManager>()
                .RegisterSingleton<ISchedulerProvider, SchedulerProvider>()
                .RegisterSingleton<IOverlayWindowController, OverlayWindowController>(WellKnownWindows.PathOfExileWindow);

            Container
                .RegisterType<IScheduler>(WellKnownSchedulers.UI, new InjectionFactory(x => RxApp.MainThreadScheduler))
                .RegisterType<IScheduler>(WellKnownSchedulers.Background, new InjectionFactory(x => RxApp.TaskpoolScheduler))
                .RegisterType<IPoeLiveHistoryProvider, PoeLiveHistoryProvider>()
                .RegisterType<IHttpClient, GenericHttpClient>()
                .RegisterType<IPoeApiWrapper, PoeApiWrapper>()
                .RegisterType<IPoeStashClient, PoeStashClient>()
                .RegisterType<IPoeEconomicsSource, PoeNinjaDatabaseReader>()
                .RegisterType<IOverlayWindowController, OverlayWindowController>()
                .RegisterType<IAudioNotificationSelectorViewModel, AudioNotificationSelectorViewModel>()
                .RegisterType(typeof (IFactory<,,>), typeof (Factory<,,>))
                .RegisterType(typeof (IFactory<,>), typeof (Factory<,>))
                .RegisterType(typeof (IFactory<>), typeof (Factory<>));

            Container
                .RegisterType<IImageViewModel, ImageViewModel>();

            Container.RegisterWindowTracker(WellKnownWindows.AllWindows, () => ".*");
            Container.RegisterWindowTracker(WellKnownWindows.MainWindow, () => Process.GetCurrentProcess().MainWindowTitle);
            Container.RegisterWindowTracker(WellKnownWindows.PathOfExileWindow, () => $"^Path of Exile$");

            Container.RegisterOverlayController(
                WellKnownOverlays.PathOfExileOverlay, 
                WellKnownWindows.PathOfExileWindow);

            Container.RegisterOverlayController(
               WellKnownOverlays.AllWindowsLayeredOverlay,
               WellKnownWindows.AllWindows);

            Container
                .RegisterType<IPoeDatabaseReader>(
                    new ContainerControlledLifetimeManager(),
                    new InjectionFactory(
                        unity => unity.Resolve<ComplexPoeDatabaseReader>(
                            new DependencyOverride<IPoeDatabaseReader[]>(
                                new IPoeDatabaseReader[]
                                {
                                    unity.Resolve<StaticPoeDatabaseReader>(),
                                    unity.Resolve<PoeNinjaDatabaseReader>(),
                                }
                            )
                        )));
        }
    }
}