using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Text.RegularExpressions;
using PoeShared.Audio.Services;
using PoeShared.Audio.ViewModels;
using PoeShared.Communications;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Resources.Notifications;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.UI.Hotkeys;
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
                .RegisterSingleton<IStartupManager, StartupManager>()
                .RegisterSingleton<PoeEyeModulesRegistrator>(typeof(IPoeEyeModulesRegistrator), typeof(IPoeEyeModulesEnumerator))
                .RegisterSingleton(typeof(IConfigProvider<>), typeof(GenericConfigProvider<>))
                .RegisterSingleton<IConverter<NameValueCollection, string>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton<IConverter<NameValueCollection, IEnumerable<KeyValuePair<string, string>>>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton<IRandomNumberGenerator, RandomNumberGenerator>()
                .RegisterSingleton<IKeyboardEventsSource, KeyboardEventsSource>()
                .RegisterSingleton<ISchedulerProvider, SchedulerProvider>()
                .RegisterSingleton<IClipboardManager, ClipboardManager>()
                .RegisterSingleton<IConfigSerializer, JsonConfigSerializer>()
                .RegisterSingleton<IHotkeyConverter, HotkeyConverter>()
                .RegisterSingleton<IAudioNotificationsManager, AudioNotificationsManager>()
                .RegisterSingleton<IAudioPlayer, AudioPlayer>()
                .RegisterSingleton<IFactory<IWinEventHookWrapper, WinEventHookArguments>, WinEventHookWrapperFactory>();

            Container
                .RegisterType<IScheduler>(WellKnownSchedulers.UI, new InjectionFactory(x => RxApp.MainThreadScheduler))
                .RegisterType<IScheduler>(WellKnownSchedulers.Background, new InjectionFactory(x => RxApp.TaskpoolScheduler))
                .RegisterType<IHttpClient, GenericHttpClient>()
                .RegisterType<IAudioNotificationSelectorViewModel, AudioNotificationSelectorViewModel>()
                .RegisterType(typeof(IFactory<,,>), typeof(Factory<,,>))
                .RegisterType(typeof(IFactory<,>), typeof(Factory<,>))
                .RegisterType(typeof(IFactory<>), typeof(Factory<>));

            Container.RegisterWindowTracker(WellKnownWindows.AllWindows, () => ".*");
            Container.RegisterWindowTracker(WellKnownWindows.MainWindow, () =>
            {
                //TODO Optimize MainWindowTitle resolution
                var mainWindowTitle = Process.GetCurrentProcess().MainWindowTitle;
                if (string.IsNullOrEmpty(mainWindowTitle))
                {
                    return string.Empty;
                }
                var regex = $"^{Regex.Escape(mainWindowTitle)}$";
                return regex;
            });
            Container.RegisterWindowTracker(WellKnownWindows.PathOfExileWindow, () => "^Path of Exile$");
            
            
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