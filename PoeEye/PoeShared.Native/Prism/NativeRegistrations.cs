using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using PoeShared.Audio.Services;
using PoeShared.Communications;
using PoeShared.Modularity;
using PoeShared.Native.Scaffolding;
using PoeShared.Resources.Notifications;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.WindowSeekers;
using Unity;
using Unity.Extension;
using Unity.Lifetime;
using Unity.Resolution;

namespace PoeShared.Prism;

public sealed class NativeRegistrations : UnityContainerExtension
{
    protected override void Initialize()
    {
        Container
            .RegisterSingleton<IStartupManager, StartupManager>()
            .RegisterSingleton<IConverter<NameValueCollection, string>, NameValueCollectionToQueryStringConverter>()
            .RegisterSingleton<IConverter<NameValueCollection, IEnumerable<KeyValuePair<string, string>>>, NameValueCollectionToQueryStringConverter>()
            .RegisterSingleton<IKeyboardMouseEventsProvider>(x => x.Resolve<KeyboardMouseEventsProvider>())
            .RegisterSingleton<IKeyboardEventsSource, KeyboardEventsSource>()
            .RegisterSingleton<IClipboardManager, ClipboardManager>()
            .RegisterSingleton<IAudioNotificationsManager, AudioNotificationsManager>()
            .RegisterSingleton<IAudioPlayer, AudioPlayer>()
            .RegisterSingleton<IWindowHandleProvider, WindowHandleProvider>()
            .RegisterSingleton<IKeyboardLayoutManager, KeyboardLayoutManager>()
            .RegisterSingleton<ISevenZipWrapper, SevenZipWrapper>()
            .RegisterSingleton<ICredentialManager, MeziantouCredentialManager>()
            .RegisterSingleton<IForegroundWindowTracker, ForegroundWindowTracker>()
            .RegisterSingleton<IWindowBoundsTrackerFactory, WindowBoundsTrackerFactory>()
            .RegisterSingleton<IProcessAudioController, ProcessAudioController>()
            .RegisterSingleton<IConverter<NameValueCollection, string>, NameValueCollectionToQueryStringConverter>()
            .RegisterSingleton<IConverter<NameValueCollection, IEnumerable<KeyValuePair<string, string>>>, NameValueCollectionToQueryStringConverter>()
            .RegisterSingleton<IFactory<IWinEventHookWrapper, WinEventHookArguments>, WinEventHookWrapperFactory>();

        Container
            .RegisterType<IPerformanceMetricsProvider, PerformanceMetricsProvider>()
            .RegisterType<IWindowSeeker, TaskWindowSeeker>()
            .RegisterType<IHttpClient, GenericHttpClient>();

        Container.RegisterFactory<IWindowBoundsTracker>(x => x.Resolve<IWindowBoundsTrackerFactory>().CreateTracker());
            
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            Container.RegisterSingleton<IAppArguments, AppArgumentsForWindows>();
        }

        Container.RegisterSingleton<IFileSoundLibrarySource, FileSoundLibrarySource>();
        Container.RegisterSingleton<IEmbeddedSoundLibrarySource, EmbeddedSoundLibrarySource>();
        Container.RegisterSingleton<ISoundLibrarySource>(
            unity => unity.Resolve<ComplexSoundLibrary>(
                new DependencyOverride<ISoundLibrarySource[]>(
                    new ISoundLibrarySource[]
                    {
                        unity.Resolve<IFileSoundLibrarySource>(),
                        unity.Resolve<IEmbeddedSoundLibrarySource>()
                    }
                )));

        Container.RegisterWindowTracker<PassthroughWindowTrackerMatcher>(WellKnownWindows.AllWindows);
        Container.RegisterWindowTracker<MainWindowTrackerTitleMatcher>(WellKnownWindows.MainWindow);
        Container.RegisterWindowTracker<OverlayWindowMatcher>(WellKnownWindows.OverlayWindow);
        Container.RegisterSingleton<IWindowTracker>(x => x.Resolve<IWindowTracker>(WellKnownWindows.AllWindows));
    }
}