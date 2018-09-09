using System;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DynamicData;
using Guards;
using JetBrains.Annotations;
using Unity; using Unity.Resolution; using Unity.Attributes;
using PoeEye.StashGrid.Models;
using PoeEye.StashGrid.Modularity;
using PoeEye.StashGrid.Services;
using PoeShared;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;
using Prism.Commands;
using ReactiveUI;

namespace PoeEye.StashGrid.ViewModels
{
    internal sealed class PoeStashGridViewModel : OverlayViewModelBase, IPoeStashHighlightService,
        IPoeStashGridViewModel
    {
        private const int MaxInventoryWidth = 24;
        private const int MaxInventoryHeight = 24;

        private readonly IConfigProvider<PoeStashGridConfig> configProvider;

        private readonly ISourceList<BasicStashGridCellViewModel> highlights =
            new SourceList<BasicStashGridCellViewModel>();

        private readonly ReadOnlyObservableCollection<BasicStashGridCellViewModel> highlightsProxy;

        public PoeStashGridViewModel(
            [NotNull] IKeyboardEventsSource keyboardMouseEvents,
            [NotNull] IOverlayWindowController controller,
            [NotNull] IConfigProvider<PoeStashGridConfig> configProvider,
            [NotNull] [Dependency(WellKnownSchedulers.Background)]
            IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)]
            IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(keyboardMouseEvents, nameof(keyboardMouseEvents));
            Guard.ArgumentNotNull(controller, nameof(controller));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            this.configProvider = configProvider;
            MinSize = new Size(300, 300);
            MaxSize = new Size(1000, 1000);
            Top = 90;
            Left = 15;
            Width = 640;
            Height = 705;
            SizeToContent = SizeToContent.Manual;
            IsUnlockable = true;
            Title = "Stash Grid";

            this.WhenAnyValue(x => x.IsLocked)
                .Subscribe(isLocked => OverlayMode = isLocked ? OverlayMode.Transparent : OverlayMode.Layered)
                .AddTo(Anchors);

            highlights
                .Connect()
                .ObserveOn(uiScheduler)
                .Bind(out highlightsProxy)
                .Subscribe()
                .AddTo(Anchors);

            WhenLoaded.Subscribe(
                    () =>
                    {
                        configProvider
                            .WhenChanged
                            .Subscribe(ApplyConfig)
                            .AddTo(Anchors);
                    })
                .AddTo(Anchors);
        }

        public ReactiveList<BasicStashGridCellViewModel> GridCells { get; } =
            new ReactiveList<BasicStashGridCellViewModel>();

        public ReadOnlyObservableCollection<BasicStashGridCellViewModel> Highlights => highlightsProxy;

        public IGridCellViewController AddHighlight(ItemPosition pos, StashTabType stashType)
        {
            Log.Instance.Debug($"Highlighting zone {pos} in tab of type {stashType}");
            pos = TransformToAbsolute(pos, stashType);

            Log.Instance.Debug($"Absolute position: {pos}");
            var cell = new HighlightedStashGridCellViewModel
            {
                Width = pos.Width,
                Height = pos.Height,
                Left = pos.X,
                Top = pos.Y
            };

            highlights.Add(cell);

            var result = new GridCellController(cell);
            Disposable.Create(() => highlights.Remove(cell)).AddTo(result.Anchors);

            return result;
        }

        private void ApplyConfig(PoeStashGridConfig config)
        {
            base.ApplyConfig(config);
            PrepareGridCells();
        }

        protected override void LockWindowCommandExecuted()
        {
            base.LockWindowCommandExecuted();
            var config = configProvider.ActualConfig;
            base.SavePropertiesToConfig(config);

            const float offsetY = 33; // FIXME Should be calculated instead of being hardcoded
            config.StashBounds = new Rect(Left, Top + offsetY, Width, Height - offsetY);
            configProvider.Save(config);
        }

        private void PrepareGridCells()
        {
            GridCells.Clear();

            for (var x = 0; x < MaxInventoryWidth; x += 1)
            {
                for (var y = 0; y < MaxInventoryHeight; y += 1)
                {
                    GridCells.Add(
                        new BasicStashGridCellViewModel
                        {
                            Left = x,
                            Top = y,
                            Width = 1,
                            Height = 1
                        });
                }
            }
        }

        private ItemPosition TransformToAbsolute(ItemPosition pos, StashTabType stashType)
        {
            switch (stashType)
            {
                case StashTabType.QuadStash:
                    return pos;

                default:
                    return new ItemPosition(
                        pos.X * 2,
                        pos.Y * 2,
                        pos.Width * 2,
                        pos.Height * 2);
            }
        }

        private sealed class GridCellController : DisposableReactiveObject, IGridCellViewController
        {
            private readonly HighlightedStashGridCellViewModel cell;

            public GridCellController(HighlightedStashGridCellViewModel cell)
            {
                this.cell = cell;

                this.BindPropertyTo(x => x.IsFresh, cell, x => x.IsFresh).AddTo(Anchors);
                this.BindPropertyTo(x => x.BorderColor, cell, x => x.BorderColor).AddTo(Anchors);
            }

            public bool IsFresh
            {
                get => cell.IsFresh;
                set => cell.IsFresh = value;
            }

            public Color? BorderColor
            {
                get => cell.BorderColor;
                set => cell.BorderColor = value;
            }

            public string ToolTipText
            {
                get => cell.ToolTipText;
                set => cell.ToolTipText = value;
            }
        }
    }
}