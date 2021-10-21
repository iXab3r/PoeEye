﻿using System.Collections.Generic;
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
                .RegisterSingleton<IWindowHandleProvider, WindowHandleProvider>()
                .RegisterSingleton<IKeyboardLayoutManager, KeyboardLayoutManager>()
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

            Container.RegisterWindowTracker<PassthroughWindowTrackerTitleMatcher>(WellKnownWindows.AllWindows);
            Container.RegisterWindowTracker<MainWindowTrackerTitleMatcher>(WellKnownWindows.MainWindow);
            Container.RegisterFactory<IWindowTracker>(x => x.Resolve<IWindowTracker>(WellKnownWindows.AllWindows));
        }
    }
}