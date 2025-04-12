using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using PoeShared.Audio.Models;
using PoeShared.Dialogs.Services;
using PoeShared.Modularity;
using PoeShared.RegionSelector.ViewModels;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Notifications.Services;
using PoeShared.Notifications.ViewModels;
using PoeShared.Profiler;
using PoeShared.RegionSelector;
using PoeShared.RegionSelector.Services;
using PoeShared.Reporting;
using PoeShared.Services;
using PoeShared.UI;
using PoeShared.UI.Evaluators;
using Unity;
using Unity.Extension;
using Application = System.Windows.Application;

namespace PoeShared.Prism;

public sealed class WpfCommonRegistrations : UnityContainerExtension
{
    private static readonly IFluentLog Log = typeof(WpfCommonRegistrations).PrepareLogger();

    protected override void Initialize()
    {
        InitializeSchedulers();
        Container
            .RegisterSingleton<IUiSharedResourceLatch, UiSharedResourceLatch>();

        Container
            .RegisterSingleton<PoeEyeModulesRegistrator>(typeof(IPoeEyeModulesRegistrator), typeof(IPoeEyeModulesEnumerator))
            .RegisterSingleton<IErrorReportingService, ErrorReportingService>()
            .RegisterSingleton<IUserInputFilterConfigurator, UserInputFilterConfigurator>()
            .RegisterSingleton<IApplicationAccessor, ApplicationAccessor>()
            .RegisterSingleton<SafeModeService>(typeof(ISafeModeService))
            .RegisterSingleton<INotificationsService, NotificationsService>()
            .RegisterSingleton<CharToKeysConverter>(typeof(IConverter<(char ch, KeyboardLayout layout), Keys>), typeof(IConverter<char, Keys>))
            .RegisterSingleton<IMMCaptureDeviceProvider, MMCaptureDeviceProvider>()
            .RegisterSingleton<IMMRenderDeviceProvider, MMRenderDeviceProvider>()
            .RegisterSingleton<IMessageBoxService, MessageBoxService>()
            .RegisterSingleton<IUserInputBlocker, UserInputBlocker>()
            .RegisterSingleton<IErrorMonitorViewModel, ErrorMonitorViewModel>()
            .RegisterSingleton<IProfilerService, PerformanceProfiler>()
            .RegisterSingleton<IScreenRegionSelectorService, ScreenRegionSelectorService>()
            .RegisterSingleton<IWindowRepository, WindowRepository>()
            .RegisterSingleton<IConverter<Keys, HotkeyGesture>, KeysToHotkeyGestureConverter>()
            .RegisterSingleton<IHotkeyConverter>(_ => HotkeyConverter.Instance);
            
        Container.RegisterOverlayController(WellKnownWindows.AllWindows, WellKnownWindows.AllWindows);

        Container
            .RegisterType<IHotkeyTracker, HotkeyTracker>()
            .RegisterType<IProfilerViewModel, PerformanceProfilerViewModel>()
            .RegisterType<ISingleInstanceValidationHelper, SingleInstanceValidationHelper>()
            .RegisterType<IWaveOutDeviceSelectorViewModel, WaveOutDeviceSelectorViewModel>()
#pragma warning disable CS0618 // legacy registration
            .RegisterType<ISelectionAdornerLegacy, SelectionAdornerLegacy>()
#pragma warning restore CS0618
            .RegisterType<IBindingsEditorViewModel, BindingsEditorViewModel>()
            .RegisterType<IWindowRegionSelector, WindowRegionSelector>()
            .RegisterType<IGenericSettingsViewModel, GenericSettingsViewModel>()
            .RegisterType<IRandomPeriodSelector, RandomPeriodSelector>()
            .RegisterType<IReportItemsAggregator, ReportItemsAggregator>()
            .RegisterType<ISelectionAdorner, SelectionAdorner>()
            .RegisterType<IOpenFileDialog, WpfFileDialog>()
            .RegisterType<ISaveFileDialog, WpfFileDialog>()
            .RegisterType<IFolderBrowserDialog, Win32FolderBrowserDialog>()
            .RegisterType<IOverlayWindowController, OverlayWindowController>()
            .RegisterType<ITrackedOverlayWindowController, TrackedOverlayWindowController>()
            .RegisterType<IExceptionDialogDisplayer, ExceptionDialogDisplayer>()
            .RegisterType<ISwitchableTextEvaluatorViewModel, SwitchableTextEvaluatorViewModel>()
            .RegisterType<INotificationContainerViewModel, NotificationContainerViewModel>()
            .RegisterType<IAudioNotificationSelectorViewModel, AudioNotificationSelectorViewModel>();
    }

