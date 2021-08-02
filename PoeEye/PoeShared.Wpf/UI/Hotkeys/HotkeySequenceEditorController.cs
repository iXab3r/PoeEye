﻿using System;
using System.Diagnostics;
using System.Drawing;
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
        private readonly IWindowTracker mainWindowTracker;
        private readonly INotificationsService notificationsService;
        private readonly ObservableAsPropertyHelper<TimeSpan> recordDuration;
        private readonly CommandWrapper startRecording;
        private readonly CommandWrapper stopRecording;

        private bool atLeastOneRecordingTypeEnabled;
        private bool canAddItem;
        private bool enableKeyboardRecording = true;
        private bool enableMouseClicksRecording = true;
        private MousePositionRecordingType enableMousePositionRecording;

        private bool isBusy;
        private bool isRecording;
        private TimeSpan mousePositionRecordingResolution = TimeSpan.FromMilliseconds(250);

        private DateTimeOffset? recordStartTime;

        private IWindowHandle targetWindow;
        private HotkeyGesture toggleRecordingHotkey = new(Key.Escape);

        private TimeSpan totalDuration;

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
            IKeyboardEventsSource keyboardEventsSource)
        {
            this.mainWindowTracker = mainWindowTracker;
            this.appArguments = appArguments;
            this.clock = clock;
            Owner = owner;
            this.notificationsService = notificationsService;
            this.hotkeyFactory = hotkeyFactory;
            this.keyboardEventsSource = keyboardEventsSource;
            
            this.WhenAnyValue(x => x.RecordStartTime)
                .DistinctUntilChanged()
                .Select(x => x != null ? Observable.Interval(TimeSpan.FromMilliseconds(250)) : Observable.Return(0L))
                .Switch()
                .Select(x => DateTime.UtcNow - RecordStartTime ?? TimeSpan.Zero)
                .ToProperty(out recordDuration, this, x => x.RecordingDuration)
                .AddTo(Anchors);
            
            startRecording = CommandWrapper.Create(StartRecordingExecuted, this.WhenAnyValue(x => x.CanAddItem).ObserveOnDispatcher());
            stopRecording = CommandWrapper.Create(StopRecordingExecuted);
            
            Binder.Attach(this).AddTo(Anchors);
        }

        public bool AtLeastOneRecordingTypeEnabled
        {
            get => atLeastOneRecordingTypeEnabled;
            private set => RaiseAndSetIfChanged(ref atLeastOneRecordingTypeEnabled, value);
        }

        public bool CanAddItem
        {
            get => canAddItem;
            private set => RaiseAndSetIfChanged(ref canAddItem, value);
        }

        public TimeSpan RecordingDuration => recordDuration.Value;

        public DateTimeOffset? RecordStartTime
        {
            get => recordStartTime;
            private set => RaiseAndSetIfChanged(ref recordStartTime, value);
        }

        public ICommand StartRecording => startRecording;

        public TimeSpan TotalDuration
        {
            get => totalDuration;
            private set => RaiseAndSetIfChanged(ref totalDuration, value);
        }

        public IHotkeySequenceEditorViewModel Owner { get; }

        public ICommand StopRecording => stopRecording;

        public bool EnableMouseClicksRecording
        {
            get => enableMouseClicksRecording;
            set => RaiseAndSetIfChanged(ref enableMouseClicksRecording, value);
        }

        public MousePositionRecordingType MousePositionRecording
        {
            get => enableMousePositionRecording;
            set => RaiseAndSetIfChanged(ref enableMousePositionRecording, value);
        }

        public bool EnableKeyboardRecording
        {
            get => enableKeyboardRecording;
            set => RaiseAndSetIfChanged(ref enableKeyboardRecording, value);
        }

        public bool IsRecording
        {
            get => isRecording;
            private set => RaiseAndSetIfChanged(ref isRecording, value);
        }

        public TimeSpan MousePositionRecordingResolution
        {
            get => mousePositionRecordingResolution;
            set => RaiseAndSetIfChanged(ref mousePositionRecordingResolution, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            set => RaiseAndSetIfChanged(ref isBusy, value);
        }

        public HotkeyGesture ToggleRecordingHotkey
        {
            get => toggleRecordingHotkey;
            set => RaiseAndSetIfChanged(ref toggleRecordingHotkey, value);
        }

        public IWindowHandle TargetWindow
        {
            get => targetWindow;
            set => RaiseAndSetIfChanged(ref targetWindow, value);
        }

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
            var windowToRecord = targetWindow;
            using var recordingAnchors = new CompositeDisposable();
            Disposable.Create(StopRecordingExecuted).AddTo(recordingAnchors);
            
            var cancel = Observable.Merge(
                this.WhenAnyValue(x => x.IsRecording).Where(x => x == false).ToUnit()
            );

            var notification = new RecordingNotificationViewModel(this).AddTo(recordingAnchors);
            notificationsService.AddNotification(notification).AddTo(recordingAnchors);
            
            if (windowToRecord != null)
            {
                Log.Debug($"Activating window before recording: {windowToRecord}, previously active: {initialWindow}");
                UnsafeNative.ActivateWindow(windowToRecord.Handle);
            }

            var tracker = hotkeyFactory.Create().AddTo(recordingAnchors);
            tracker.HotkeyMode = HotkeyMode.Hold;
            tracker.SuppressKey = true;
            tracker.Hotkey = toggleRecordingHotkey;
            tracker.HandleApplicationKeys = true;
            await tracker.WhenAnyValue(x => x.IsActive).Where(x => x).ToUnit().Merge(cancel).Take(1);
            tracker.Reset();

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
                Log.Debug($"Restoring window after recording: {initialWindow}");
                UnsafeNative.ActivateWindow(initialWindow);
            }).AddTo(recordingAnchors);

            Observable.Merge(
                    windowToRecord == null ? Observable.Empty<string>() : mainWindowTracker.WhenAnyValue(x => x.ActiveWindowHandle).Where(x => x != windowToRecord.Handle).Select(x => $"Active window changed ! Expected {windowToRecord}, got: {x.ToHexadecimal()} ({UnsafeNative.GetWindowTitle(x)})"),
                    tracker.WhenAnyValue(x => x.IsActive).Where(x => x).Select(x => $"Hotkey {tracker} detected"),
                    this.WhenAnyValue(x => x.CanAddItem).Where(x => !x).Select(x => $"Cannot add more items: {totalDuration} / {Owner.MaxDuration}, {Owner.TotalCount} / {Owner.MaxItemsCount}"))
                .Take(1)
                .Subscribe(reason =>
                {
                    Log.Debug($"Stopping recording, reason: {reason}");
                    StopRecordingExecuted();
                })
                .AddTo(recordingAnchors);

            var sw = new Stopwatch();
            if (enableMousePositionRecording != MousePositionRecordingType.None && MousePositionRecordingResolution > TimeSpan.Zero)
            {
                keyboardEventsSource.WhenMouseMove
                    .Sample(MousePositionRecordingResolution)
                    .Select(x => (Point?)new Point(x.X, x.Y))
                    .StartWith(System.Windows.Forms.Cursor.Position)
                    .DistinctUntilChanged()
                    .ObserveOnDispatcher()
                    .TakeUntil(cancel)
                    .WithPrevious()
                    .Subscribe(x =>
                    {
                        if (x.Current == null)
                        {
                            return;
                        }
                        
                        var isRelative = enableMousePositionRecording == MousePositionRecordingType.Relative;
                        if (x.Previous == null && isRelative)
                        {
                            return;
                        }
                        
                        if (sw.ElapsedMilliseconds > 0)
                        {
                            Owner.AddItem.Execute(new HotkeySequenceDelay
                            {
                                Delay = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds),
                                IsKeypress = true,
                            });
                        }
                        sw.Restart();

                        var position = isRelative
                            ? new Point(x.Current.Value.X - x.Previous.Value.X, x.Current.Value.Y - x.Previous.Value.Y)
                            : windowToRecord == null 
                                ? x.Current
                                : new Point(x.Current.Value.X - windowToRecord.ClientBounds.X, x.Current.Value.Y - windowToRecord.ClientBounds.Y); 
                        Owner.AddItem.Execute(new HotkeySequenceHotkey
                        {
                            MousePosition = position,
                            IsRelative = isRelative
                        });
                    })
                    .AddTo(recordingAnchors);
            }

            if (EnableKeyboardRecording)
            {
                Observable.Merge(
                        keyboardEventsSource.WhenKeyDown.Select(x => new {x.KeyCode, IsDown = true}),
                        keyboardEventsSource.WhenKeyUp.Select(x => new {x.KeyCode, IsDown = false})
                    )
                    .DistinctUntilChanged()
                    .Where(x => !toggleRecordingHotkey.Contains(x.KeyCode))
                    .ObserveOnDispatcher()
                    .TakeUntil(cancel)
                    .Subscribe(x =>
                    {
                        if (sw.ElapsedMilliseconds > 0)
                        {
                            Owner.AddItem.Execute(new HotkeySequenceDelay
                            {
                                Delay = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds),
                                IsKeypress = true,
                            });
                        }
                        sw.Restart();
                        Owner.AddItem.Execute(new HotkeySequenceHotkey
                        {
                            Hotkey = new HotkeyGesture(x.KeyCode.ToInputKey()),
                            IsDown = x.IsDown,
                        });
                        sw.Restart();
                    })
                    .AddTo(recordingAnchors);
            }

            if (EnableMouseClicksRecording)
            {
                Observable.Merge(
                        keyboardEventsSource.WhenMouseDown.Select(x => new {x.Button, x.X, x.Y, IsDown = true}),
                        keyboardEventsSource.WhenMouseUp.Select(x => new {x.Button, x.X, x.Y, IsDown = false})
                    )
                    .DistinctUntilChanged()
                    .ObserveOnDispatcher()
                    .TakeUntil(cancel)
                    .Subscribe(x =>
                    {
                        var windowHandle = UnsafeNative.WindowFromPoint(new Point(x.X, x.Y));
                        var processId = UnsafeNative.GetProcessIdByWindowHandle(windowHandle);
                        if (processId == appArguments.ProcessId)
                        {
                            return;
                        }

                        if (sw.ElapsedMilliseconds > 0)
                        {
                            Owner.AddItem.Execute(new HotkeySequenceDelay
                            {
                                Delay = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds),
                                IsKeypress = true,
                            });
                        }
                        sw.Restart();
                        Owner.AddItem.Execute(new HotkeySequenceHotkey
                        {
                            Hotkey = new HotkeyGesture(x.Button),
                            IsDown = x.IsDown,
                        });
                        sw.Restart();
                    }).AddTo(recordingAnchors);
            }

            Log.Debug("Awaiting for recording to stop");
            await this.WhenAnyValue(x => x.IsRecording).Where(x => x == false).Take(1);
        }
    }
}