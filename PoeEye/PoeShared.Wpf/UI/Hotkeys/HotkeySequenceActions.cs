using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Interop;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Point = System.Drawing.Point;

namespace PoeShared.UI.Hotkeys
{
    internal sealed class HotkeySequenceActions : DisposableReactiveObject
    {
        private readonly HotkeySequenceEditor owner;
        private bool isRecording;
        private readonly ObservableAsPropertyHelper<ObservableCollection<HotkeySequenceItem>> itemsSource;
        private readonly ObservableAsPropertyHelper<TimeSpan> totalDuration;
        private readonly ObservableAsPropertyHelper<TimeSpan> maxDuration;
        private readonly ObservableAsPropertyHelper<int> maxItems;
        private readonly ObservableAsPropertyHelper<int> totalItemsCount;
        private readonly ObservableAsPropertyHelper<bool> canAddItem;
        private readonly ObservableAsPropertyHelper<bool> maxItemsExceeded;
        private readonly ObservableAsPropertyHelper<bool> maxDurationExceeded;
        private readonly ObservableAsPropertyHelper<TimeSpan> recordDuration;
        private readonly ObservableAsPropertyHelper<TimeSpan> totalItemsDuration;
        private readonly ObservableAsPropertyHelper<DateTimeOffset?> recordStartTime;
        private readonly ObservableAsPropertyHelper<bool> atLeastOneRecordingTypeEnabled;
        private readonly ObservableAsPropertyHelper<HotkeySequenceDelay> defaultItemDelay;

        public HotkeySequenceActions(
            HotkeySequenceEditor owner)
        {
            this.owner = owner;
            owner.Observe(HotkeySequenceEditor.ItemsSourceProperty)
                .Select(x => owner.ItemsSource)
                .OfType<ObservableCollection<HotkeySequenceItem>>()
                .ToProperty(out itemsSource, this, x => x.ItemsSource)
                .AddTo(Anchors);

            owner.Observe(HotkeySequenceEditor.MaxRecordingDurationProperty)
                .Select(x => owner.MaxRecordingDuration)
                .ToProperty(out maxDuration, this, x => x.MaxDuration)
                .AddTo(Anchors);

            owner.Observe(HotkeySequenceEditor.MaxItemsCountProperty)
                .Select(x => owner.MaxItemsCount)
                .ToProperty(out maxItems, this, x => x.MaxItemsCount)
                .AddTo(Anchors);

            owner.Observe(HotkeySequenceEditor.DefaultKeyPressDurationProperty)
                .Select(x => owner.DefaultKeyPressDuration)
                .Select(x => new HotkeySequenceDelay() {Delay = x, IsKeypress = false})
                .ToProperty(out defaultItemDelay, this, x => x.DefaultItemDelay)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.ItemsSource)
                .Select(x => x != null
                    ? SumEx.Sum(x.ToObservableChangeSet(), y => y is HotkeySequenceDelay delay ? delay.Delay.TotalMilliseconds : 0)
                    : Observable.Return(0d))
                .Switch()
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
            
            this.WhenAnyValue(x => x.ItemsSource)
                .Select(x => x != null
                    ? x.ToObservableChangeSet().CountIf()
                    : Observable.Return(0))
                .Switch()
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
                .Select(x =>  DateTime.UtcNow - RecordStartTime ?? TimeSpan.Zero)
                .ToProperty(out recordDuration, this, x => x.RecordingDuration)
                .AddTo(Anchors);

            Observable.CombineLatest(
                    owner.Observe(HotkeySequenceEditor.EnableKeyboardRecordingProperty).Select(_ => owner.EnableKeyboardRecording),
                    owner.Observe(HotkeySequenceEditor.EnableMouseClicksRecordingProperty).Select(_ => owner.EnableMouseClicksRecording),
                    owner.Observe(HotkeySequenceEditor.EnableMousePositionRecordingProperty).Select(_ => owner.EnableMousePositionRecording))
                .Select(x => x.Any(y => y))
                .ToProperty(out atLeastOneRecordingTypeEnabled, this, x => x.AtLeastOneRecordingTypeEnabled)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.MaxDurationExceeded, x => x.MaxItemsExceeded, x => x.AtLeastOneRecordingTypeEnabled)
                .Select(_ => !MaxDurationExceeded && !MaxItemsExceeded && AtLeastOneRecordingTypeEnabled)
                .ToProperty(out canAddItem, this, x => x.CanAddItem)
                .AddTo(Anchors);
            
