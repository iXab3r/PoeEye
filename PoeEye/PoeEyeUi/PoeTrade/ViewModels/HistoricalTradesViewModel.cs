namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using PoeShared.Common;
    using PoeShared.Utilities;

    using ReactiveUI;

    internal sealed class HistoricalTradesViewModel : DisposableReactiveObject, IHistoricalTradesViewModel
    {
        private readonly IReactiveList<IPoeTradeViewModel> actualTrades;
        private readonly IReactiveList<IPoeItem> historicalTrades;
        private readonly IPoePriceCalculcator poePriceCalculcator;

        private bool isExpanded;

        private double maxPrice;

        private DateTime maxTimestamp;

        private double minPrice;

        private DateTime minTimestamp;

        public HistoricalTradesViewModel(
            [NotNull] IReactiveList<IPoeItem> historicalTrades,
            [NotNull] IReactiveList<IPoeTradeViewModel> actualTrades,
            [NotNull] IPoePriceCalculcator poePriceCalculcator)
        {
            Guard.ArgumentNotNull(() => actualTrades);
            Guard.ArgumentNotNull(() => historicalTrades);
            Guard.ArgumentNotNull(() => poePriceCalculcator);

            this.historicalTrades = historicalTrades;
            this.actualTrades = actualTrades;
            this.poePriceCalculcator = poePriceCalculcator;

            historicalTrades.Changed.ToUnit().Merge(actualTrades.Changed.ToUnit())
                            .StartWith(Unit.Default)
                            .Subscribe(RefreshPoints)
                            .AddTo(Anchors);
        }

        public double MinPrice
        {
            get { return minPrice; }
            set { this.RaiseAndSetIfChanged(ref minPrice, value); }
        }

        public DateTime MinTimestamp
        {
            get { return minTimestamp; }
            set { this.RaiseAndSetIfChanged(ref minTimestamp, value); }
        }

        public double MaxPrice
        {
            get { return maxPrice; }
            set { this.RaiseAndSetIfChanged(ref maxPrice, value); }
        }

        public DateTime MaxTimestamp
        {
            get { return maxTimestamp; }
            set { this.RaiseAndSetIfChanged(ref maxTimestamp, value); }
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set { this.RaiseAndSetIfChanged(ref isExpanded, value); }
        }

        public IReactiveList<PoeItemPricePoint> HistoricalPoints { get; } = new ReactiveList<PoeItemPricePoint>();

        public IReactiveList<PoeItemPricePoint> ActualPoints { get; } = new ReactiveList<PoeItemPricePoint>();

        private void RefreshPoints()
        {
            RefreshActualPoints();
            RefreshHistoricalPoints();

            var newMinPrice = 0d;
            var newMaxPrice = 0d;
            var newMinTimestamp = DateTime.MaxValue;
            var newMaxTimestamp = DateTime.MinValue;

            var pointsToAnalyze = ActualPoints.Concat(HistoricalPoints).ToArray();
            if (pointsToAnalyze.Any())
            {
                foreach (var source in pointsToAnalyze)
                {
                    newMinPrice = Math.Min(source.Price, newMinPrice);
                    newMaxPrice = Math.Max(source.Price, newMaxPrice);

                    if (source.Timestamp < newMinTimestamp)
                    {
                        newMinTimestamp = source.Timestamp;
                    }

                    if (source.Timestamp > newMaxTimestamp)
                    {
                        newMaxTimestamp = source.Timestamp;
                    }
                }
            }

            MinPrice = newMinPrice - newMinPrice * 0.1;
            MaxPrice = newMaxPrice + newMaxPrice * 0.1;
            MinTimestamp = newMinTimestamp;
            MaxTimestamp = newMaxTimestamp;
        }

        private void RefreshHistoricalPoints()
        {
            var historicalPoints = historicalTrades
               .Select(ToPoint)
               .Where(x => x != null)
               .Select(x => x.Value)
               .ToArray();
            ActualizeList(HistoricalPoints, historicalPoints);
        }

        private void RefreshActualPoints()
        {
            var actualPoints = actualTrades
               .Select(ToPoint)
               .Where(x => x != null)
               .Select(x => x.Value)
               .ToArray();

            ActualizeList(ActualPoints, actualPoints);

        }

        private static void ActualizeList(IReactiveList<PoeItemPricePoint> list, PoeItemPricePoint[] points)
        {
            var pointsToAdd = points.Except(list).ToArray();
            var pointsToRemove = list.Except(points).ToArray();

            if (!pointsToAdd.Any() && !pointsToRemove.Any())
            {
                return;
            }

            using (list.SuppressChangeNotifications())
            {
                list.AddRange(pointsToAdd);
                list.RemoveAll(pointsToRemove);
            }
        }

        private static PoeItemPricePoint? ToPoint(IPoeTradeViewModel trade)
        {
            return ToPoint(trade.PriceInChaosOrbs, trade.Trade.Timestamp);
        }

        private PoeItemPricePoint? ToPoint(IPoeItem trade)
        {
            var price = poePriceCalculcator.GetEquivalentInChaosOrbs(trade.Price);
            return ToPoint(price, trade.Timestamp);
        }

        private static PoeItemPricePoint? ToPoint(float? price, DateTime timestamp)
        {
            if (price == null || !IsValid(timestamp))
            {
                return null;
            }
            return new PoeItemPricePoint
            {
                Price = price.Value,
                Timestamp = timestamp
            };
        }

        private static bool IsValid(DateTime time)
        {
            return time != DateTime.MinValue && time != DateTime.MaxValue;
        }
    }
}