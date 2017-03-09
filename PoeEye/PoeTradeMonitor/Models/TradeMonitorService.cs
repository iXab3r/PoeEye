using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Scaffolding;
using PoeWhisperMonitor;
using PoeWhisperMonitor.Chat;
using DisposableReactiveObject = PoeShared.Scaffolding.DisposableReactiveObject;

namespace PoeEye.TradeMonitor.Models
{
    public class TradeMonitorService : DisposableReactiveObject, ITradeMonitorService
    {
        private readonly IPoeMessageParser[] parsers;
        private readonly ISubject<TradeModel> trades = new Subject<TradeModel>();

        public TradeMonitorService(
            [NotNull] IPoeWhisperService whisperService,
            [NotNull] IPoeMessageParser[] parsers)
        {
            Guard.ArgumentNotNull(() => whisperService);
            Guard.ArgumentNotNull(() => parsers);
            this.parsers = parsers;

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