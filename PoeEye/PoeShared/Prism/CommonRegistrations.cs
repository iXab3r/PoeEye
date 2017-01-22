using System.Collections.Specialized;
using System.Diagnostics;
using System.Reactive.Concurrency;
using Gma.System.MouseKeyHook;
using PoeShared.Communications;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.PoeTrade.Query;
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
                .RegisterInstance(typeof(IKeyboardMouseEvents), Hook.GlobalEvents(), new ContainerControlledLifetimeManager())
                .RegisterSingleton<IPoeEyeModulesRegistrator, PoeEyeModulesRegistrator>()
                .RegisterSingleton<IPoeEyeModulesEnumerator, PoeEyeModulesRegistrator>()
                .RegisterSingleton<IPoeDatabaseReader, StaticPoeDatabaseReader>()
                .RegisterSingleton<IEqualityComparer<IPoeItem>, PoeItemEqualityComparer>()
                .RegisterSingleton<IConverter<NameValueCollection, string>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton<IProxyProvider, GenericProxyProvider>(new InjectionFactory(unity => new GenericProxyProvider()))
                .RegisterSingleton<IRandomNumberGenerator, RandomNumberGenerator>()
                .RegisterSingleton<IImagesCacheService, ImagesCacheService>()
                .RegisterSingleton<IOverlayWindowController, OverlayWindowController>(WellKnownWindows.PathOfExileWindow);

            Container
                .RegisterType<IScheduler>(WellKnownSchedulers.UI, new InjectionFactory(x => RxApp.MainThreadScheduler))
                .RegisterType<IScheduler>(WellKnownSchedulers.Background, new InjectionFactory(x => RxApp.TaskpoolScheduler))
                .RegisterType<IPoeLiveHistoryProvider, PoeLiveHistoryProvider>()
                .RegisterType<IHttpClient, GenericHttpClient>()
                .RegisterType<IPoeApiWrapper, PoeApiWrapper>()
                .RegisterType<IOverlayWindowController, OverlayWindowController>()
                .RegisterType(typeof (IFactory<,,>), typeof (Factory<,,>))
                .RegisterType(typeof (IFactory<,>), typeof (Factory<,>))
                .RegisterType(typeof (IFactory<>), typeof (Factory<>));

            Container
                .RegisterType<IImageViewModel, ImageViewModel>();

            Container.RegisterWindowTracker(WellKnownWindows.AllWindows, () => ".*");
            Container.RegisterWindowTracker(WellKnownWindows.MainWindow, () => Process.GetCurrentProcess().MainWindowTitle);
            Container.RegisterWindowTracker(WellKnownWindows.PathOfExileWindow, () => $"^Path of Exile$");

            Container.RegisterOverlayController(
                WellKnownOverlays.PathOfExileLayeredOverlay, 
                WellKnownWindows.PathOfExileWindow, 
                OverlayMode.Layered);

            Container.RegisterOverlayController(
                WellKnownOverlays.PathOfExileTransparentOverlay,
                WellKnownWindows.PathOfExileWindow,
                OverlayMode.Transparent);

            Container.RegisterOverlayController(
               WellKnownOverlays.AllWindowsLayeredOverlay,
               WellKnownWindows.AllWindows,
               OverlayMode.Layered);
        }
    }
}