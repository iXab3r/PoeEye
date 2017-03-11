﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using PoeEye.TradeMonitor.Modularity;
using PoeEye.TradeMonitor.ViewModels;
using PoeShared;
using PoeShared.Audio;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeWhisperMonitor;
using PoeWhisperMonitor.Chat;
using ReactiveUI;
using DisposableReactiveObject = PoeShared.Scaffolding.DisposableReactiveObject;

namespace PoeEye.TradeMonitor.Models
{
    internal sealed class TradeMonitorService : DisposableReactiveObject, ITradeMonitorService
    {
        [NotNull] private readonly IAudioNotificationsManager audioManager;
        private readonly IPoeMessageParser[] parsers;
        private readonly ISubject<TradeModel> trades = new Subject<TradeModel>();

        public TradeMonitorService(
            [NotNull] IConfigProvider<PoeTradeMonitorConfig> configProvider,
            [NotNull] IPoeWhisperService whisperService,
            [NotNull] IAudioNotificationsManager audioManager,
            [NotNull] IPoeMessageParser[] parsers)
        {
            Guard.ArgumentNotNull(() => configProvider);
            Guard.ArgumentNotNull(() => whisperService);
            Guard.ArgumentNotNull(() => audioManager);
            Guard.ArgumentNotNull(() => parsers);
            this.audioManager = audioManager;
            this.parsers = parsers;

            trades
                .Where(x => x.TradeType == TradeType.Buy)
                .Subscribe(() => audioManager.PlayNotification(configProvider.ActualConfig.NotificationType))
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
    }
}