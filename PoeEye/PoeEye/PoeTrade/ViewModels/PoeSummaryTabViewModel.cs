using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using Common.Logging;
using DynamicData;
using DynamicData.Binding;
using Guards;
using JetBrains.Annotations;
using LinqKit;
using PoeEye.Config;
using PoeEye.PoeTrade.Common;
using PoeEye.PoeTrade.Models;
using PoeShared.Audio;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Unity.Attributes;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeSummaryTabViewModel : DisposableReactiveObject, IPoeSummaryTabViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeSummaryTabViewModel));

        private static readonly TimeSpan ResortRefilterThrottleTimeout = TimeSpan.FromMilliseconds(250);

        private readonly ReactiveCommand markAllAsReadCommand;
        private readonly IPoeTradeQuickFilter quickFilterBuilder;

        private readonly ISubject<Unit> rebuildFilterRequest = new Subject<Unit>();
        private readonly ISubject<Unit> sortRequest = new Subject<Unit>();
        private readonly ReadOnlyObservableCollection<IMainWindowTabViewModel> tabCollection;

        private SortDescriptionData activeSortDescriptionData;
        private string quickFilterText;

        private bool showNewItems = true;

        private bool showRemovedItems;

        public PoeSummaryTabViewModel(
            [NotNull] ReadOnlyObservableCollection<IMainWindowTabViewModel> tabsList,
            [NotNull] IFactory<IPoeAdvancedTradesListViewModel> listFactory,
            [NotNull] IFactory<IPoeTradeQuickFilter> quickFilterFactory,
            [NotNull] IConfigProvider<PoeEyeMainConfig> configProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(tabsList, nameof(tabsList));
            Guard.ArgumentNotNull(listFactory, nameof(listFactory));
            Guard.ArgumentNotNull(quickFilterFactory, nameof(quickFilterFactory));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            markAllAsReadCommand = ReactiveCommand.Create(MarkAllAsReadExecuted);

            quickFilterBuilder = quickFilterFactory.Create();

            this.WhenAnyValue(x => x.ActiveSortDescriptionData)
                .ToUnit()
                .Subscribe(sortRequest)
                .AddTo(Anchors);

            SortingOptions = new[]
            {
                new SortDescriptionData(nameof(IPoeItem.Timestamp), ListSortDirection.Descending),
                new SortDescriptionData(nameof(IPoeItem.Timestamp), ListSortDirection.Ascending),
                new SortDescriptionData(nameof(IPoeTradeViewModel.PriceInChaosOrbs), ListSortDirection.Descending),
                new SortDescriptionData(nameof(IPoeTradeViewModel.PriceInChaosOrbs), ListSortDirection.Ascending)
            };
            ActiveSortDescriptionData = SortingOptions.FirstOrDefault();

            var list = listFactory.Create();

            tabsList
                .ToObservableChangeSet()
                .FilterOnProperty(
                    x => x.SelectedAudioNotificationType,
                    x => x.SelectedAudioNotificationType != AudioNotificationType.Disabled,
                    ResortRefilterThrottleTimeout)
                .Bind(out tabCollection)
                .Subscribe()
                .AddTo(Anchors);
            configProvider.WhenChanged.Subscribe(x => list.PageParameter.PageSize = x.ItemPageSize).AddTo(Anchors);
            list.Add(tabCollection);
            TradesView = list.Items;
            PageParameter = list.PageParameter;

            this.WhenAnyValue(x => x.ActiveSortDescriptionData)
                .Subscribe(x => list.SortBy(x.PropertyName, x.Direction))
                .AddTo(Anchors);

            list.Filter(rebuildFilterRequest.Select(x => BuildFilter()));

            Observable.Merge(
                          this.WhenAnyValue(x => x.ShowNewItems).ToUnit(),
                          this.WhenAnyValue(x => x.ShowRemovedItems).ToUnit(),
                          this.WhenAnyValue(x => x.QuickFilter).ToUnit()
                      )
                      .Subscribe(rebuildFilterRequest)
                      .AddTo(Anchors);
        }

        public string QuickFilter
        {
            get => quickFilterText;
            set => this.RaiseAndSetIfChanged(ref quickFilterText, value);
        }

        public IPageParameterDataViewModel PageParameter { get; }

        public ReadOnlyObservableCollection<IPoeTradeViewModel> TradesView { get; }

        public ICommand MarkAllAsReadCommand => markAllAsReadCommand;

        public bool ShowNewItems
        {
            get => showNewItems;
            set => this.RaiseAndSetIfChanged(ref showNewItems, value);
        }

        public bool ShowRemovedItems
        {
            get => showRemovedItems;
            set => this.RaiseAndSetIfChanged(ref showRemovedItems, value);
        }

        public SortDescriptionData[] SortingOptions { get; }

        public SortDescriptionData ActiveSortDescriptionData
        {
            get => activeSortDescriptionData;
            set => this.RaiseAndSetIfChanged(ref activeSortDescriptionData, value);
        }

        private Predicate<IPoeTradeViewModel> BuildFilter()
        {
            var filter = PredicateBuilder.True<IPoeTradeViewModel>();

            filter = filter.And(PredicateBuilder.False<IPoeTradeViewModel>()
                                                .Or(x => ShowNewItems && x.TradeState == PoeTradeState.New)
                                                .Or(x => ShowRemovedItems && x.TradeState == PoeTradeState.Removed));

            if (!string.IsNullOrWhiteSpace(QuickFilter))
            {
                filter = filter.And(x => quickFilterBuilder.Apply(quickFilterText, x));
            }

            return new Predicate<IPoeTradeViewModel>(filter.Compile());
        }

        private void MarkAllAsReadExecuted()
        {
            Log.Debug($"[PoeSummaryTabViewModel.MarkAllAsReadExecuted] Sending command to {tabCollection.Count} tab(s)");
            foreach (var mainWindowTabViewModel in tabCollection)
            {
                if (mainWindowTabViewModel.MarkAllAsReadCommand.CanExecute(null))
                {
                    mainWindowTabViewModel.MarkAllAsReadCommand.Execute(null);
                }
            }
        }
    }
}