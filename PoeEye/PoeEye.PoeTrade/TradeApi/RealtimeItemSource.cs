using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoeShared.Common;
using PoeShared.Communications;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Stateless;
using TypeConverter;
using Unity.Attributes;

namespace PoeEye.PoeTrade.TradeApi
{
    internal sealed class RealtimeItemSource : DisposableReactiveObject, IPoeTradeLiveAdapter
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RealtimeItemSource));

        private static readonly TimeSpan LiveQueryRefreshPeriod = TimeSpan.FromSeconds(5);

        private static readonly string PoeTradeUri = @"http://poe.trade";

        private static readonly string InitialLiveQueryId = "-1";
        private static readonly int MaxItemsToReplay = 99;

        private readonly ISubject<IPoeQueryResult> resultSink = new ReplaySubject<IPoeQueryResult>(MaxItemsToReplay);

        private readonly IFactory<IHttpClient> clientFactory;

        private readonly SerialDisposable liveAnchors = new SerialDisposable();
        private readonly IPoeTradeParser parser;
        private readonly IPoeQueryResult initialData;
        private readonly string queryLeague;
        private readonly string queryId;
        private readonly IPoeQueryResult emptyResult;

        private readonly StateMachine<State, Trigger> queryStateMachine = new StateMachine<State, Trigger>(State.Created);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Uri> toLiveQueryTransitionTrigger;

        private readonly StateMachine<State, Trigger>.TriggerWithParameters<string> toNextLiveQueryTransitionTrigger;

        private Uri liveQueryUri;
        private string nextLiveQueryId;

        public RealtimeItemSource(
            [NotNull] IPoeQueryResult initialData,
            [NotNull] IFactory<IHttpClient> clientFactory,
            [NotNull] IConverter<IPoeQueryInfo, IPoeQuery> queryInfoToQueryConverter,
            [NotNull] IConverter<IPoeQuery, NameValueCollection> queryToPostConverter,
            [NotNull] IPoeTradeParser parser,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(initialData, nameof(initialData));
            Guard.ArgumentNotNull(clientFactory, nameof(clientFactory));
            Guard.ArgumentNotNull(queryInfoToQueryConverter, nameof(queryInfoToQueryConverter));
            Guard.ArgumentNotNull(queryToPostConverter, nameof(queryToPostConverter));
            Guard.ArgumentNotNull(parser, nameof(parser));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));

            this.clientFactory = clientFactory;
            this.parser = parser;
            this.initialData = initialData;
            Log.Debug($"Constructing query out of supplied data: {initialData.DumpToTextRaw()}");

            queryId = initialData.Id;
            queryLeague = initialData.Query.League;

            if (string.IsNullOrWhiteSpace(queryId))
            {
                throw new ArgumentException($"QueryId must be set in the supplied data, got: {initialData.DumpToTextRaw()}");
            }
            
            if (string.IsNullOrWhiteSpace(queryLeague))
            {
                throw new ArgumentException($"League must be set in the supplied data, got: {initialData.DumpToTextRaw()}");
            }

            emptyResult = ToQueryResult(Array.Empty<IPoeItem>());

            liveAnchors.AddTo(Anchors);
            Disposable.Create(() =>
            {
                Log.Debug($"[{queryId}] Disposing live query...");
                queryStateMachine.Fire(Trigger.Dispose);
            }).AddTo(Anchors);

            queryStateMachine
                .Configure(State.Created)
                .Permit(Trigger.Dispose, State.Disposed)
                .Permit(Trigger.Create, State.AwaitingForInitialRequest);

            toNextLiveQueryTransitionTrigger = queryStateMachine.SetTriggerParameters<string>(Trigger.LiveQuerySucceeded);
            toLiveQueryTransitionTrigger = queryStateMachine.SetTriggerParameters<Uri>(Trigger.LiveQueryStarted);

            queryStateMachine
                .Configure(State.AwaitingForInitialRequest)
                .OnEntry(Reset)
                .Permit(Trigger.Dispose, State.Disposed)
                .Permit(Trigger.LiveQueryStarted, State.LiveQuery)
                .PermitReentry(Trigger.ReceivedUnexpectedInitialResponse);

            queryStateMachine
                .Configure(State.LiveQuery)
                .Permit(Trigger.Dispose, State.Disposed)
                .OnEntryFrom(toLiveQueryTransitionTrigger, SetLiveQueryUri)
                .OnEntryFrom(toLiveQueryTransitionTrigger, _ => SetNextLiveQueryId(InitialLiveQueryId))
                .OnEntryFrom(toNextLiveQueryTransitionTrigger, SetNextLiveQueryId)
                .PermitReentry(Trigger.LiveQuerySucceeded)
                .Permit(Trigger.LiveQueryFailed, State.AwaitingForInitialRequest);

            queryStateMachine
                .Configure(State.Disposed)
                .Ignore(Trigger.LiveQueryFailed)
                .Ignore(Trigger.LiveQuerySucceeded)
                .Ignore(Trigger.LiveQueryStarted)
                .Ignore(Trigger.ReceivedUnexpectedInitialResponse)
                .OnEntry(Reset);

            if (Log.IsTraceEnabled)
            {
                Log.Trace($"[{queryId}] FSM:\n {queryStateMachine.ToDotGraph()}");
            }

            queryStateMachine.Fire(Trigger.Create);

            queryStateMachine.OnTransitioned(x =>
            {
                if (Log.IsTraceEnabled)
                {
                    Log.Trace($"[{queryId}] State {x.Source} => {x.Trigger} => {x.Destination} (isReentry: {x.IsReentry})");
                }
            });
            queryStateMachine.OnUnhandledTrigger((state, trigger) =>
            {
                Log.Error($"[{queryId}] Failed to process trigger {trigger}, state: {state}");
                resultSink.OnError(new ApplicationException($"Failed to process trigger {trigger}, state: {state}"));
            });

            Observable
                .Timer(TimeSpan.Zero, LiveQueryRefreshPeriod, bgScheduler)
                .Subscribe(RefreshData)
                .AddTo(Anchors);
        }

        private IPoeQueryResult ToQueryResult(IEnumerable<IPoeItem> items)
        {
            return new PoeQueryResult
            {
                Id = initialData.Id,
                Query = initialData.Query,
                ItemsList = items.ToArray(),
            };
        }

        private void SetNextLiveQueryId(string nextQueryId)
        {
            Log.Debug($"[{queryId}] Next Live queryId: '{nextLiveQueryId}' => '{nextQueryId}'");
            nextLiveQueryId = nextQueryId;
        }
        
        private void RefreshData()
        {
            switch (queryStateMachine.State)
            {
                case State.LiveQuery:
                    Log.Debug($"[{queryId}] Requesting next live query result...");
                    SendLiveQuery();
                    break;

                case State.AwaitingForInitialRequest:
                    Log.Debug($"[{queryId}] Processing initial data...");
                    ProcessInitialRequest(initialData);
                    break;
            }
        }

        private void SetLiveQueryUri(Uri newQueryUri)
        {
            Log.Debug($"[{queryId}] Next Live query URI: '{liveQueryUri}' => '{newQueryUri}'");
            liveQueryUri = newQueryUri;
        }

        private void Reset()
        {
            SetNextLiveQueryId(null);
            SetLiveQueryUri(null);
            liveAnchors.Disposable = null;
        }

        private void ProcessInitialRequest(IPoeQueryResult data)
        {
            resultSink.OnNext(ToQueryResult(data.ItemsList));

            if (Uri.TryCreate(new Uri(PoeTradeUri), data.Id, out var liveUri))
            {
                Log.Debug($"[{queryId}] Live query URI: {liveUri}");
                queryStateMachine.Fire(toLiveQueryTransitionTrigger, liveUri);
            }
            else
            {
                Log.Debug($"[{queryId}] Failed to extract live uri from the initial response");
                queryStateMachine.Fire(Trigger.ReceivedUnexpectedInitialResponse);
            }
        }

        private void SendLiveQuery()
        {
            Log.Debug($"[{queryId}] Sending next live query, uri: {liveQueryUri}, id: '{nextLiveQueryId}'");

            Guard.ArgumentNotNull(liveQueryUri, nameof(liveQueryUri));
            Guard.ArgumentNotNull(nextLiveQueryId, nameof(nextLiveQueryId));

            var client = clientFactory
                .Create();

            client.Cookies.Add(new Cookie("league", queryLeague, @"/", "poe.trade"));
            client.Cookies.Add(new Cookie("live_notify_sound", "0", @"/", "poe.trade"));
            client.Cookies.Add(new Cookie("live_notify_browser", "0", @"/", "poe.trade"));
            client.Cookies.Add(new Cookie("live_frequency", $"{LiveQueryRefreshPeriod.TotalSeconds:F0}", @"/", "poe.trade"));

            try
            {
                var rawData = client
                              .Post(liveQueryUri.AbsoluteUri, new NameValueCollection {{"id", nextLiveQueryId}})
                              .ToTask()
                              .Result;

                var result = JToken.Parse(rawData);
                Log.Debug($"[{queryId}] Live query response: {result}");

                var nextId = result.Value<string>("newid");
                if (string.IsNullOrWhiteSpace(nextId))
                {
                    throw new DataException($"[{queryId}] Failed to extract nextId from the supplied data:\n{result}");
                }

                var itemsCount = result.Value<string>("count");
                var itemsData = result.Value<string>("data");

                IPoeItem[] items = null;
                if (!string.IsNullOrWhiteSpace(itemsData))
                {
                    var data = parser.ParseQueryResponse(itemsData);
                    
                    if (data.ItemsList?.Length > 0)
                    {
                        Log.Debug($"[{queryId}] Extracted {data.ItemsList.Length} valid item from live query response(expected: {itemsCount})");
                        data.ItemsList.ForEach(x => x.ItemState = PoeTradeState.New);
                        items = data.ItemsList;
                    }
                }

                var requestResult = items?.Any() == true ? ToQueryResult(items) : emptyResult;
                if (Log.IsTraceEnabled)
                {
                    Log.Trace($"Reporting query result, data: {requestResult.DumpToTextRaw()}");
                }
                resultSink.OnNext(requestResult);

                queryStateMachine.Fire(toNextLiveQueryTransitionTrigger, nextId);
            }
            catch (Exception ex)
            {
                Log.Warn($"[{queryId}] Exception occurred during live request", ex);
                queryStateMachine.Fire(Trigger.LiveQueryFailed);
            }
        }

        private enum State
        {
            Created,
            AwaitingForInitialRequest,
            LiveQuery,
            Disposed
        }

        private enum Trigger
        {
            Create,
            ReceivedUnexpectedInitialResponse,
            LiveQueryStarted,
            LiveQuerySucceeded,
            LiveQueryFailed,
            Dispose
        }

        public IObservable<IPoeQueryResult> Updates => resultSink;
    }
}