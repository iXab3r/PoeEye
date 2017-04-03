using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Guards;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoeEye.PoeTrade;
using PoeShared;
using PoeShared.Common;
using PoeShared.Communications;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Stateless;
using TypeConverter;

namespace PoeEye.PoeTradeRealtimeApi
{
    internal sealed class RealtimeItemSource : IRealtimeItemSource
    {
        private static readonly string PoeTradeSearchUri = @"http://poe.trade/search";
        private static readonly string PoeTradeUri = @"http://poe.trade";

        private static readonly string InitialLiveQueryId = "-1";

        private readonly IFactory<IHttpClient> clientFactory;
        private readonly IPoeTradeParser parser;
        private readonly NameValueCollection queryPostData;

        private readonly StateMachine<State, Trigger> queryStateMachine = new StateMachine<State, Trigger>(State.Created);

        private readonly StateMachine<State, Trigger>.TriggerWithParameters<string> toNextLiveQueryTransitionTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Uri> toLiveQueryTransitionTrigger;

        private readonly ICollection<IPoeItem> itemsList = new List<IPoeItem>();

        private Uri liveQueryUri;
        private string nextLiveQueryId;

        public RealtimeItemSource(
            [NotNull] IFactory<IHttpClient> clientFactory,
            [NotNull] IConverter<IPoeQueryInfo, IPoeQuery> queryInfoToQueryConverter,
            [NotNull] IConverter<IPoeQuery, NameValueCollection> queryToPostConverter,
            [NotNull] IPoeTradeParser parser,
            [NotNull] IPoeQueryInfo queryInfo)
        {
            Guard.ArgumentNotNull(clientFactory, nameof(clientFactory));
            Guard.ArgumentNotNull(queryInfoToQueryConverter, nameof(queryInfoToQueryConverter));
            Guard.ArgumentNotNull(queryToPostConverter, nameof(queryToPostConverter));
            Guard.ArgumentNotNull(parser, nameof(parser));
            Guard.ArgumentNotNull(queryInfo, nameof(queryInfo));

            this.clientFactory = clientFactory;
            this.parser = parser;

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
                .OnEntry(Reset);

            Log.Instance.Debug($"[RealtimeItemsSource..ctor] FSM:\n {queryStateMachine.ToDotGraph()}");

            Log.Instance.Debug($"[RealtimeItemsSource..ctor] Constructing query out of supplied data: {queryInfo.DumpToText(Formatting.None)}");
            var query = queryInfoToQueryConverter.Convert(queryInfo);
            queryPostData = queryToPostConverter.Convert(query);
            Log.Instance.Debug($"[RealtimeItemsSource..ctor] Post data for supplied query has been constructed:\n Query: {queryInfo.DumpToText(Formatting.None)}\n Post: {queryPostData.DumpToText(Formatting.None)}");

            queryStateMachine.Fire(Trigger.Create);

            queryStateMachine.OnTransitioned(x => Log.Instance.Debug($"[RealtimeItemsSource.State] {x.Source} => {x.Trigger} => {x.Destination} (isReentry: {x.IsReentry})"));
            queryStateMachine.OnUnhandledTrigger((state, trigger) => Log.Instance.Debug($"[RealtimeItemsSource.UnhandledState] Failed to process trigger {trigger}, state: {state}"));
        }

        public IPoeQueryResult GetResult()
        {
            switch (queryStateMachine.State)
            {
                case State.LiveQuery:
                    Log.Instance.Debug($"[RealtimeItemsSource.GetResult] Requesting next live query result...");
                    SendLiveQuery();
                    break;

                case State.AwaitingForInitialRequest:
                    Log.Instance.Debug($"[RealtimeItemsSource.GetResult] Sending initial request...");
                    SendInitialRequest();
                    break;
            }

            return new PoeQueryResult()
            {
                ItemsList = itemsList.ToArray(),
            };
        }

        public bool IsDisposed => queryStateMachine.IsInState(State.Disposed);

        private void SetNextLiveQueryId(string queryId)
        {
            Log.Instance.Debug($"[RealtimeItemsSource.SetNextLiveQueryId] Next Live queryId: '{nextLiveQueryId}' => '{queryId}'");
            nextLiveQueryId = queryId;
        }

