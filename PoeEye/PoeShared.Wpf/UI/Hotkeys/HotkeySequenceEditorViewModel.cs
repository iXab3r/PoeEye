using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using log4net;
using PInvoke;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Unity;
using IDropTarget = GongSolutions.Wpf.DragDrop.IDropTarget;
using Point = System.Drawing.Point;

namespace PoeShared.UI.Hotkeys
{
    internal sealed class HotkeySequenceEditorViewModel : DisposableReactiveObject, IHotkeySequenceEditorViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HotkeySequenceEditorViewModel));

        private readonly IAppArguments appArguments;
        private readonly NotificationsService notificationsService;
        private readonly IFactory<IHotkeyTracker> hotkeyFactory;
        private readonly IKeyboardEventsSource keyboardEventsSource;
        private readonly ObservableAsPropertyHelper<int> totalItemsCount;
        private readonly ObservableAsPropertyHelper<bool> canAddItem;
        private readonly ObservableAsPropertyHelper<bool> maxItemsExceeded;
        private readonly ObservableAsPropertyHelper<bool> maxDurationExceeded;
        private readonly ObservableAsPropertyHelper<TimeSpan> totalDuration;
        private readonly ObservableAsPropertyHelper<TimeSpan> recordDuration;
        private readonly ObservableAsPropertyHelper<TimeSpan> totalItemsDuration;
        private readonly ObservableAsPropertyHelper<DateTimeOffset?> recordStartTime;
        private readonly ObservableAsPropertyHelper<bool> atLeastOneRecordingTypeEnabled;
        private readonly ObservableAsPropertyHelper<HotkeySequenceDelay> defaultItemDelay;
        private readonly SerialDisposable recordingAnchors;

        private bool enableMouseClicksRecording = true;
        private bool enableMousePositionRecording;
        private bool enableKeyboardRecording = true;
        private bool hideKeypressDelays;
        private TimeSpan defaultKeyPressDuration = TimeSpan.FromMilliseconds(50);
        private TimeSpan mousePositionRecordingResolution = TimeSpan.FromMilliseconds(250);
        private TimeSpan maxDuration = TimeSpan.FromSeconds(10);
        private bool isRecording;
        private int maxItemsCount = 250;
        private UIElement owner;
        private HotkeyGesture stopRecordingHotkey = new(Key.Escape);
        
        public HotkeySequenceEditorViewModel(
            IAppArguments appArguments,
            NotificationsService notificationsService,
            IFactory<IHotkeyTracker> hotkeyFactory,
            IKeyboardEventsSource keyboardEventsSource)
        {
            recordingAnchors = new SerialDisposable().AddTo(Anchors);
            var items = new ObservableCollectionExtended<HotkeySequenceItem>();
            Items = items;

            this.appArguments = appArguments;
            this.notificationsService = notificationsService;
            this.hotkeyFactory = hotkeyFactory;
            this.keyboardEventsSource = keyboardEventsSource;

            this.WhenAnyValue(x => x.DefaultKeyPressDuration)
                .Select(x => new HotkeySequenceDelay() {Delay = x, IsKeypress = false})
                .ToProperty(out defaultItemDelay, this, x => x.DefaultItemDelay)
                .AddTo(Anchors);

            SumEx.Sum(items.ToObservableChangeSet(), y => y is HotkeySequenceDelay delay ? delay.Delay.TotalMilliseconds : 0)
                .Select(TimeSpan.FromMilliseconds)
                .ToProperty(out totalItemsDuration, this, x => x.TotalItemsDuration)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsRecording)
                .Select(x =>
                {
                    var initialDuration = TotalItemsDuration;
                    return x ? this.WhenAnyValue(y => y.RecordingDuration).Select(y => y + initialDuration) : this.WhenAnyValue(y => y.TotalItemsDuration);
                })
                .Switch()
                .ToProperty(out totalDuration, this, x => x.TotalDuration)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.TotalDuration, x => x.MaxDuration)
                .Select(x => Math.Round(MaxDuration.TotalSeconds - TotalDuration.TotalSeconds) <= 0.5)
                .ToProperty(out maxDurationExceeded, this, x => x.MaxDurationExceeded)
                .AddTo(Anchors);

            items.ToObservableChangeSet()
                .CountIf()
                .ToProperty(out totalItemsCount, this, x => x.TotalItemsCount)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.TotalItemsCount, x => x.MaxItemsCount)
                .Select(x => TotalItemsCount >= MaxItemsCount)
                .ToProperty(out maxItemsExceeded, this, x => x.MaxItemsExceeded)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsRecording)
                .DistinctUntilChanged()
                .Select(x => x ? DateTime.UtcNow : default(DateTimeOffset?))
                .ToProperty(out recordStartTime, this, x => x.RecordStartTime)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.RecordStartTime)
                .DistinctUntilChanged()
                .Select(x => x != null ? Observable.Interval(TimeSpan.FromMilliseconds(250)) : Observable.Return(0L))
                .Switch()
                .Select(x => DateTime.UtcNow - RecordStartTime ?? TimeSpan.Zero)
                .ToProperty(out recordDuration, this, x => x.RecordingDuration)
                .AddTo(Anchors);

            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.EnableKeyboardRecording),
                    this.WhenAnyValue(x => x.EnableMouseClicksRecording),
                    this.WhenAnyValue(x => x.EnableMousePositionRecording))
                .Select(x => x.Any(y => y))
                .ToProperty(out atLeastOneRecordingTypeEnabled, this, x => x.AtLeastOneRecordingTypeEnabled)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.MaxDurationExceeded, x => x.MaxItemsExceeded, x => x.AtLeastOneRecordingTypeEnabled)
                .Select(_ => !MaxDurationExceeded && !MaxItemsExceeded && AtLeastOneRecordingTypeEnabled)
                .ToProperty(out canAddItem, this, x => x.CanAddItem)
                .AddTo(Anchors);

            AddItem = CommandWrapper.Create<object>(AddItemExecuted);
            RemoveItem = CommandWrapper.Create<object>(RemoveItemExecuted);
            StartRecording = CommandWrapper.Create(StartRecordingExecuted, this.WhenAnyValue(x => x.CanAddItem).ObserveOnDispatcher());
            StopRecording = CommandWrapper.Create(StopRecordingExecuted);
            ClearItems = CommandWrapper.Create(() => items.Clear());
        }
        
        private void RemoveItemExecuted(object arg)
        {
            var itemsToRemove = arg switch
            {
                HotkeySequenceItem item => new[] {item},
                _ => throw new ArgumentOutOfRangeException(nameof(arg), arg, "Unknown item type")
            };
            Items.RemoveMany(itemsToRemove);
        }

        private void AddItemExecuted(object arg)
        {
            var itemsToAdd = arg switch
            {
                HotkeySequenceItem item => new[] {item},
                Key key => new HotkeySequenceItem[]
                {
                    new HotkeySequenceHotkey() {Hotkey = new HotkeyGesture(key), IsDown = true},
                    new HotkeySequenceDelay() {Delay = this.DefaultKeyPressDuration},
                    new HotkeySequenceHotkey() {Hotkey = new HotkeyGesture(key), IsDown = false}
                },
                
                _ => throw new ArgumentOutOfRangeException(nameof(arg), arg, "Unknown item type")
            };
            Items.AddRange(itemsToAdd);
        }

        public HotkeyGesture StopRecordingHotkey
        {
            get => stopRecordingHotkey;
            set => RaiseAndSetIfChanged(ref stopRecordingHotkey, value);
        }
        
        public ObservableCollection<HotkeySequenceItem> Items { get; }

        public ICommand AddItem { get; }

        public ICommand RemoveItem { get; }

        public HotkeySequenceDelay DefaultItemDelay => defaultItemDelay.Value;

        public HotkeySequenceText DefaultItemText { get; } = new() {Text = "text"};

        public bool AtLeastOneRecordingTypeEnabled => atLeastOneRecordingTypeEnabled.Value;

        public bool MaxItemsExceeded => maxItemsExceeded.Value;

        public bool MaxDurationExceeded => maxDurationExceeded.Value;

        public bool CanAddItem => canAddItem.Value;

        public int TotalItemsCount => totalItemsCount.Value;

        public bool EnableMouseClicksRecording
        {
            get => enableMouseClicksRecording;
            set => RaiseAndSetIfChanged(ref enableMouseClicksRecording, value);
        }

        public bool EnableMousePositionRecording
        {
            get => enableMousePositionRecording;
            set => RaiseAndSetIfChanged(ref enableMousePositionRecording, value);
        }

        public bool EnableKeyboardRecording
        {
            get => enableKeyboardRecording;
            set => RaiseAndSetIfChanged(ref enableKeyboardRecording, value);
        }

        public bool HideKeypressDelays
        {
            get => hideKeypressDelays;
            set => RaiseAndSetIfChanged(ref hideKeypressDelays, value);
        }

        public int MaxItemsCount
        {
            get => maxItemsCount;
            set => RaiseAndSetIfChanged(ref maxItemsCount, value);
        }

        public TimeSpan DefaultKeyPressDuration
        {
            get => defaultKeyPressDuration;
            set => RaiseAndSetIfChanged(ref defaultKeyPressDuration, value);
        }

        public TimeSpan MousePositionRecordingResolution
        {
            get => mousePositionRecordingResolution;
            set => RaiseAndSetIfChanged(ref mousePositionRecordingResolution, value);
        }

        public TimeSpan MaxDuration
        {
            get => maxDuration;
            set => RaiseAndSetIfChanged(ref maxDuration, value);
        }

        public UIElement Owner
        {
            get => owner;
            set => RaiseAndSetIfChanged(ref owner, value);
        }

        public TimeSpan TotalDuration => totalDuration.Value;

        public TimeSpan TotalItemsDuration => totalItemsDuration.Value;

        public TimeSpan RecordingDuration => recordDuration.Value;

        public DateTimeOffset? RecordStartTime => recordStartTime.Value;

        public bool IsRecording
        {
            get => isRecording;
            private set => RaiseAndSetIfChanged(ref isRecording, value);
        }

        public ICommand StartRecording { get; }

        public ICommand StopRecording { get; }

        public ICommand ClearItems { get; }

        private void StopRecordingExecuted()
        {
            if (!IsRecording)
            {
                throw new InvalidOperationException("Not recording");
            }
            
            Log.Debug($"Stopping recording, isRecording: {isRecording}");
            recordingAnchors.Disposable = null;
        }

        private async Task StartRecordingExecuted()
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("Already recording");
            }
            IsRecording = true;
            
            var anchors = new CompositeDisposable().AssignTo(recordingAnchors);
            Disposable.Create(() => IsRecording = false).AddTo(anchors);
            var cancel = Observable.Merge(
                this.WhenAnyValue(x => x.IsRecording).Where(x => x == false).ToUnit()
            );

            var notification = new RecordingNotificationViewModel(this).AddTo(anchors);
            notificationsService.AddNotification(notification).AddTo(anchors);

            var tracker = hotkeyFactory.Create().AddTo(anchors);
            tracker.HotkeyMode = HotkeyMode.Hold;
            tracker.SuppressKey = true;
            tracker.Hotkey = stopRecordingHotkey;
            tracker.HandleApplicationKeys = true;

            Observable.Merge(
                    tracker.WhenAnyValue(x => x.IsActive).Where(x => x).Select(x => $"Hotkey {tracker} detected"),
                    this.WhenAnyValue(x => x.CanAddItem).Where(x => !x).Select(x => $"Cannot add more items: {totalDuration.Value} / {maxDuration}, {totalItemsCount.Value} / {maxItemsCount}"))
                .Take(1)
                .Subscribe(reason =>
                {
                    Log.Debug($"Stopping recording, reason: {reason}");
                    StopRecordingExecuted();
                })
                .AddTo(anchors);

            var sw = Stopwatch.StartNew();
            if (EnableMousePositionRecording && MousePositionRecordingResolution > TimeSpan.Zero)
            {
                keyboardEventsSource.WhenMouseMove
                    .Sample(MousePositionRecordingResolution)
                    .Select(x => new Point(x.X, x.Y))
                    .DistinctUntilChanged()
                    .ObserveOnDispatcher()
                    .TakeUntil(cancel)
                    .Subscribe(x =>
                    {
                        Items.Add(new HotkeySequenceDelay
                        {
                            Delay = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds),
                            IsKeypress = true,
                        });
                        sw.Restart();
                        Items.Add(new HotkeySequenceHotkey
                        {
                            MousePosition = x
                        });
                    })
                    .AddTo(anchors);
            }

            if (EnableKeyboardRecording)
            {
                Observable.Merge(
                        keyboardEventsSource.WhenKeyDown.Select(x => new {x.KeyCode, IsDown = true}),
                        keyboardEventsSource.WhenKeyUp.Select(x => new {x.KeyCode, IsDown = false})
                    )
                    .DistinctUntilChanged()
                    .ObserveOnDispatcher()
                    .TakeUntil(cancel)
                    .Subscribe(x =>
                    {
                        Items.Add(new HotkeySequenceDelay
                        {
                            Delay = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds),
                            IsKeypress = true,
                        });
                        sw.Restart();
                        Items.Add(new HotkeySequenceHotkey
                        {
                            Hotkey = new HotkeyGesture(x.KeyCode.ToInputKey()),
                            IsDown = x.IsDown,
                        });
                        sw.Restart();
                    })
                    .AddTo(anchors);
            }


            var defaultDuration = DefaultKeyPressDuration;

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

                        Items.Add(new HotkeySequenceDelay
                        {
                            Delay = defaultDuration,
                            IsKeypress = true,
                        });
                        sw.Restart();
                        Items.Add(new HotkeySequenceHotkey
                        {
                            Hotkey = new HotkeyGesture(x.Button),
                            IsDown = x.IsDown,
                        });
                        sw.Restart();
                    }).AddTo(anchors);
            }
        }
    }
}