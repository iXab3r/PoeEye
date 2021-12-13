using System;
using System.Diagnostics;
using System.Drawing;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Notifications.Services;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PropertyBinder;
using ReactiveUI;
using Unity;

namespace PoeShared.UI
{
    internal sealed class HotkeySequenceEditorController : DisposableReactiveObject, IHotkeySequenceEditorController
    {
        private static readonly IFluentLog Log = typeof(HotkeySequenceEditorController).PrepareLogger();

        private static readonly Binder<HotkeySequenceEditorController> Binder = new();
        private readonly IAppArguments appArguments;
        private readonly IClock clock;
        private readonly IFactory<IHotkeyTracker> hotkeyFactory;
        private readonly IKeyboardEventsSource keyboardEventsSource;
        private readonly IScheduler uiScheduler;
        private readonly IWindowTracker mainWindowTracker;
        private readonly INotificationsService notificationsService;
        private readonly ObservableAsPropertyHelper<TimeSpan> recordDuration;
        private readonly CommandWrapper startRecording;
        private readonly CommandWrapper stopRecording;


        static HotkeySequenceEditorController()
        {
            Binder
                .Bind(x => x.RecordingDuration + x.Owner.TotalDuration < x.Owner.MaxDuration && !x.Owner.MaxDurationExceeded && !x.Owner.MaxItemsExceeded && x.AtLeastOneRecordingTypeEnabled)
                .To(x => x.CanAddItem);
            Binder
                .Bind(x => x.EnableKeyboardRecording || x.EnableMouseClicksRecording || x.MousePositionRecording != MousePositionRecordingType.None)
                .To(x => x.AtLeastOneRecordingTypeEnabled);

            Binder.BindIf(x => x.IsRecording, x => x.Owner.TotalDuration + x.RecordingDuration)
                .Else(x => x.Owner.TotalDuration)
                .To(x => x.TotalDuration);

            Binder.Bind(x => x.stopRecording.IsBusy || x.startRecording.IsBusy || x.IsRecording).To(x => x.IsBusy);
        }

        public HotkeySequenceEditorController(
            [Dependency(WellKnownWindows.AllWindows)] IWindowTracker mainWindowTracker,
            IAppArguments appArguments,
            IClock clock,
            IHotkeySequenceEditorViewModel owner,
            INotificationsService notificationsService,
            IFactory<IHotkeyTracker> hotkeyFactory,
            IKeyboardEventsSource keyboardEventsSource,
            [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.mainWindowTracker = mainWindowTracker;
            this.appArguments = appArguments;
            this.clock = clock;
            Owner = owner;
            this.notificationsService = notificationsService;
            this.hotkeyFactory = hotkeyFactory;
            this.keyboardEventsSource = keyboardEventsSource;
            this.uiScheduler = uiScheduler;

            this.WhenAnyValue(x => x.RecordStartTime)
                .DistinctUntilChanged()
                .Select(x => x != null ? Observable.Interval(TimeSpan.FromMilliseconds(250)) : Observable.Return(0L))
                .Switch()
                .Select(x => DateTime.UtcNow - RecordStartTime ?? TimeSpan.Zero)
                .ToProperty(out recordDuration, this, x => x.RecordingDuration)
                .AddTo(Anchors);

            startRecording = CommandWrapper.Create(StartRecordingExecuted, this.WhenAnyValue(x => x.CanAddItem).ObserveOn(uiScheduler));
            stopRecording = CommandWrapper.Create(StopRecordingExecuted);

            Binder.Attach(this).AddTo(Anchors);
        }

        public bool AtLeastOneRecordingTypeEnabled { get; private set; }

        public bool CanAddItem { get; private set; }

        public TimeSpan RecordingDuration => recordDuration.Value;

        public DateTimeOffset? RecordStartTime { get; private set; }

        public ICommand StartRecording => startRecording;

        public TimeSpan TotalDuration { get; private set; }

        public IHotkeySequenceEditorViewModel Owner { get; }

        public ICommand StopRecording => stopRecording;

        public bool EnableMouseClicksRecording { get; set; } = true;

        public MousePositionRecordingType MousePositionRecording { get; set; }

        public bool EnableKeyboardRecording { get; set; } = true;

        public bool IsRecording { get; private set; }

        public TimeSpan MousePositionRecordingResolution { get; set; } = TimeSpan.FromMilliseconds(250);

        public bool IsBusy { get; set; }

        public Fallback<HotkeyGesture> ToggleRecordingHotkey { get; set; } = new((actualHotkey, defaultHotkey) => actualHotkey == null || actualHotkey.IsEmpty || actualHotkey.Equals(defaultHotkey));

        public IWindowHandle TargetWindow { get; set; }

        public Point? MouseLocation { get; private set; }

        private void StopRecordingExecuted()
        {
            if (!IsRecording)
            {
                return;
            }

            IsRecording = false;
        }

        private async Task StartRecordingExecuted()
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("Already recording");
            }

