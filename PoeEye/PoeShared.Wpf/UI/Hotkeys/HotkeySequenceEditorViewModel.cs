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
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.RegionSelector.Services;
using PoeShared.Scaffolding.WPF;
using PropertyBinder;
using ReactiveUI;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace PoeShared.UI
{
    internal sealed class HotkeySequenceEditorViewModel : DisposableReactiveObject, IHotkeySequenceEditorViewModel
    {
        private static readonly Binder<HotkeySequenceEditorViewModel> Binder = new();

        private static readonly IFluentLog Log = typeof(HotkeySequenceEditorViewModel).PrepareLogger();
        private readonly IScreenRegionSelectorService screenRegionSelectorService;

        private readonly ObservableAsPropertyHelper<TimeSpan> totalDuration;
        private readonly ObservableAsPropertyHelper<int> totalItemsCount;
        private TimeSpan defaultKeyPressDuration = TimeSpan.FromMilliseconds(50);
        private TimeSpan maxDuration = TimeSpan.FromSeconds(10);
        private int maxItemsCount = 250;

        static HotkeySequenceEditorViewModel()
        {
            Binder.Bind(x => new HotkeySequenceDelay() {Delay = x.DefaultKeyPressDuration, IsKeypress = false}).To(x => x.DefaultItemDelay);
            Binder.Bind(x => Math.Round(x.MaxDuration.TotalSeconds - x.TotalDuration.TotalSeconds) <= 0.5).To(x => x.MaxDurationExceeded);
            Binder.Bind(x => x.TotalCount >= x.MaxItemsCount).To(x => x.MaxItemsExceeded);
        }

        public HotkeySequenceEditorViewModel(
            IFactory<IHotkeySequenceEditorController, IHotkeySequenceEditorViewModel> controllerFactory,
            IScreenRegionSelectorService screenRegionSelectorService)
        {
            this.screenRegionSelectorService = screenRegionSelectorService;
            var items = new ObservableCollectionExtended<HotkeySequenceItem>();
            Items = items;

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

        public HotkeySequenceDelay DefaultItemDelay { get; private set; }

        public HotkeySequenceText DefaultItemText { get; } = new() {Text = "text"};

        public UIElement Owner { get; set; }

        public TimeSpan TotalDuration => totalDuration.Value;

        public ObservableCollection<HotkeySequenceItem> Items { get; }

        public ICommand AddItem { get; }

        public ICommand RemoveItem { get; }

        public bool MaxItemsExceeded { get; private set; }

        public bool MaxDurationExceeded { get; private set; }

        public int TotalCount => totalItemsCount.Value;

        public bool HideKeypressDelays { get; set; }

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

            var windowToRecord = Controller.TargetWindow;
            var initialWindow = UnsafeNative.GetForegroundWindow();
            if (windowToRecord != null)
            {
                UnsafeNative.ActivateWindow(windowToRecord.Handle);
                Disposable.Create(() => UnsafeNative.ActivateWindow(initialWindow)).AddTo(windowAnchors);
            }

            var result = await screenRegionSelectorService.SelectRegion(Size.Empty);
            if (result.IsValid)
            {
                var selectedLocation = result.AbsoluteSelection.Location;
                if (windowToRecord != null && !windowToRecord.ClientBounds.Contains(selectedLocation))
                {
                    throw new InvalidOperationException($"Click must be inside window {windowToRecord.Title}, bounds: {windowToRecord.ClientBounds}, click location: {selectedLocation}");
                }
                var position = windowToRecord == null
                    ? selectedLocation
                    : new Point(selectedLocation.X - windowToRecord.ClientBounds.X, selectedLocation.Y - windowToRecord.ClientBounds.Y);
                var newItem = new HotkeySequenceHotkey
                {
                    MousePosition = position
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