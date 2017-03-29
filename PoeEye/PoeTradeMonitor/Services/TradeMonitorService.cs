using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Guards;
using JetBrains.Annotations;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.Modularity;
using PoeEye.TradeMonitor.Services.Notifications;
using PoeShared;
using PoeShared.Audio;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;
using PoeWhisperMonitor;
using PoeWhisperMonitor.Chat;
using DisposableReactiveObject = PoeShared.Scaffolding.DisposableReactiveObject;

namespace PoeEye.TradeMonitor.Services
{
    internal sealed class TradeMonitorService : DisposableReactiveObject, ITradeMonitorService
    {
        [NotNull] private readonly IAudioNotificationsManager audioManager;
        [NotNull] private readonly IPoeNotifier notifier;
        private readonly IPoeMessageParser[] parsers;
        private readonly ISubject<TradeModel> trades = new Subject<TradeModel>();

        public TradeMonitorService(
            [NotNull] IConfigProvider<PoeTradeMonitorConfig> configProvider,
            [NotNull] IPoeWhisperService whisperService,
            [NotNull] IAudioNotificationsManager audioManager,
            [NotNull] IPoePriceCalculcator priceCalculcator,
            [NotNull] IPoeNotifier notifier,
            [NotNull] IPoeMessageParser[] parsers)
        {
            Guard.ArgumentNotNull(() => configProvider);
            Guard.ArgumentNotNull(() => whisperService);
            Guard.ArgumentNotNull(() => audioManager);
            Guard.ArgumentNotNull(() => priceCalculcator);
            Guard.ArgumentNotNull(() => notifier);
            Guard.ArgumentNotNull(() => parsers);

            this.audioManager = audioManager;
            this.notifier = notifier;
            this.parsers = parsers;

            trades
                .Where(x => x.TradeType == TradeType.Sell)
                .Subscribe(() => audioManager.PlayNotification(configProvider.ActualConfig.NotificationType))
                .AddTo(Anchors);

            trades
                .Where(x => x.TradeType == TradeType.Sell)
                .Select(x => new { Trade = x, PriceInChaos = priceCalculcator.GetEquivalentInChaosOrbs(x.Price) })
                .Where(x => x.PriceInChaos.Value >= configProvider.ActualConfig.CriticalNotificationThresholdInChaos)
                .Subscribe(x => HandleHighValueTrade(x.Trade, x.PriceInChaos))
                .AddTo(Anchors);

            whisperService
                .Messages
                .Select(ParseMessage)
                .Select(ProcessMessage)
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .Subscribe(trades)
                .AddTo(Anchors);
        }

        public IObservable<TradeModel> Trades => trades;

        private TradeModel? ProcessMessage(IDictionary<IPoeMessageParser, TradeModel> successfullMatches)
        {
            if (successfullMatches.Count > 1)
            {
                Log.Instance.Warn($"[TradeMonitorService.ParseMessage] Multiple successfull matches:\n{successfullMatches.DumpToText()}");
            }
            return successfullMatches.Count > 0 
                ? successfullMatches.First().Value 
                : default(TradeModel?);
        }

        private IDictionary<IPoeMessageParser, TradeModel> ParseMessage(PoeMessage message)
        {
            Log.Instance.Warn($"[TradeMonitorService.ParseMessage] Processing message {message.DumpToText()}...");
            var result = new Dictionary<IPoeMessageParser, TradeModel>();
            foreach (var poeMessageParser in parsers)
            {
                TradeModel parseResult;
                if (poeMessageParser.TryParse(message, out parseResult))
                {
                    result[poeMessageParser] = parseResult;
                }

            }
            return result;
        }

        private void HandleHighValueTrade(TradeModel trade, PoePrice priceInChaos)
        {
            var message = $"[{trade.Timestamp}] TradeMonitor: {trade.CharacterName} wants to buy {trade.PositionName} for {trade.Price} (~{priceInChaos}), league: {trade.League}, tab: {trade.TabName} position: {trade.ItemPosition}";
            if (!string.IsNullOrWhiteSpace(trade.Offer))
            {
                message += $" offer: {trade.Offer}";
            }
            notifier.SendNotification(message, NotificationLevel.Critical);
        }
    }
}