            IsRecording = true;

            var initialWindow = UnsafeNative.GetForegroundWindow();
            var windowToRecord = TargetWindow;
            using var recordingAnchors = new CompositeDisposable();
            Disposable.Create(StopRecordingExecuted).AddTo(recordingAnchors);

            var cancel = Observable.Merge(
                this.WhenAnyValue(x => x.IsRecording).Where(x => x == false).ToUnit()
            );

            if (windowToRecord != null)
            {
                Log.Debug(() => $"Activating window before recording: {windowToRecord}, previously active: {initialWindow}");
                UnsafeNative.ActivateWindow(windowToRecord.Handle);
            }

            using var notification = new RecordingNotificationViewModel(this).AddTo(recordingAnchors);
            notificationsService.AddNotification(notification).AddTo(recordingAnchors);

            var mouseLocationSource = keyboardEventsSource.WhenMouseMove
                .Select(x => new Point(x.X, x.Y))
                .StartWith(System.Windows.Forms.Cursor.Position)
                .Publish();

            if (MousePositionRecording != MousePositionRecordingType.None && MousePositionRecordingResolution > TimeSpan.Zero)
            {
                mouseLocationSource
                    .Sample(UiConstants.UiThrottlingDelay)
                    .ObserveOn(uiScheduler)
                    .Finally(() => MouseLocation = default)
                    .SubscribeSafe(x => MouseLocation = x, Log.HandleUiException)
                    .AddTo(recordingAnchors);
            }

            mouseLocationSource.Connect().AddTo(recordingAnchors);

            var hotkey = ToggleRecordingHotkey.Value ?? HotkeyGesture.Empty;
            var tracker = hotkeyFactory.Create().AddTo(recordingAnchors);
            tracker.HotkeyMode = HotkeyMode.Hold;
            tracker.SuppressKey = true;
            tracker.Hotkey = hotkey;
            tracker.HandleApplicationKeys = true;
            if (hotkey != HotkeyGesture.Empty)
            {
                await tracker.WhenAnyValue(x => x.IsActive).Where(x => x).ToUnit().Merge(cancel).Take(1);
                tracker.Reset();
            }

            if (!IsRecording)
            {
                Log.Debug("Recording was stopped");
                return;
            }

            RecordStartTime = clock.UtcNow;

            Disposable.Create(() =>
            {
                IsRecording = false;
                RecordStartTime = default;
                Log.Debug(() => $"Restoring window after recording: {initialWindow}");
                UnsafeNative.ActivateWindow(initialWindow);
            }).AddTo(recordingAnchors);

            Observable.Merge(
                    windowToRecord == null ? Observable.Empty<string>() : mainWindowTracker.WhenAnyValue(x => x.ActiveWindowHandle).Where(x => x != windowToRecord.Handle).Select(x => $"Active window changed ! Expected {windowToRecord}, got: {x.ToHexadecimal()} ({UnsafeNative.GetWindowTitle(x)})"),
                    tracker.WhenAnyValue(x => x.IsActive).Where(x => x).Select(x => $"Hotkey {tracker} detected"),
                    this.WhenAnyValue(x => x.CanAddItem).Where(x => !x).Select(x => $"Cannot add more items: {TotalDuration} / {Owner.MaxDuration}, {Owner.TotalCount} / {Owner.MaxItemsCount}"))
                .Take(1)
                .SubscribeSafe(reason =>
                {
                    Log.Debug(() => $"Stopping recording, reason: {reason}");
                    StopRecordingExecuted();
                }, Log.HandleUiException)
                .AddTo(recordingAnchors);

            var sw = new Stopwatch();

            void AddDelayStep()
            {
                if (sw.ElapsedMilliseconds <= 0)
                {
                    return;
                }

                AddActionDelay(sw.ElapsedMilliseconds, isKeyPress: true);
                sw.Restart();
            }

