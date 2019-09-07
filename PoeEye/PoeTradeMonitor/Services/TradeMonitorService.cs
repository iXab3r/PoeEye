using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.Modularity;
using PoeEye.TradeMonitor.Services.Notifications;
using PoeShared.Audio;
using PoeShared.Audio.Services;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;
using PoeWhisperMonitor;
using PoeWhisperMonitor.Chat;

namespace PoeEye.TradeMonitor.Services
{
    internal sealed class TradeMonitorService : DisposableReactiveObject, ITradeMonitorService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TradeMonitorService));

        private readonly IAudioNotificationsManager audioManager;
        private readonly IPoeNotifier notifier;
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
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(whisperService, nameof(whisperService));
            Guard.ArgumentNotNull(audioManager, nameof(audioManager));
            Guard.ArgumentNotNull(priceCalculcator, nameof(priceCalculcator));
            Guard.ArgumentNotNull(notifier, nameof(notifier));
            Guard.ArgumentNotNull(parsers, nameof(parsers));

            this.audioManager = audioManager;
            this.notifier = notifier;
            this.parsers = parsers;

            trades
                .Where(x => x.TradeType == TradeType.Sell)
                .Subscribe(() => audioManager.PlayNotification(configProvider.ActualConfig.NotificationType))
                .AddTo(Anchors);

            trades
                .Where(x => x.TradeType == TradeType.Sell)
                .Select(x => new {Trade = x, PriceInChaos = priceCalculcator.GetEquivalentInChaosOrbs(x.Price)})
                .Where(x => x.PriceInChaos.Value >= configProvider.ActualConfig.CriticalNotificationThresholdInChaos)
                .Subscribe(x => HandleHighValueTrade(x.Trade, x.PriceInChaos))
                .AddTo(Anchors);

            whisperService
                .Messages
                .Where(x => x.MessageType == PoeMessageType.WhisperIncoming || x.MessageType == PoeMessageType.WhisperOutgoing)
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
                Log.Debug($"[TradeMonitorService.ParseMessage] Multiple successfull matches:\n{successfullMatches.DumpToText()}");
            }

            return successfullMatches.Count > 0
                ? successfullMatches.First().Value
                : default(TradeModel?);
        }

        private IDictionary<IPoeMessageParser, TradeModel> ParseMessage(PoeMessage message)
        {
            Log.Debug($"[TradeMonitorService.ParseMessage] Processing message {message.DumpToText()}...");
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
            var message =
                $"[{trade.Timestamp}] TradeMonitor: {trade.CharacterName} wants to buy {trade.PositionName} for {trade.Price} (~{priceInChaos}), league: {trade.League}, tab: {trade.TabName} position: {trade.ItemPosition}";
            if (!string.IsNullOrWhiteSpace(trade.Offer))
            {
                message += $" offer: {trade.Offer}";
            }

            notifier.SendNotification(message, NotificationLevel.Critical);
        }
    }
}