        private void SetLiveQueryUri(Uri newQueryUri)
        {
            Log.Instance.Debug($"[RealtimeItemsSource.SetLiveQueryUri] Next Live query URI: '{liveQueryUri}' => '{newQueryUri}'");
            liveQueryUri = newQueryUri;
        }

        private void ClearItemList()
        {
            Log.Instance.Debug($"[RealtimeItemsSource.ClearItemsList] Clearing items list...");
            itemsList.Clear();
        }

        private void Reset()
        {
            ClearItemList();
            SetNextLiveQueryId(null);
            SetLiveQueryUri(null);
        }

        private void SendInitialRequest()
        {
            var data = clientFactory
                .Create()
                .Post(PoeTradeSearchUri, queryPostData)
                .Select(ThrowIfNotParseable)
                .Select(parser.ParseQueryResponse)
                .ToTask()
                .Result;
            Log.Instance.Debug($"[RealtimeItemsSource.GetResult] Initial update contains {data.ItemsList.Length} item(s)");

            data.ItemsList.ToList().ForEach(itemsList.Add);

            ProcessInitialRequest(data);
        }

        private void ProcessInitialRequest(IPoeQueryResult data)
        {
            Uri liveUri;
            if (Uri.TryCreate(new Uri(PoeTradeUri), data.Id, out liveUri))
            {
                Log.Instance.Debug($"[RealtimeItemsSource.GetResult] Live query URI: {liveUri}");
                queryStateMachine.Fire(toLiveQueryTransitionTrigger, liveUri);
            }
            else
            {
                Log.Instance.Debug($"[RealtimeItemsSource.GetResult] Failed to extract live uri from the initial response");
                queryStateMachine.Fire(Trigger.ReceivedUnexpectedInitialResponse);
            }
        }

        private void SendLiveQuery()
        {
            Log.Instance.Debug($"[RealtimeItemsSource.Live] Sending next live query, uri: {liveQueryUri}, id: '{nextLiveQueryId}'");

            Guard.ArgumentNotNull(liveQueryUri, nameof(liveQueryUri));
            Guard.ArgumentNotNull(nextLiveQueryId, nameof(nextLiveQueryId));

            var client = clientFactory
                .Create();

            client.Cookies.Add(new Cookie("league", "Prophecy", @"/", "poe.trade"));
            client.Cookies.Add(new Cookie("live_notify_sound", "0", @"/", "poe.trade"));
            client.Cookies.Add(new Cookie("live_notify_browser", "0", @"/", "poe.trade"));
            client.Cookies.Add(new Cookie("live_frequency", "90", @"/", "poe.trade"));

            try
            {
                var rawData = client
                    .Post(liveQueryUri.AbsoluteUri, new NameValueCollection() { { "id", nextLiveQueryId } })
                    .ToTask()
                    .Result;

                var result = JToken.Parse(rawData);
                Log.Instance.Debug($"[RealtimeItemsSource.Live] Live query response: {result}");

                var nextId = result.Value<string>("newid");
                if (string.IsNullOrWhiteSpace(nextId))
                {
                    throw new DataException($"Failed to extract nextId from the supplied data:\n{result}");
                }

                var itemsCount = result.Value<string>("count");
                var itemsData = result.Value<string>("data");

                if (!string.IsNullOrWhiteSpace(itemsData))
                {
                    var data = parser.ParseQueryResponse(itemsData);
                    Log.Instance.Debug($"[RealtimeItemsSource.Live] Extracted {data.ItemsList.Length} from live query response(expected: {itemsCount})");
                    data.ItemsList.ToList().ForEach(itemsList.Add);
                }

                queryStateMachine.Fire(toNextLiveQueryTransitionTrigger, nextId);
            }
            catch (Exception ex)
            {
                Log.Instance.Warn($"[RealtimeItemsSource.Live] Exception occurred during live request", ex);
                queryStateMachine.Fire(Trigger.LiveQueryFailed);
            }
        }

        private string ThrowIfNotParseable(string queryResult)
        {
            if (string.IsNullOrWhiteSpace(queryResult))
            {
                throw new ApplicationException("Malformed query result - empty string");
            }

            return queryResult;
        }

        private enum State
        {
            Created,
            AwaitingForInitialRequest,
            LiveQuery,
            Disposed,
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

        public void Dispose()
        {
            queryStateMachine.Fire(Trigger.Dispose);
        }
    }
}
