using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using DynamicData;
using DynamicData.Binding;
using Guards;
using JetBrains.Annotations;
using LinqKit;
using PoeEye.PoeTrade.Shell.ViewModels;
using PoeEye.PoeTrade.ViewModels;
using PoeEye.TradeSummaryOverlay.Modularity;
using PoeShared.Audio;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity.Attributes;

namespace PoeEye.TradeSummaryOverlay.ViewModels
{
    internal sealed class PoeTradeSummaryViewModel : OverlayViewModelBase
    {
        private static readonly TimeSpan ThrottleTimeout = TimeSpan.FromMilliseconds(250);

        private readonly IConfigProvider<PoeTradeSummaryConfig> configProvider;
        private readonly ReadOnlyObservableCollection<IMainWindowTabViewModel> tabCollection;

        private HorizontalAlignment controlBarAlignment;

        private bool isVisible;

        private double listScaleFactor;

        public PoeTradeSummaryViewModel(
            [NotNull] IConfigProvider<PoeTradeSummaryConfig> configProvider,
            [NotNull] IOverlayWindowController controller,
            [NotNull] IFactory<IPoeAdvancedTradesListViewModel> listFactory,
            [NotNull] IMainWindowViewModel mainWindow,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(controller, nameof(controller));
            Guard.ArgumentNotNull(listFactory, nameof(listFactory));
            Guard.ArgumentNotNull(mainWindow, nameof(mainWindow));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            this.configProvider = configProvider;

            MinSize = new Size(300, 300);
            MaxSize = new Size(double.NaN, double.NaN);
            Top = 200;
            Left = 200;
            Width = 300;
            Height = 300;
            SizeToContent = SizeToContent.Manual;
            IsUnlockable = true;
            Title = "Trade Summary";

            WhenLoaded
                .Take(1)
                .Select(x => configProvider.WhenChanged)
                .Switch()
                .Subscribe(ApplyConfig)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsLocked)
                .Where(x => IsLocked)
                .Subscribe(() => IsVisible = true)
                .AddTo(Anchors);

            var list = listFactory.Create();
            list.MaxItems = 10;
            list.SortBy(nameof(IPoeItem.Timestamp), ListSortDirection.Descending);
            list.Filter(Observable.Return(BuildFilter()));

            mainWindow
                .TabsList
                .ToObservableChangeSet()
                .FilterOnProperty(
                    x => x.SelectedAudioNotificationType,
                    x => x.SelectedAudioNotificationType != AudioNotificationType.Disabled,
                    ThrottleTimeout)
                .Bind(out tabCollection)
                .Subscribe()
                .AddTo(Anchors);

            list.Add(tabCollection);

            TradesView = list.Items;
        }

        public bool IsVisible
        {
            get => isVisible;
            set => this.RaiseAndSetIfChanged(ref isVisible, value);
        }

        public HorizontalAlignment ControlBarAlignment
        {
            get => controlBarAlignment;
            set => this.RaiseAndSetIfChanged(ref controlBarAlignment, value);
        }

        public double ListScaleFactor
        {
            get => listScaleFactor;
            set => this.RaiseAndSetIfChanged(ref listScaleFactor, value);
        }

        public ReadOnlyObservableCollection<IPoeTradeViewModel> TradesView { get; }

        private void ApplyConfig(PoeTradeSummaryConfig config)
        {
            base.ApplyConfig(config);

            ControlBarAlignment = config.ControlBarAlignment;
            IsVisible = config.IsVisible;
            ListScaleFactor = config.ScaleFactor;
        }

        protected override void LockWindowCommandExecuted()
        {
            base.LockWindowCommandExecuted();

            var config = configProvider.ActualConfig;
            SavePropertiesToConfig(config);

            config.ControlBarAlignment = ControlBarAlignment;
            config.IsVisible = IsVisible;
            config.ScaleFactor = ListScaleFactor;

            configProvider.Save(config);
        }

        private Predicate<IPoeTradeViewModel> BuildFilter()
        {
            var filter = PredicateBuilder.True<IPoeTradeViewModel>().And(x => x.TradeState == PoeTradeState.New);

            return new Predicate<IPoeTradeViewModel>(filter.Compile());
        }
    }
}