            AddItem = CommandWrapper.Create<object>(AddItemExecuted);
            StartRecording = CommandWrapper.Create(AddRecordingExecuted, this.WhenAnyValue(x => x.CanAddItem).ObserveOnDispatcher());
            StopRecording = CommandWrapper.Create(StopRecordingExecuted);
            ClearItems = CommandWrapper.Create(() => ItemsSource.Clear(), this.WhenAnyValue(x => x.ItemsSource).Select(x => x != null).ObserveOnDispatcher());
        }

        private void AddItemExecuted(object arg)
        {
            var itemsToAdd = arg switch
            {
                HotkeySequenceItem item => new[] { item },
                Key key => new HotkeySequenceItem[]
                {
                    new HotkeySequenceHotkey() { Hotkey = new HotkeyGesture(key), IsDown = true},
                    new HotkeySequenceDelay() { Delay = owner.DefaultKeyPressDuration },
                    new HotkeySequenceHotkey() { Hotkey = new HotkeyGesture(key), IsDown = false}
                },
                _ => throw new ArgumentOutOfRangeException(nameof(arg), arg, "Unknown item type")
            };
            ItemsSource.AddRange(itemsToAdd);
        }

        public ICommand AddItem { get; }

        public HotkeySequenceDelay DefaultItemDelay => defaultItemDelay.Value;
        
        public HotkeySequenceText DefaultItemText { get; } = new() { Text = "text" };

        public bool AtLeastOneRecordingTypeEnabled => atLeastOneRecordingTypeEnabled.Value;

        public bool MaxItemsExceeded => maxItemsExceeded.Value;

        public bool MaxDurationExceeded => maxDurationExceeded.Value;

        public bool CanAddItem => canAddItem.Value;

        public int TotalItemsCount => totalItemsCount.Value;

        public int MaxItemsCount => maxItems.Value;

        public TimeSpan MaxDuration => maxDuration.Value;

        public TimeSpan TotalDuration => totalDuration.Value;

        public TimeSpan TotalItemsDuration => totalItemsDuration.Value;

        public TimeSpan RecordingDuration => recordDuration.Value;

        public DateTimeOffset? RecordStartTime => recordStartTime.Value;

        public ObservableCollection<HotkeySequenceItem> ItemsSource => itemsSource.Value;
        
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
            IsRecording = false;
        }

        private async Task AddRecordingExecuted()
        {
            if (owner.KeyboardEventsSource == null)
            {
                throw new InvalidOperationException("Keyboard event source is not initialized");
            }

            if (IsRecording)
            {
                return;
            }

            IsRecording = true;
            var cancel = Observable.Merge(
                this.WhenAnyValue(x => x.IsRecording).Where(x => x == false).ToUnit()
            );

            Observable.Merge(
                    owner.KeyboardEventsSource.WhenKeyDown.Where(x => x.KeyCode == Keys.Escape).ToUnit(),
                    this.WhenAnyValue(x => x.CanAddItem).Where(x => !x).ToUnit())
                .Take(1)
                .Subscribe(StopRecordingExecuted);

            var sw = Stopwatch.StartNew();

            if (owner.EnableMousePositionRecording)
            {
                owner.KeyboardEventsSource.WhenMouseMove
                    .Sample(owner.MousePositionRecordingResolution)
                    .Select(x => new Point(x.X, x.Y))
                    .DistinctUntilChanged()
                    .ObserveOnDispatcher()
                    .TakeUntil(cancel)
                    .Subscribe(x =>
                    {
                        ItemsSource.Add( new HotkeySequenceDelay
                        {
                            Delay = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds),
                            IsKeypress = true,
                        });
                        sw.Restart();
                        ItemsSource.Add(new HotkeySequenceHotkey
                        {
                            MousePosition = x
                        });
                    });
            }

            if (owner.EnableKeyboardRecording)
            {
                Observable.Merge(
                        owner.KeyboardEventsSource.WhenKeyDown.Select(x => new { x.KeyCode, IsDown = true }),
                        owner.KeyboardEventsSource.WhenKeyUp.Select(x => new { x.KeyCode, IsDown = false })
                    )
                    .DistinctUntilChanged()
                    .ObserveOnDispatcher()
                    .TakeUntil(cancel)
                    .Subscribe(x =>
                    {
                        ItemsSource.Add( new HotkeySequenceDelay
                        {
                            Delay = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds),
                            IsKeypress = true,
                        });
                        sw.Restart();
                        ItemsSource.Add(new HotkeySequenceHotkey
                        {
                            Hotkey = new HotkeyGesture(x.KeyCode.ToInputKey()),
                            IsDown = x.IsDown,
                        });
                        sw.Restart();
                    });
            }

            var parentWindow = this.owner.FindVisualAncestor<Window>();
            if (parentWindow == null)
            {
                throw new InvalidOperationException($"Failed to find parent window of control {owner}");
            }
            var ownerWindowHandle = new WindowInteropHelper(parentWindow).EnsureHandle();
            var defaultDuration = owner.DefaultKeyPressDuration;

            if (owner.EnableMouseClicksRecording)
            {
                Observable.Merge(
                        owner.KeyboardEventsSource.WhenMouseDown.Select(x => new { x.Button, x.X, x.Y, IsDown = true }),
                        owner.KeyboardEventsSource.WhenMouseUp.Select(x => new { x.Button,  x.X, x.Y, IsDown = false })
                    )
                    .DistinctUntilChanged()
                    .ObserveOnDispatcher()
                    .TakeUntil(cancel)
                    .Subscribe(x =>
                    {
                        if (UnsafeNative.GetForegroundWindow() == ownerWindowHandle)
                        {
                            var windowCoords = owner.PointFromScreen(new System.Windows.Point(x.X, x.Y));
                            var hitElement = owner.InputHitTest(windowCoords);
                            if (hitElement is UIElement uiElement)
                            {
                                var editor = uiElement.FindVisualAncestor<HotkeySequenceEditor>();
                                if (editor != null)
                                {
                                    // clicked inside SequenceEditor control, ignoring
                                    return;
                                }
                            }
                        }
                        
                        ItemsSource.Add(new HotkeySequenceDelay
                        {
                            Delay = defaultDuration,
                            IsKeypress = true,
                        });
                        sw.Restart();
                        ItemsSource.Add(new HotkeySequenceHotkey
                        {
                            Hotkey = new HotkeyGesture(x.Button),
                            IsDown = x.IsDown,
                        });
                        sw.Restart();
                    });
            }
        }
    }
}