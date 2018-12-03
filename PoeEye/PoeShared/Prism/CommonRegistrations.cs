using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Text.RegularExpressions;
using Gma.System.MouseKeyHook;
using PoeShared.Audio;
using PoeShared.Audio.Services;
using PoeShared.Audio.ViewModels;
using PoeShared.Common;
using PoeShared.Communications;
using PoeShared.Converters;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Resources.Notifications;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI.Models;
using PoeShared.UI.ViewModels;
using ProxyProvider;
using ReactiveUI;
using TypeConverter;
using Unity;
using Unity.Extension;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Resolution;

namespace PoeShared.Prism
{
    public sealed class CommonRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IClock, Clock>()
                .RegisterSingleton<PoeEyeModulesRegistrator>(typeof(IPoeEyeModulesRegistrator), typeof(IPoeEyeModulesEnumerator))
                .RegisterSingleton(typeof(IConfigProvider<>), typeof(GenericConfigProvider<>))
                .RegisterSingleton<IConverter<NameValueCollection, string>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton<IConverter<NameValueCollection, IEnumerable<KeyValuePair<string, string>>>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton<IProxyProvider>(new InjectionFactory(unity => new GenericProxyProvider()))
                .RegisterSingleton<IRandomNumberGenerator, RandomNumberGenerator>()
                .RegisterSingleton<IImagesCacheService, ImagesCacheService>()
                .RegisterSingleton<IKeyboardEventsSource>(
                    new InjectionFactory(x => x.Resolve<KeyboardEventsSource>(new DependencyOverride(typeof(IKeyboardMouseEvents), Hook.GlobalEvents()))))
                .RegisterSingleton<ISchedulerProvider, SchedulerProvider>()
                .RegisterSingleton<IClipboardManager, ClipboardManager>()
                .RegisterSingleton<IConfigSerializer, JsonConfigSerializer>()
                .RegisterSingleton<IAudioPlayer, AudioPlayer>()
                .RegisterSingleton<IAudioNotificationsManager, AudioNotificationsManager>()
                .RegisterSingleton<IFactory<IWinEventHookWrapper, WinEventHookArguments>, WinEventHookWrapperFactory>()
                .RegisterSingleton<IOverlayWindowController, OverlayWindowController>(WellKnownWindows.PathOfExileWindow);

            Container
                .RegisterType<IScheduler>(WellKnownSchedulers.UI, new InjectionFactory(x => RxApp.MainThreadScheduler))
                .RegisterType<IScheduler>(WellKnownSchedulers.Background, new InjectionFactory(x => RxApp.TaskpoolScheduler))
                .RegisterType<IHttpClient, GenericHttpClient>()
                .RegisterType<IPageParameterDataViewModel, PageParameterDataViewModel>()
                .RegisterType<IOverlayWindowController, OverlayWindowController>()
                .RegisterType<IAudioNotificationSelectorViewModel, AudioNotificationSelectorViewModel>()
                .RegisterType(typeof(IFactory<,,>), typeof(Factory<,,>))
                .RegisterType(typeof(IFactory<,>), typeof(Factory<,>))
                .RegisterType(typeof(IFactory<>), typeof(Factory<>));

            Container
                .RegisterType<IImageViewModel, ImageViewModel>();

            Container.RegisterWindowTracker(WellKnownWindows.AllWindows, () => ".*");
            Container.RegisterWindowTracker(WellKnownWindows.MainWindow, () =>
            {
                var mainWindowTitle = Process.GetCurrentProcess().MainWindowTitle;
                return $"^{Regex.Escape(mainWindowTitle)}$";
            });
            Container.RegisterWindowTracker(WellKnownWindows.PathOfExileWindow, () => "^Path of Exile$");

            Container.RegisterOverlayController(
                WellKnownOverlays.PathOfExileOverlay,
                WellKnownWindows.PathOfExileWindow);

            Container.RegisterOverlayController(
                WellKnownOverlays.AllWindowsLayeredOverlay,
                WellKnownWindows.AllWindows);
            
            Container
                .RegisterType<ISoundLibrarySource>(
                    new ContainerControlledLifetimeManager(),
                    new InjectionFactory(
                        unity => unity.Resolve<ComplexSoundLibrary>(
                            new DependencyOverride<ISoundLibrarySource[]>(
                                new ISoundLibrarySource[]
                                {
                                    unity.Resolve<FileSoundLibrarySource>(),
                                    unity.Resolve<EmbeddedSoundLibrarySource>()
                                }
                            )
                        )));
        }
    }
}