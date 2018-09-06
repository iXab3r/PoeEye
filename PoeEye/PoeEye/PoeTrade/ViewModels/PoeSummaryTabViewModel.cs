using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Windows.Navigation;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;
using DynamicData.Controllers;
using LinqKit;
using Microsoft.Practices.Unity;
using PoeEye.Config;
using PoeEye.ItemParser.Services;
using PoeEye.PoeTrade.Models;
using PoeShared;
using PoeShared.Audio;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.Prism;
using ReactiveUI.Legacy;

namespace PoeEye.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Windows.Data;
    using System.Windows.Input;
    using Common;
    using CsQuery.ExtensionMethods;
    using Guards;
    using JetBrains.Annotations;
    using PoeShared.Scaffolding;
    using ReactiveUI;

    internal sealed class PoeSummaryTabViewModel : DisposableReactiveObject, IPoeSummaryTabViewModel
    {
        private static readonly TimeSpan ResortRefilterThrottleTimeout = TimeSpan.FromMilliseconds(250);

        private readonly ReactiveCommand markAllAsReadCommand;

        private bool showNewItems = true;

        private bool showRemovedItems;
        private string quickFilterText;

        private SortDescriptionData activeSortDescriptionData;

        private readonly ISubject<Unit> rebuildFilterRequest = new Subject<Unit>();
        private readonly ISubject<Unit> sortRequest = new Subject<Unit>();
        private readonly ReadOnlyObservableCollection<IMainWindowTabViewModel> tabCollection;
        private readonly IPoeTradeQuickFilter quickFilterBuilder;

        public PoeSummaryTabViewModel(
            [NotNull] ReadOnlyObservableCollection<IMainWindowTabViewModel> tabsList,
            [NotNull] IFactory<IPoeAdvancedTradesListViewModel> listFactory,
            [NotNull] IFactory<IPoeTradeQuickFilter> quickFilterFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)]
            IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(tabsList, nameof(tabsList));
            Guard.ArgumentNotNull(listFactory, nameof(listFactory));
            Guard.ArgumentNotNull(quickFilterFactory, nameof(quickFilterFactory));
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
                new SortDescriptionData(nameof(IPoeTradeViewModel.PriceInChaosOrbs), ListSortDirection.Ascending),
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
            list.Add(tabCollection);
            TradesView = list.Items;

            this.WhenAnyValue(x => x.ActiveSortDescriptionData)
                .Subscribe(x => list.SortBy(x.PropertyName, x.Direction))
                .AddTo(Anchors);

            list.Filter(rebuildFilterRequest.Select(x => BuildFilter()));

            Observable.Merge(
                    this.WhenAnyValue(x => x.ShowNewItems).ToUnit(),
                    this.WhenAnyValue(x => x.ShowRemovedItems).ToUnit(),
                    this.WhenAnyValue(x => x.QuickFilter).ToUnit()
                )
                .Throttle(ResortRefilterThrottleTimeout)
                .Subscribe(rebuildFilterRequest)
                .AddTo(Anchors);
        }

        public ReadOnlyObservableCollection<IPoeTradeViewModel> TradesView { get; }

        public ICommand MarkAllAsReadCommand => markAllAsReadCommand;

        public bool ShowNewItems
        {
            get { return showNewItems; }
            set { this.RaiseAndSetIfChanged(ref showNewItems, value); }
        }

        public bool ShowRemovedItems
        {
            get { return showRemovedItems; }
            set { this.RaiseAndSetIfChanged(ref showRemovedItems, value); }
        }

        public string QuickFilter
        {
            get { return quickFilterText; }
            set { this.RaiseAndSetIfChanged(ref quickFilterText, value); }
        }

        public SortDescriptionData[] SortingOptions { get; }

        public SortDescriptionData ActiveSortDescriptionData
        {
            get { return activeSortDescriptionData; }
            set { this.RaiseAndSetIfChanged(ref activeSortDescriptionData, value); }
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
            Log.Instance.Debug($"[PoeSummaryTabViewModel.MarkAllAsReadExecuted] Sending command to {tabCollection.Count} tab(s)");
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