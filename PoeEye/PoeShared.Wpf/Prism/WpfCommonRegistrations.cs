﻿using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using log4net;
using PoeShared.Audio.Models;
using PoeShared.Audio.ViewModels;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.Services;
using PoeShared.UI;
using PoeShared.Wpf.Scaffolding;
using PoeShared.Wpf.Services;
using ReactiveUI;
using Unity;
using Unity.Extension;
using Unity.Lifetime;
using Application = System.Windows.Application;

namespace PoeShared.Prism
{
    public sealed class WpfCommonRegistrations : UnityContainerExtension
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WpfCommonRegistrations));

        protected override void Initialize()
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            Log.Debug($"Dispatcher set to: {dispatcher}");
            var syncContext = new DispatcherSynchronizationContext(dispatcher);
            Log.Debug($"Synchronization context: {syncContext}");
            SynchronizationContext.SetSynchronizationContext(syncContext);
            var taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Log.Debug($"Task scheduler: {taskScheduler}");
            
            Log.Debug($"Capturing {dispatcher} as {WellKnownDispatchers.UI}");
            Container
                .RegisterFactory<Dispatcher>(WellKnownDispatchers.UI, x => dispatcher, new ContainerControlledLifetimeManager())
                .RegisterFactory<IScheduler>(WellKnownSchedulers.UI, x =>
                {
                    var uiDispatcher = x.Resolve<Dispatcher>(WellKnownDispatchers.UI);
                    Log.Debug($"Initializing {WellKnownSchedulers.UI} scheduler on {uiDispatcher}");
                    return new DispatcherScheduler(uiDispatcher, DispatcherPriority.Normal);
                }, new ContainerControlledLifetimeManager())
                .RegisterFactory<IScheduler>(WellKnownSchedulers.UIIdle, x =>
                {
                    var uiDispatcher = x.Resolve<Dispatcher>(WellKnownDispatchers.UI);
                    Log.Debug($"Initializing {WellKnownSchedulers.UIIdle} scheduler on {uiDispatcher}");
                    return new DispatcherScheduler(uiDispatcher, DispatcherPriority.ApplicationIdle);
                }, new ContainerControlledLifetimeManager())
                .RegisterFactory<IScheduler>(WellKnownSchedulers.Background, x => RxApp.TaskpoolScheduler, new ContainerControlledLifetimeManager())
                .RegisterFactory<TaskScheduler>(WellKnownSchedulers.UI, x => taskScheduler, new ContainerControlledLifetimeManager())
                .RegisterFactory<IUiSharedResourceLatch>(container =>
                {
                    var result = container.Resolve<UiSharedResourceLatch>();
                    result.Name = "Ui";
                    return result;
                }, new ContainerControlledLifetimeManager());

            Container
                .RegisterFactory<IScheduler>(WellKnownSchedulers.InputHook, x => x.Resolve<ISchedulerProvider>().GetOrCreate(WellKnownSchedulers.InputHook));

            Container
                .RegisterSingleton<PoeEyeModulesRegistrator>(typeof(IPoeEyeModulesRegistrator), typeof(IPoeEyeModulesEnumerator))
                .RegisterSingleton<IExceptionDialogDisplayer, ExceptionDialogDisplayer>()
                .RegisterSingleton<ISchedulerProvider, SchedulerProvider>()
                .RegisterSingleton<IUserInputFilterConfigurator, UserInputFilterConfigurator>()
                .RegisterSingleton<IApplicationAccessor, ApplicationAccessor>()
                .RegisterSingleton<INotificationsService, NotificationsService>()
                .RegisterSingleton<IConverter<char, Keys>, CharToKeysConverter>()
                .RegisterSingleton<IMicrophoneProvider, MicrophoneProvider>()
                .RegisterSingleton<IConverter<Keys, HotkeyGesture>, KeysToHotkeyGestureConverter>()
                .RegisterInstance<IHotkeyConverter>(HotkeyConverter.Instance, new ContainerControlledLifetimeManager());
            Log.Debug($"Initializing application: {Application.Current}");
            var accessor = Container.Resolve<IApplicationAccessor>();
            Log.Debug($"Application accessor: {accessor}");

            Container.RegisterOverlayController(WellKnownWindows.AllWindows, WellKnownWindows.AllWindows);
            
            Container
                .RegisterType<IHotkeyTracker, HotkeyTracker>()
                .RegisterType<IWaveOutDeviceSelectorViewModel, WaveOutDeviceSelectorViewModel>()
                .RegisterType<IGenericSettingsViewModel, GenericSettingsViewModel>()
                .RegisterType<IRandomPeriodSelector, RandomPeriodSelector>()
                .RegisterType<IHotkeySequenceEditorViewModel, HotkeySequenceEditorViewModel>()
                .RegisterType<IAudioNotificationSelectorViewModel, AudioNotificationSelectorViewModel>();
        }
    }
}