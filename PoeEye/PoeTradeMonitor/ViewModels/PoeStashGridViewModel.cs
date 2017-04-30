using System.Reactive.Concurrency;
using System.Windows;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.TradeMonitor.Modularity;
using PoeEye.TradeMonitor.Services;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Anotar.Log4Net;
using DynamicData;
using PoeEye.TradeMonitor.Models;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;
using Prism.Commands;

namespace PoeEye.TradeMonitor.ViewModels
{
    internal sealed class PoeStashGridViewModel : OverlayViewModelBase, IPoeStashHighlightService
    {
        [NotNull] private readonly IConfigProvider<PoeStashGridConfig> configProvider;
        private readonly ISourceList<BasicStashGridCellViewModel> highlights = new SourceList<BasicStashGridCellViewModel>();

        private readonly ReadOnlyObservableCollection<BasicStashGridCellViewModel> highlightsProxy;

        public PoeStashGridViewModel([NotNull] IKeyboardEventsSource keyboardMouseEvents,
            [NotNull] IPoeStashService stashService,
            [NotNull] IConfigProvider<PoeStashGridConfig> configProvider,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.configProvider = configProvider;
            MinSize = new Size(300, 300);
            MaxSize = new Size(1000, 1000);
            Top = 100;
            Left = 100;
            SizeToContent = SizeToContent.Manual;

            LockWindowCommand = new DelegateCommand(LockWindowCommandExecuted);

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
                }).AddTo(Anchors);
        }

        private float opacity;

        public float Opacity
        {
            get { return opacity; }
            set { this.RaiseAndSetIfChanged(ref opacity, value); }
        }

        public ReactiveList<BasicStashGridCellViewModel> GridCells { get; } = new ReactiveList<BasicStashGridCellViewModel>();

        public ReadOnlyObservableCollection<BasicStashGridCellViewModel> Highlights => highlightsProxy;

        public ICommand LockWindowCommand { get; }

        private void ApplyConfig(PoeStashGridConfig config)
        {
            if (config.OverlayOpacity <= 0.01)
            {
                IsLocked = false;
                config.OverlayOpacity = 1;
            }
            if (config.OverlaySize.Height <= 0 || config.OverlaySize.Width <= 0)
            {
                IsLocked = false;
                config.OverlaySize = MinSize;
            }
            Width = config.OverlaySize.Width;
            Height = config.OverlaySize.Height;

            if (config.OverlayLocation.X <= 1 && config.OverlayLocation.Y <= 1)
            {
                IsLocked = false;
                config.OverlayLocation = new Point(Width / 2, Height / 2);
            }
            Left = config.OverlayLocation.X;
            Top = config.OverlayLocation.Y;

            Opacity = config.OverlayOpacity;
            PrepareGridCells();
        }

        private void LockWindowCommandExecuted()
        {
            var config = configProvider.ActualConfig;
            config.OverlayLocation = new Point(Left, Top);
            config.OverlaySize = new Size(Width, Height);
            config.OverlayOpacity = Opacity;
            configProvider.Save(config);
            IsLocked = true;
        }

        private void PrepareGridCells()
        {
            GridCells.Clear();

            for (int x = 0; x < 24; x += 1)
            {
                for (int y = 0; y < 24; y += 1)
                {
                    GridCells.Add(new BasicStashGridCellViewModel()
                    {
                        Left = x,
                        Top = y,
                        Width = 1,
                        Height = 1
                    });
                }
            }
        }

        public IGridCellViewController AddHighlight(ItemPosition pos, StashTabType stashType)
        {
            LogTo.Debug($"Highlighting zone {pos} in tab of type {stashType}");
            pos = TransformToAbsolute(pos, stashType);

            LogTo.Debug($"Absolute position: {pos}");
            var cell = new HighlightedStashGridCellViewModel
            {
                Width = pos.Width,
                Height = pos.Height,
                Left = pos.X,
                Top = pos.Y,
            };

            highlights.Add(cell);

            var result = new GridCellController(cell);
            Disposable.Create(() => highlights.Remove(cell)).AddTo(result.Anchors);

            return result;
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
            }

            public bool IsFresh
            {
                get => cell.IsFresh;
                set => cell.IsFresh = value;
            }
        }
    }
}