    private void InitializeSchedulers()
    {
        var uiDispatcher = Dispatcher.CurrentDispatcher;
        Log.Info($"Dispatcher set to: {uiDispatcher}");
        Log.Info($"Capturing {uiDispatcher} as {WellKnownDispatchers.UI}");
        
        var syncContext = new DispatcherSynchronizationContext(uiDispatcher);
        Log.Info($"Synchronization context: {syncContext}");
        SynchronizationContext.SetSynchronizationContext(syncContext);
        var taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        
        Log.Info($"Task scheduler: {taskScheduler}");

        //FIXME For stability reasons this is disabled for now - should test other changes related to multiple dispatchers first
        //var overlayDispatcher = SchedulerProvider.Instance.AddDispatcher(WellKnownDispatchers.UIOverlay, ThreadPriority.Normal);
        
        var uiThread = Thread.CurrentThread;
        Container
            .RegisterSingleton<ISchedulerProvider>(x => SchedulerProvider.Instance)
            .RegisterSingleton<Dispatcher>(WellKnownDispatchers.UI, x => uiDispatcher)
            .RegisterSingleton<IScheduler>(WellKnownSchedulers.UI, x =>
            {
                Log.Debug($"Initializing {WellKnownSchedulers.UI} scheduler on {uiDispatcher}");
                return new DispatcherScheduler(uiDispatcher, DispatcherPriority.Normal);
            })
            .RegisterSingleton<TaskScheduler>(WellKnownSchedulers.UI, x => taskScheduler)
            .RegisterSingleton<IScheduler>(WellKnownSchedulers.UIIdle, x =>
            {
                Log.Debug($"Initializing {WellKnownSchedulers.UIIdle} scheduler on {uiDispatcher}");
                return new DispatcherScheduler(uiDispatcher, DispatcherPriority.Background);
            })
            .RegisterSingleton<IScheduler>(WellKnownSchedulers.Background, x => ThreadPoolScheduler.Instance.DisableOptimizations())
            .RegisterSingleton<IScheduler>(WellKnownSchedulers.RedirectToUI, x => new EnforcedThreadScheduler(uiThread, x.Resolve<IScheduler>(WellKnownSchedulers.UI)))
            .RegisterSingleton<IScheduler>(WellKnownSchedulers.InputHook, x => x.Resolve<ISchedulerProvider>().Add(WellKnownSchedulers.InputHook, ThreadPriority.Highest))
            .RegisterSingleton<IScheduler>(WellKnownSchedulers.SharedThread, x => x.Resolve<ISchedulerProvider>().Add(WellKnownSchedulers.SharedThread, ThreadPriority.Normal))
            .RegisterSingleton<Dispatcher>(WellKnownDispatchers.UIOverlay, x => x.Resolve<Dispatcher>(WellKnownDispatchers.UI))
            .RegisterSingleton<IScheduler>(WellKnownSchedulers.UIOverlay, x => x.Resolve<IScheduler>(WellKnownSchedulers.UI));

        var schedulerProvider = Container.Resolve<ISchedulerProvider>();
        if (schedulerProvider is SchedulerProvider provider)
        {
            Log.Info($"Initializing scheduler provider: {provider}");
            provider.Initialize(Container);
        }
        else
        {
            throw new InvalidOperationException($"Something went wrong - seems that type of {typeof(ISchedulerProvider)} is not {typeof(SchedulerProvider)}, but {schedulerProvider.GetType()}");
        }
    }
}