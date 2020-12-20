using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Text.RegularExpressions;
using PoeShared.Audio.Services;
using PoeShared.Communications;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Native.Resources.Notifications;
using PoeShared.Native.Scaffolding;
using PoeShared.Scaffolding;
using PoeShared.Services;
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
                .RegisterSingleton<IKeyboardEventsSource, KeyboardEventsSource>()
                .RegisterSingleton<IClipboardManager, ClipboardManager>()
                .RegisterSingleton<IComparisonService, ComparisonService>()
                .RegisterSingleton<IAudioNotificationsManager, AudioNotificationsManager>()
                .RegisterSingleton<IAudioPlayer, AudioPlayer>()
                .RegisterSingleton<IConfigSerializer, JsonConfigSerializer>()
                .RegisterSingleton<IConverter<NameValueCollection, string>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton<IConverter<NameValueCollection, IEnumerable<KeyValuePair<string, string>>>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton(typeof(IConfigProvider<>), typeof(GenericConfigProvider<>))
                .RegisterSingleton<IFactory<IWinEventHookWrapper, WinEventHookArguments>, WinEventHookWrapperFactory>();

            Container
                .RegisterType<IHttpClient, GenericHttpClient>();

            Container.RegisterFactory<IAppArguments>(container => AppArguments.Instance);

            Container.RegisterSingleton<IFileSoundLibrarySource, FileSoundLibrarySource>();
            Container.RegisterFactory<ISoundLibrarySource>(
                unity => unity.Resolve<ComplexSoundLibrary>(
                    new DependencyOverride<ISoundLibrarySource[]>(
                        new ISoundLibrarySource[]
                        {
                            unity.Resolve<IFileSoundLibrarySource>(),
                            unity.Resolve<EmbeddedSoundLibrarySource>()
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