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
using PoeShared.Notifications.Services;
using PoeShared.Notifications.ViewModels;
using PoeShared.Profiler;
using PoeShared.RegionSelector.Services;
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
            .RegisterSingleton<IExceptionDialogDisplayer, ExceptionDialogDisplayer>()
            .RegisterSingleton<IExceptionReportingService, ExceptionReportingService>()
            .RegisterSingleton<IUserInputFilterConfigurator, UserInputFilterConfigurator>()
            .RegisterSingleton<IApplicationAccessor, ApplicationAccessor>()
            .RegisterSingleton<INotificationsService, NotificationsService>()
            .RegisterSingleton<CharToKeysConverter>(typeof(IConverter<(char ch, KeyboardLayout layout), Keys>), typeof(IConverter<char, Keys>))
            .RegisterSingleton<IMicrophoneProvider, MicrophoneProvider>()
            .RegisterSingleton<IMessageBoxService, MessageBoxService>()
            .RegisterSingleton<IUserInputBlocker, UserInputBlocker>()
            .RegisterSingleton<IErrorMonitorViewModel, ErrorMonitorViewModel>()
            .RegisterSingleton<IProfilerViewModel, ProfilerViewModel>()
            .RegisterSingleton<IScreenRegionSelectorService, ScreenRegionSelectorService>()
            .RegisterSingleton<IConverter<Keys, HotkeyGesture>, KeysToHotkeyGestureConverter>()
            .RegisterSingleton<IHotkeyConverter>(_ => HotkeyConverter.Instance);
            
        Log.Debug(() => $"Initializing application: {Application.Current}");
        var accessor = Container.Resolve<IApplicationAccessor>();
        Log.Debug(() => $"Application accessor: {accessor}");

        Container.RegisterOverlayController(WellKnownWindows.AllWindows, WellKnownWindows.AllWindows);

        Container
            .RegisterType<IHotkeyTracker, HotkeyTracker>()
            .RegisterType<ISingleInstanceValidationHelper, SingleInstanceValidationHelper>()
            .RegisterType<IWaveOutDeviceSelectorViewModel, WaveOutDeviceSelectorViewModel>()
            .RegisterType<ISelectionAdornerViewModel, SelectionAdornerViewModel>()
            .RegisterType<IBindingsEditorViewModel, BindingsEditorViewModel>()
            .RegisterType<IRegionSelectorViewModel, RegionSelectorViewModel>()
            .RegisterType<IGenericSettingsViewModel, GenericSettingsViewModel>()
            .RegisterType<IRandomPeriodSelector, RandomPeriodSelector>()
            .RegisterType<IReportItemsAggregator, ReportItemsAggregator>()
            .RegisterType<ISwitchableTextEvaluatorViewModel, SwitchableTextEvaluatorViewModel>()
            .RegisterType<IHotkeySequenceEditorController, HotkeySequenceEditorController>()
            .RegisterType<IHotkeySequenceEditorViewModel, HotkeySequenceEditorViewModel>()
            .RegisterType<INotificationContainerViewModel, NotificationContainerViewModel>()
            .RegisterType<IAudioNotificationSelectorViewModel, AudioNotificationSelectorViewModel>();
    }

    private void InitializeSchedulers()
    {
        var defaultDispatcher = Dispatcher.CurrentDispatcher;
        Log.Debug(() => $"Dispatcher set to: {defaultDispatcher}");
        var syncContext = new DispatcherSynchronizationContext(defaultDispatcher);
        Log.Debug(() => $"Synchronization context: {syncContext}");
        SynchronizationContext.SetSynchronizationContext(syncContext);
        var taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        Log.Debug(() => $"Task scheduler: {taskScheduler}");
        Log.Debug(() => $"Capturing {defaultDispatcher} as {WellKnownDispatchers.UI}");
        
        var uiThread = Thread.CurrentThread;
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
                Log.Debug(() => $"Initializing {WellKnownSchedulers.UI} scheduler on {uiDispatcher}");
                return new DispatcherScheduler(uiDispatcher, DispatcherPriority.Normal);
            })
            .RegisterSingleton<IScheduler>(WellKnownSchedulers.UIIdle, x =>
            {
                var uiDispatcher = x.Resolve<Dispatcher>(WellKnownDispatchers.UI);
                Log.Debug(() => $"Initializing {WellKnownSchedulers.UIIdle} scheduler on {uiDispatcher}");
                return new DispatcherScheduler(uiDispatcher, DispatcherPriority.Background);
            })
            .RegisterSingleton<IScheduler>(WellKnownSchedulers.Background, x => ThreadPoolScheduler.Instance.DisableOptimizations())
            .RegisterSingleton<TaskScheduler>(WellKnownSchedulers.UI, x => taskScheduler)
            .RegisterSingleton<IScheduler>(WellKnownSchedulers.RedirectToUI, x => new EnforcedThreadScheduler(uiThread, x.Resolve<IScheduler>(WellKnownSchedulers.UI)))
            .RegisterSingleton<IScheduler>(WellKnownSchedulers.InputHook, x => x.Resolve<ISchedulerProvider>().Add(WellKnownSchedulers.InputHook, ThreadPriority.AboveNormal))
            .RegisterSingleton<IScheduler>(WellKnownSchedulers.SharedThread, x => x.Resolve<ISchedulerProvider>().Add(WellKnownSchedulers.SharedThread, ThreadPriority.Normal))
            .RegisterSingleton<IScheduler>(WellKnownSchedulers.SendInput, x => x.Resolve<ISchedulerProvider>().Add(WellKnownSchedulers.SendInput, ThreadPriority.Highest));
    }
}