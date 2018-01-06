using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using PoeBud.Models;
using PoeBud.Scaffolding;
using PoeBud.Services;
using PoeShared;
using PoeShared.Common;
using PoeShared.Converters;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;
using Prism.Commands;
using ReactiveUI;

namespace PoeBud.ViewModels
{
    internal sealed class PriceSummaryViewModel : DisposableReactiveObject, IPriceSummaryViewModel
    {
        private readonly IPoePriceCalculcator poePriceCalculcator;
        private readonly IHighlightingService highlightingService;
        private PoePrice priceInChaosOrbs;

        private IPoeTradeSolution solution;
        private ObservableCollection<Tuple<PoePrice, PoePrice>> pricesByType = new ObservableCollection<Tuple<PoePrice, PoePrice>>();

        public PriceSummaryViewModel(
            [NotNull] IPoePriceCalculcator poePriceCalculcator,
            [NotNull] IHighlightingService highlightingService)
        {
            Guard.ArgumentNotNull(poePriceCalculcator, nameof(poePriceCalculcator));
            Guard.ArgumentNotNull(highlightingService, nameof(highlightingService));

            this.poePriceCalculcator = poePriceCalculcator;
            this.highlightingService = highlightingService;

            PricesByType = new ReadOnlyObservableCollection<Tuple<PoePrice, PoePrice>>(pricesByType);

            this.WhenAnyValue(x => x.Solution)
                .Subscribe(x => PriceInChaosOrbs = x == null ? PoePrice.Empty : CalculateTotal(x))
                .AddTo(Anchors);

            ShowHighlighting = new DelegateCommand(() => highlightingService.Highlight(Solution));
            HideHighlighting = new DelegateCommand(() => highlightingService.Highlight(null));
        }

        public IPoeTradeSolution Solution
        {
            get => solution;
            set => this.RaiseAndSetIfChanged(ref solution, value);
        }

        public PoePrice PriceInChaosOrbs
        {
            get => priceInChaosOrbs;
            set => this.RaiseAndSetIfChanged(ref priceInChaosOrbs, value);
        }
        
        public ReadOnlyObservableCollection<Tuple<PoePrice, PoePrice>> PricesByType { get; }
        
        public ICommand HideHighlighting { get; }
        
        public ICommand ShowHighlighting { get; }
        
        private PoePrice CalculateTotal(IPoeTradeSolution solution)
        {
            Log.Instance.Debug($"Calculating Currency total...");

            var currencyWithAmount = solution
                .Items
                .Select(x => new { Item = x, Price = StringToPoePriceConverter.Instance.Convert($"{x.StackSize} {x.TypeLine}")})
                .Select(x => new { Item = x, RawPrice = x.Price, PriceInChaosOrbs = poePriceCalculcator.GetEquivalentInChaosOrbs(x.Price) })
                .Where(x => x.PriceInChaosOrbs.HasValue)
                .ToArray();
            
            var currencyByType = currencyWithAmount
                .GroupBy(x => x.RawPrice.CurrencyType);

            var prices = new List<Tuple<PoePrice, PoePrice>>();
            foreach (var grouping in currencyByType)
            {
                var currencySum = grouping.Select(x => x.RawPrice.Value).Sum();
                var chaosSum = grouping.Select(x => x.PriceInChaosOrbs.Value).Sum();

                var currencyPrice = new PoePrice(grouping.Key, currencySum);
                var chaosPrice = new PoePrice(grouping.First().PriceInChaosOrbs.CurrencyType, chaosSum);
                prices.Add(new Tuple<PoePrice, PoePrice>(currencyPrice, chaosPrice));
            }
            Log.Instance.Debug($"Currency found:\n\t{prices.Select(x => new { Item = x.Item1, ChaosEquiv = x.Item2 }).DumpToTable()}");
            
            pricesByType.Clear();
            prices.OrderByDescending(x => x.Item2.Value).ForEach(pricesByType.Add);
            
            var sum = currencyWithAmount.Any()
                ? currencyWithAmount.Select(x => x.PriceInChaosOrbs.Value).Sum()
                : 0;
            
            return new PoePrice(KnownCurrencyNameList.ChaosOrb, sum);
        }
    }
}