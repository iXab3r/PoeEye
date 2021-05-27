﻿using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using log4net;
using PoeShared.Audio.ViewModels;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.UI;
using PoeShared.UI.Hotkeys;
using PoeShared.Wpf.Services;
using PoeShared.Wpf.UI.ExceptionViewer;
using PoeShared.Wpf.UI.Settings;
using ReactiveUI;
using Unity;
using Unity.Extension;
using Unity.Lifetime;

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
                .RegisterFactory<TaskScheduler>(WellKnownSchedulers.UI, x => taskScheduler, new ContainerControlledLifetimeManager());

            Container
                .RegisterFactory<IScheduler>(WellKnownSchedulers.InputHook, x => x.Resolve<ISchedulerProvider>().GetOrCreate(WellKnownSchedulers.InputHook));

            Container
                .RegisterSingleton<PoeEyeModulesRegistrator>(typeof(IPoeEyeModulesRegistrator), typeof(IPoeEyeModulesEnumerator))
                .RegisterSingleton<IExceptionDialogDisplayer, ExceptionDialogDisplayer>()
                .RegisterSingleton<ISchedulerProvider, SchedulerProvider>()
                .RegisterSingleton<IUserInputFilterConfigurator, UserInputFilterConfigurator>()
                .RegisterSingleton<IApplicationAccessor, ApplicationAccessor>()
                .RegisterSingleton<IConverter<char, Keys>, CharToKeysConverter>()
                .RegisterSingleton<IConverter<Keys, HotkeyGesture>, KeysToHotkeyGestureConverter>()
                .RegisterSingleton<IHotkeyConverter, HotkeyConverter>();

            Container
                .RegisterType<IHotkeyTracker, HotkeyTracker>()
                .RegisterType<IGenericSettingsViewModel, GenericSettingsViewModel>()
                .RegisterType<IRandomPeriodSelector, RandomPeriodSelector>()
                .RegisterType<IAudioNotificationSelectorViewModel, AudioNotificationSelectorViewModel>();
        }
    }
}