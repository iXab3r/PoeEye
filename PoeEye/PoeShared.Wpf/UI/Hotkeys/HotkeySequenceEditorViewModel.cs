using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using PoeShared.Prism;
using PoeShared.RegionSelector.Views;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Scaffolding.WPF;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.UI
{
    internal sealed class HotkeySequenceEditorViewModel : DisposableReactiveObject, IHotkeySequenceEditorViewModel
    {
        private static readonly Binder<HotkeySequenceEditorViewModel> Binder = new();

        private static readonly IFluentLog Log = typeof(HotkeySequenceEditorViewModel).PrepareLogger();

        private readonly IFactory<RegionSelectorWindow> regionSelectorWindowFactory;
        private readonly ObservableAsPropertyHelper<int> totalItemsCount;
        private readonly ObservableAsPropertyHelper<TimeSpan> totalDuration;

        private HotkeySequenceDelay defaultItemDelay;
        private TimeSpan defaultKeyPressDuration = TimeSpan.FromMilliseconds(50);

        private bool hideKeypressDelays;
        private TimeSpan maxDuration = TimeSpan.FromSeconds(10);
        private bool maxDurationExceeded;
        private int maxItemsCount = 250;
        private bool maxItemsExceeded;
        private UIElement owner;

        static HotkeySequenceEditorViewModel()
        {
            Binder.Bind(x => new HotkeySequenceDelay() {Delay = x.DefaultKeyPressDuration, IsKeypress = false}).To(x => x.DefaultItemDelay);
            Binder.Bind(x => Math.Round(x.MaxDuration.TotalSeconds - x.TotalDuration.TotalSeconds) <= 0.5).To(x => x.MaxDurationExceeded);
            Binder.Bind(x => x.TotalCount >= x.MaxItemsCount).To(x => x.MaxItemsExceeded);
        }

        public HotkeySequenceEditorViewModel(
            IFactory<IHotkeySequenceEditorController, IHotkeySequenceEditorViewModel> controllerFactory,
            IFactory<RegionSelectorWindow> regionSelectorWindowFactory)
        {
            var items = new ObservableCollectionExtended<HotkeySequenceItem>();
            Items = items;

            this.regionSelectorWindowFactory = regionSelectorWindowFactory;

            SumEx.Sum(items.ToObservableChangeSet(), y => y is HotkeySequenceDelay delay ? delay.Delay.TotalMilliseconds : 0)
                .Select(TimeSpan.FromMilliseconds)
                .ToProperty(out totalDuration, this, x => x.TotalDuration)
                .AddTo(Anchors);

            items.ToObservableChangeSet()
                .CountIf()
                .ToProperty(out totalItemsCount, this, x => x.TotalCount)
                .AddTo(Anchors);

            AddItem = CommandWrapper.Create<object>(AddItemExecuted);
            RemoveItem = CommandWrapper.Create<object>(RemoveItemExecuted);
            ClearItems = CommandWrapper.Create(() => items.Clear());
            MouseMoveCommand = CommandWrapper.Create(HandleMouseMoveExecuted);
            
            Controller = controllerFactory.Create(this).AddTo(Anchors);
            Binder.Attach(this).AddTo(Anchors);
        }

        public ICommand MouseMoveCommand { get; }

        public HotkeySequenceDelay DefaultItemDelay
        {
            get => defaultItemDelay;
            private set => RaiseAndSetIfChanged(ref defaultItemDelay, value);
        }

        public HotkeySequenceText DefaultItemText { get; } = new() {Text = "text"};

        public UIElement Owner
        {
            get => owner;
            set => RaiseAndSetIfChanged(ref owner, value);
        }

        public TimeSpan TotalDuration => totalDuration.Value;

        public ObservableCollection<HotkeySequenceItem> Items { get; }

        public ICommand AddItem { get; }

        public ICommand RemoveItem { get; }

        public bool MaxItemsExceeded
        {
            get => maxItemsExceeded;
            private set => RaiseAndSetIfChanged(ref maxItemsExceeded, value);
        }

        public bool MaxDurationExceeded
        {
            get => maxDurationExceeded;
            private set => RaiseAndSetIfChanged(ref maxDurationExceeded, value);
        }

        public int TotalCount => totalItemsCount.Value;

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

        public TimeSpan MaxDuration
        {
            get => maxDuration;
            set => RaiseAndSetIfChanged(ref maxDuration, value);
        }

        public ICommand ClearItems { get; }

        public IHotkeySequenceEditorController Controller { get; }

        private async Task HandleMouseMoveExecuted()
        {
            using var windowAnchors = new CompositeDisposable();

            var window = regionSelectorWindowFactory.Create().AddTo(windowAnchors);
            Disposable.Create(() => Log.Debug("Disposed selector window: {window}")).AddTo(windowAnchors);
            Log.Debug($"Showing new selector window: {window}");
            window.Show();
            window.SelectScreenCoordinates();
            Log.Debug($"Awaiting for selection result from {window}");
            var result = await Observable.FromEventPattern<EventHandler, EventArgs>(h => window.Closed += h, h => window.Closed -= h)
                .Select(x => window.Result)
                .Take(1);
            if (result.IsValid)
            {
                var newItem = new HotkeySequenceHotkey
                {
                    MousePosition = result.AbsoluteSelection.Location
                };
                AddItemExecuted(newItem);
            }
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
    }
}