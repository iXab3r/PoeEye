using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Text.RegularExpressions;
using PoeShared.Audio.Services;
using PoeShared.Communications;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Native.Scaffolding;
using PoeShared.Resources.Notifications;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Services;
using PoeShared.WindowSeekers;
using ReactiveUI;
using Unity;
using Unity.Extension;
using Unity.Lifetime;
using Unity.Resolution;

namespace PoeShared.Prism
{
    public sealed class NativeRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IStartupManager, StartupManager>()
                .RegisterSingleton<IConverter<NameValueCollection, string>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton<IConverter<NameValueCollection, IEnumerable<KeyValuePair<string, string>>>, NameValueCollectionToQueryStringConverter>()
                .RegisterFactory<IKeyboardMouseEventsProvider>(x => x.Resolve<KeyboardMouseEventsProvider>(), new ContainerControlledLifetimeManager())
                .RegisterSingleton<IKeyboardEventsSource, KeyboardEventsSource>()
                .RegisterSingleton<IClipboardManager, ClipboardManager>()
                .RegisterSingleton<IAudioNotificationsManager, AudioNotificationsManager>()
                .RegisterSingleton<IAudioPlayer, AudioPlayer>()
                .RegisterSingleton<IUserInputBlocker, UserInputBlocker>()
                .RegisterSingleton<IWindowHandleProvider, WindowHandleProvider>()
                .RegisterSingleton<IConverter<NameValueCollection, string>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton<IConverter<NameValueCollection, IEnumerable<KeyValuePair<string, string>>>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton<IFactory<IWinEventHookWrapper, WinEventHookArguments>, WinEventHookWrapperFactory>();

            Container
                .RegisterType<IWindowSeeker, TaskWindowSeeker>()
                .RegisterType<IHttpClient, GenericHttpClient>();
            
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                Container.RegisterSingleton<IAppArguments, AppArgumentsForWindows>();
            }

            Container.RegisterSingleton<IFileSoundLibrarySource, FileSoundLibrarySource>();
            Container.RegisterSingleton<IEmbeddedSoundLibrarySource, EmbeddedSoundLibrarySource>();
            Container.RegisterFactory<ISoundLibrarySource>(
                unity => unity.Resolve<ComplexSoundLibrary>(
                    new DependencyOverride<ISoundLibrarySource[]>(
                        new ISoundLibrarySource[]
                        {
                            unity.Resolve<IFileSoundLibrarySource>(),
                            unity.Resolve<IEmbeddedSoundLibrarySource>()
                        }
                    )),new ContainerControlledLifetimeManager());

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
        }
    }
}