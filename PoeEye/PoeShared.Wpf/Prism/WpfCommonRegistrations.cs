﻿using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using log4net;
using PoeShared.Audio.Models;
using PoeShared.Audio.ViewModels;
using PoeShared.Dialogs.Services;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.RegionSelector.ViewModels;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Notifications;
using PoeShared.Notifications.Services;
using PoeShared.Notifications.ViewModels;
using PoeShared.Scaffolding.WPF;
using PoeShared.Services;
using PoeShared.UI;
using PoeShared.Wpf.Scaffolding;
using PoeShared.Wpf.Services;
using ReactiveUI;
using Unity;
using Unity.Extension;
using Application = System.Windows.Application;

namespace PoeShared.Prism
{
    public sealed class WpfCommonRegistrations : UnityContainerExtension
    {
        private static readonly IFluentLog Log = typeof(WpfCommonRegistrations).PrepareLogger();

        protected override void Initialize()
        {
            InitializeSchedulers();
            Container
                .RegisterSingleton<IUiSharedResourceLatch>(container =>
                {
                    var result = container.Resolve<UiSharedResourceLatch>();
                    result.Name = "Ui";
                    return result;
                });

            Container
                .RegisterSingleton<PoeEyeModulesRegistrator>(typeof(IPoeEyeModulesRegistrator), typeof(IPoeEyeModulesEnumerator))
                .RegisterSingleton<IExceptionDialogDisplayer, ExceptionDialogDisplayer>()
                .RegisterSingleton<IUserInputFilterConfigurator, UserInputFilterConfigurator>()
                .RegisterSingleton<IApplicationAccessor, ApplicationAccessor>()
                .RegisterSingleton<INotificationsService, NotificationsService>()
                .RegisterSingleton<IConverter<char, Keys>, CharToKeysConverter>()
                .RegisterSingleton<IMicrophoneProvider, MicrophoneProvider>()
                .RegisterSingleton<IMessageBoxService, MessageBoxService>()
                .RegisterSingleton<IConverter<Keys, HotkeyGesture>, KeysToHotkeyGestureConverter>()
                .RegisterSingleton<IHotkeyConverter>(_ => HotkeyConverter.Instance);
            
            Log.Debug($"Initializing application: {Application.Current}");
            var accessor = Container.Resolve<IApplicationAccessor>();
            Log.Debug($"Application accessor: {accessor}");

            Container.RegisterOverlayController(WellKnownWindows.AllWindows, WellKnownWindows.AllWindows);

            Container
                .RegisterType<IHotkeyTracker, HotkeyTracker>()
                .RegisterType<IWaveOutDeviceSelectorViewModel, WaveOutDeviceSelectorViewModel>()
                .RegisterType<ISelectionAdornerViewModel, SelectionAdornerViewModel>()
                .RegisterType<ISelectionAdornerViewModel, SelectionAdornerViewModel>()
                .RegisterType<IRegionSelectorViewModel, RegionSelectorViewModel>()
                .RegisterType<IGenericSettingsViewModel, GenericSettingsViewModel>()
                .RegisterType<IRandomPeriodSelector, RandomPeriodSelector>()
                .RegisterType<IHotkeySequenceEditorViewModel, HotkeySequenceEditorViewModel>()
                .RegisterType<INotificationContainerViewModel, NotificationContainerViewModel>()
                .RegisterType<IAudioNotificationSelectorViewModel, AudioNotificationSelectorViewModel>();
        }

        private void InitializeSchedulers()
        {
            var defaultDispatcher = Dispatcher.CurrentDispatcher;
            Log.Debug($"Dispatcher set to: {defaultDispatcher}");
            var syncContext = new DispatcherSynchronizationContext(defaultDispatcher);
            Log.Debug($"Synchronization context: {syncContext}");
            SynchronizationContext.SetSynchronizationContext(syncContext);
            var taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Log.Debug($"Task scheduler: {taskScheduler}");
            Log.Debug($"Capturing {defaultDispatcher} as {WellKnownDispatchers.UI}");
            Container
                .RegisterSingleton<ISchedulerProvider>(x =>
                {
                    var result = SchedulerProvider.Instance;
                    if (result is SchedulerProvider provider)
                    {
                        provider.Initialize(x);
                    }

                    return result;
                })
                .RegisterSingleton<Dispatcher>(WellKnownDispatchers.UI, x => defaultDispatcher)
                .RegisterSingleton<IScheduler>(WellKnownSchedulers.UI, x =>
                {
                    var uiDispatcher = x.Resolve<Dispatcher>(WellKnownDispatchers.UI);
                    Log.Debug($"Initializing {WellKnownSchedulers.UI} scheduler on {uiDispatcher}");
                    return new DispatcherScheduler(uiDispatcher, DispatcherPriority.Normal);
                })
                .RegisterSingleton<IScheduler>(WellKnownSchedulers.UIIdle, x =>
                {
                    var uiDispatcher = x.Resolve<Dispatcher>(WellKnownDispatchers.UI);
                    Log.Debug($"Initializing {WellKnownSchedulers.UIIdle} scheduler on {uiDispatcher}");
                    return new DispatcherScheduler(uiDispatcher, DispatcherPriority.ApplicationIdle);
                })
                .RegisterSingleton<IScheduler>(WellKnownSchedulers.Background, x => RxApp.TaskpoolScheduler)
                .RegisterSingleton<TaskScheduler>(WellKnownSchedulers.UI, x => taskScheduler)
                .RegisterSingleton<IScheduler>(WellKnownSchedulers.InputHook, x => x.Resolve<ISchedulerProvider>().GetOrCreate(WellKnownSchedulers.InputHook))
                .RegisterSingleton<IScheduler>(WellKnownSchedulers.SharedThread, x => x.Resolve<ISchedulerProvider>().GetOrCreate(WellKnownSchedulers.SharedThread));
        }
    }
}