            if (MousePositionRecordingResolution > TimeSpan.Zero)
            {
                if (MousePositionRecording == MousePositionRecordingType.Absolute)
                {
                    mouseLocationSource
                        .Sample(MousePositionRecordingResolution)
                        .DistinctUntilChanged()
                        .ObserveOn(uiScheduler)
                        .TakeUntil(cancel)
                        .SubscribeSafe(x =>
                        {
                            AddDelayStep();
                            var position = windowToRecord == null
                                ? x
                                : new Point(x.X - windowToRecord.ClientBounds.X, x.Y - windowToRecord.ClientBounds.Y);
                            AddMouseMove(position, isRelative: false);
                        }, Log.HandleUiException)
                        .AddTo(recordingAnchors);
                } else if (MousePositionRecording == MousePositionRecordingType.Relative)
                {
                    var recordMouseAbsolutePosition = true; // relative recording still need improvements, it's a bit off
                    if (recordMouseAbsolutePosition)
                    {
                        mouseLocationSource
                            .Select(x => (Point?)x)
                            .Sample(MousePositionRecordingResolution)
                            .DistinctUntilChanged()
                            .ObserveOn(uiScheduler)
                            .TakeUntil(cancel)
                            .WithPrevious()
                            .SubscribeSafe(x =>
                            {
                                if (x.Current == null)
                                {
                                    return;
                                }
                        
                                if (x.Previous == null)
                                {
                                    return;
                                }
                        
                                AddDelayStep();
                                var position = new Point(x.Current.Value.X - x.Previous.Value.X, x.Current.Value.Y - x.Previous.Value.Y);
                                AddMouseMove(position, isRelative: true);
                            }, Log.HandleUiException)
                            .AddTo(recordingAnchors);
                    }
                    else
                    {
                        Point? initialLocation = default;
                        mouseLocationSource
                            .Buffer(MousePositionRecordingResolution)
                            .Select(positions =>
                            {
                                if (positions.Count == 0)
                                {
                                    return default(Point?);
                                }

                                if (initialLocation == null)
                                {
                                    initialLocation = positions[0];
                                }

                                if (positions.Count == 1)
                                {
                                    return default;
                                }

                                var resultX = 0;
                                var resultY = 0;
                                foreach (var point in positions)
                                {
                                    resultX += point.X - initialLocation.Value.X;
                                    resultY += point.Y - initialLocation.Value.Y;
                                    initialLocation = point;
                                }

                                return new Point(resultX, resultY);
                            })
                            .DistinctUntilChanged()
                            .Where(x => x != default && !x.Value.IsEmpty)
                            .ObserveOn(uiScheduler)
                            .TakeUntil(cancel)
                            .SubscribeSafe(x =>
                            {
                                if (x == null)
                                {
                                    return;
                                }

                                AddDelayStep();
                                AddMouseMove(x.Value, isRelative: true);
                            }, Log.HandleUiException)
                            .AddTo(recordingAnchors);
                    }
                }
            }


            if (EnableKeyboardRecording)
            {
                Observable.Merge(
                        keyboardEventsSource.WhenKeyDown.Select(x => new { x.KeyCode, IsDown = true }),
                        keyboardEventsSource.WhenKeyUp.Select(x => new { x.KeyCode, IsDown = false })
                    )
                    .DistinctUntilChanged()
                    .Where(x => !hotkey.Contains(x.KeyCode))
                    .ObserveOn(uiScheduler)
                    .TakeUntil(cancel)
                    .SubscribeSafe(x =>
                    {
                        AddDelayStep();
                        Owner.AddItem.Execute(new HotkeySequenceHotkey
                        {
                            Hotkey = new HotkeyGesture(x.KeyCode.ToInputKey()),
                            IsDown = x.IsDown,
                        });
                        sw.Restart();
                    }, Log.HandleUiException)
                    .AddTo(recordingAnchors);
            }

            if (EnableMouseClicksRecording)
            {
                Observable.Merge(
                        keyboardEventsSource.WhenMouseDown.Select(x => new { x.Button, x.X, x.Y, IsDown = true }),
                        keyboardEventsSource.WhenMouseUp.Select(x => new { x.Button, x.X, x.Y, IsDown = false })
                    )
                    .DistinctUntilChanged()
                    .ObserveOn(uiScheduler)
                    .TakeUntil(cancel)
                    .SubscribeSafe(x =>
                    {
                        var windowHandle = UnsafeNative.WindowFromPoint(new Point(x.X, x.Y));
                        var processId = UnsafeNative.GetProcessIdByWindowHandle(windowHandle);
                        if (processId == appArguments.ProcessId)
                        {
                            return;
                        }

                        AddDelayStep();

                        Owner.AddItem.Execute(new HotkeySequenceHotkey
                        {
                            Hotkey = new HotkeyGesture(x.Button),
                            IsDown = x.IsDown,
                        });
                        sw.Restart();
                    }, Log.HandleUiException).AddTo(recordingAnchors);
            }

            Log.Debug("Awaiting for recording to stop");
            await this.WhenAnyValue(x => x.IsRecording).Where(x => x == false).Take(1);
        }

        private void AddMouseMove(Point? position, bool isRelative)
        {
            Owner.AddItem.Execute(new HotkeySequenceHotkey
            {
                MousePosition = position,
                IsRelative = isRelative
            });
        }

        private void AddActionDelay(double delayInMs, bool isKeyPress)
        {
            AddActionDelay(TimeSpan.FromMilliseconds(delayInMs), isKeyPress);
        }

        private void AddActionDelay(TimeSpan delay, bool isKeyPress)
        {
            Owner.AddItem.Execute(new HotkeySequenceDelay
            {
                Delay = delay,
                IsKeypress = true,
            });
        }
    }
}