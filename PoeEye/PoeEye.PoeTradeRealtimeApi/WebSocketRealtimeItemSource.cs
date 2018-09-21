using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Common.Logging;
using DynamicData;
using DynamicData.Cache.Internal;
using Guards;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
using SuperSocket.ClientEngine;
using TypeConverter;
using WebSocket4Net;

namespace PoeEye.PoeTradeRealtimeApi
{
    internal sealed class WebSocketRealtimeItemSource : DisposableReactiveObject, IRealtimeItemSource
    {
        private static readonly ILog Log = LogManager.GetLogger<WebSocketRealtimeItemSource>();
        
        private static readonly string PoeTradeSearchUri = @"http://poe.trade/search";
        private static readonly string PoeTradeWebSocketUri = @"ws://live.poe.trade";
        private static readonly TimeSpan WebSocketPingInterval = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan WebSocketGracefulCloseTimeout = TimeSpan.FromSeconds(1);

        private static readonly string UserAgent =
            @"Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";

        private static readonly string InitialLiveQueryId = "-1";
        private readonly IFactory<IHttpClient> clientFactory;

        private readonly IClock clock;

        private readonly LockFreeObservableCache<IPoeItem, string> itemsList = new LockFreeObservableCache<IPoeItem, string>();
        private readonly IPoeTradeParser parser;
        private readonly string queryLeague;
        private readonly NameValueCollection queryPostData;
        private readonly StateMachine<State, Trigger> queryStateMachine = new StateMachine<State, Trigger>(State.Created);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<string> toLiveQueryTransitionTrigger;

        private readonly StateMachine<State, Trigger>.TriggerWithParameters<string> toNextLiveQueryTransitionTrigger;

        private readonly SerialDisposable webSocketAnchors = new SerialDisposable();

        private string liveQueryName;
        private string nextLiveQueryId;

        public WebSocketRealtimeItemSource(
            [NotNull] IClock clock,
            [NotNull] IFactory<IHttpClient> clientFactory,
            [NotNull] IConverter<IPoeQueryInfo, IPoeQuery> queryInfoToQueryConverter,
            [NotNull] IConverter<IPoeQuery, NameValueCollection> queryToPostConverter,
            [NotNull] IPoeTradeParser parser,
            [NotNull] IPoeQueryInfo queryInfo)
        {
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(clientFactory, nameof(clientFactory));
            Guard.ArgumentNotNull(queryInfoToQueryConverter, nameof(queryInfoToQueryConverter));
            Guard.ArgumentNotNull(queryToPostConverter, nameof(queryToPostConverter));
            Guard.ArgumentNotNull(parser, nameof(parser));
            Guard.ArgumentNotNull(queryInfo, nameof(queryInfo));

            this.clock = clock;
            this.clientFactory = clientFactory;
            this.parser = parser;

            webSocketAnchors.AddTo(Anchors);
            Disposable.Create(() => queryStateMachine.Fire(Trigger.Dispose)).AddTo(Anchors);

            toNextLiveQueryTransitionTrigger = queryStateMachine.SetTriggerParameters<string>(Trigger.LiveQuerySucceeded);
            toLiveQueryTransitionTrigger = queryStateMachine.SetTriggerParameters<string>(Trigger.LiveQueryStarted);

            queryStateMachine
                .Configure(State.Created)
                .Permit(Trigger.Dispose, State.Disposed)
                .Permit(Trigger.Create, State.AwaitingForInitialRequest);

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
                .OnEntryFrom(Trigger.LiveQueryStarted, SetupLiveQuery)
                .PermitReentry(Trigger.LiveQuerySucceeded)
                .Permit(Trigger.LiveQueryFailed, State.AwaitingForInitialRequest);

            queryStateMachine
                .Configure(State.Disposed)
                .OnEntry(Reset);

            Log.Debug($"FSM:\n {queryStateMachine.ToDotGraph()}");

            Log.Debug($"Constructing query out of supplied data: {queryInfo.DumpToText(Formatting.None)}");
            queryLeague = queryInfo.League;
            var query = queryInfoToQueryConverter.Convert(queryInfo);
            queryPostData = queryToPostConverter.Convert(query);
            Log.Debug(
                $"Post data for supplied query has been constructed:\n Query: {queryInfo.DumpToText(Formatting.None)}\n Post: {queryPostData.DumpToText(Formatting.None)}");

            queryStateMachine.Fire(Trigger.Create);

            queryStateMachine.OnTransitioned(
                x => Log.Debug($"[RealtimeItemsSource.State] {x.Source} => {x.Trigger} => {x.Destination} (isReentry: {x.IsReentry})"));
            queryStateMachine.OnUnhandledTrigger((state, trigger) =>
                                                     Log.Debug(
                                                         $"[RealtimeItemsSource.UnhandledState] Failed to process trigger {trigger}, state: {state}"));
        }

        public IPoeQueryResult GetResult()
        {
            switch (queryStateMachine.State)
            {
                case State.AwaitingForInitialRequest:
                    Log.Debug($"Sending initial request...");
                    SendInitialRequest();
                    break;
            }

            return new PoeQueryResult
            {
                ItemsList = itemsList.Items.ToArray()
            };
        }

        public bool IsDisposed => queryStateMachine.IsInState(State.Disposed);

        private void ClearItemList()
        {
            Log.Debug($"Clearing items list...");
            itemsList.Clear();
        }

        private void Reset()
        {
            Log.Debug($"Resetting state, items count: {itemsList.Count}");

            ClearItemList();
            webSocketAnchors.Disposable = null;
        }

        private void SendInitialRequest()
        {
            var client = PrepareClient();

            var data = client
                       .Post(PoeTradeSearchUri, queryPostData)
                       .Select(ThrowIfNotParseable)
                       .Select(parser.ParseQueryResponse)
                       .ToTask()
                       .Result;
            var validItems = data.ItemsList.Where(x => !string.IsNullOrWhiteSpace(x.Hash)).ToArray();
            Log.Debug($"Initial update contains {data.ItemsList.Length} item(s), of which {validItems.Length} are valid");

            validItems.ForEach(x => itemsList.Edit(y => y.AddOrUpdate(x, x.Hash)));

            ProcessInitialRequest(data);
        }

        private IHttpClient PrepareClient()
        {
            var client = clientFactory.Create();

            client.CustomHeaders.Add("Origin", "http://poe.trade");
            client.CustomHeaders.Add("Cache-Control", "max-age=0");
            client.CustomHeaders.Add("Upgrade-Insecure-Requests", "1");

            client.Cookies.Add(new Cookie("league", queryLeague, @"/", "poe.trade"));
            client.Cookies.Add(new Cookie("live_notify_sound", "0", @"/", "poe.trade"));
            client.Cookies.Add(new Cookie("live_notify_browser", "0", @"/", "poe.trade"));
            client.Cookies.Add(new Cookie("live_frequency", "0", @"/", "poe.trade"));

            client.Referer = "http://poe.trade";
            client.UserAgent = UserAgent;

            return client;
        }

        private void ProcessInitialRequest(IPoeQueryResult data)
        {
            Log.Debug($"Processing initial response, data.Id: {data.Id}");
            var rawIdMatch = Regex.Match(data.Id ?? string.Empty, @"search\/(?'id'.*?)\/live", RegexOptions.IgnoreCase);
            if (rawIdMatch.Success)
            {
                var liveQueryId = rawIdMatch.Groups["id"].Value;
                Log.Debug($"Live query Id: {liveQueryId}");
                queryStateMachine.Fire(toLiveQueryTransitionTrigger, liveQueryId);
            }
            else
            {
                Log.Debug($"Failed to extract live uri from the initial response");
                queryStateMachine.Fire(Trigger.ReceivedUnexpectedInitialResponse);
            }
        }

        private void SetNextLiveQueryId(string queryId)
        {
            Log.Debug($"Next Live queryId: '{nextLiveQueryId}' => '{queryId}'");
            nextLiveQueryId = queryId;
        }

        private void SetLiveQueryUri(string newLiveQueryId)
        {
            Log.Debug($"Next Live queryName: '{liveQueryName}' => '{newLiveQueryId}'");
            liveQueryName = newLiveQueryId;
        }

        private void SetupLiveQuery()
        {
            Log.Debug($"Setting up webSocket, live queryName: {liveQueryName}");

            try
            {
                SendLiveQuery();

                var liveUri = new Uri(new Uri(PoeTradeWebSocketUri), liveQueryName);

                Log.Debug($"Live URI: {liveUri}");
                var anchors = new CompositeDisposable();
                webSocketAnchors.Disposable = anchors;

                var cookies = new List<KeyValuePair<string, string>>();
                var headers = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits")
                };

                var webSocket = new WebSocket(
                    liveUri.ToString(),
                    "",
                    cookies,
                    headers,
                    UserAgent,
                    "http://poe.trade",
                    WebSocketVersion.Rfc6455)
                {
                };
                Disposable.Create(() => WebSocketCloseSafe(webSocket, liveQueryName)).AddTo(anchors);
                webSocket.AddTo(anchors);

                Observable.FromEventPattern<MessageReceivedEventArgs>(
                              h => webSocket.MessageReceived += h,
                              h => webSocket.MessageReceived -= h)
                          .Select(x => x.EventArgs.Message)
                          .Subscribe(message => HandleWebSocketMessage(message, liveQueryName), ex => HandleWebSocketError(ex, liveQueryName))
                          .AddTo(anchors);

                Observable.FromEventPattern<ErrorEventArgs>(
                              h => webSocket.Error += h,
                              h => webSocket.Error -= h)
                          .Select(x => x.EventArgs.Exception)
                          .Subscribe(ex => HandleWebSocketError(ex, liveQueryName), ex => HandleWebSocketError(ex, liveQueryName))
                          .AddTo(anchors);

                Observable.FromEventPattern<DataReceivedEventArgs>(
                              h => webSocket.DataReceived += h,
                              h => webSocket.DataReceived -= h)
                          .Select(x => x.EventArgs.Data)
                          .Subscribe(data => HandleWebSocketData(data ?? new byte[0], liveQueryName), ex => HandleWebSocketError(ex, liveQueryName))
                          .AddTo(anchors);

                Observable.FromEventPattern(
                              h => webSocket.Opened += h,
                              h => webSocket.Opened -= h)
                          .Subscribe(_ => HandleWebSocketOpened(webSocket, liveQueryName), ex => HandleWebSocketError(ex, liveQueryName))
                          .AddTo(anchors);

                Observable.FromEventPattern(
                              h => webSocket.Closed += h,
                              h => webSocket.Closed -= h)
                          .Subscribe(_ => HandleWebSocketClosed(webSocket, liveQueryName), ex => HandleWebSocketError(ex, liveQueryName))
                          .AddTo(anchors);

                Observable
                    .Timer(WebSocketPingInterval, WebSocketPingInterval)
                    .Subscribe(() => HandleWebSocketPingRequest(webSocket, liveQueryName), ex => HandleWebSocketError(ex, liveQueryName))
                    .AddTo(anchors);

                Log.Debug($"Opening webSocket...");
                webSocket.Open();

                itemsList
                    .Connect()
                    .Where(x => webSocket.State == WebSocketState.Open)
                    .WhereReasonsAre(ChangeReason.Add)
                    .SelectMany(x => x)
                    .Subscribe(item => WebSocketSubscribeToItemUpdates(webSocket, liveQueryName, item.Current), ex => HandleWebSocketError(ex, liveQueryName))
                    .AddTo(anchors);

                Log.Debug($"Listening for updates");
            }
            catch (Exception e)
            {
                Log.HandleException(e);
                queryStateMachine.Fire(Trigger.LiveQueryFailed);
            }
        }

        private void WebSocketCloseSafe(WebSocket webSocket, string liveQueryId)
        {
            try
            {
                Log.Debug($"[WebSocket] [{liveQueryId}] Trying to gracefully close webSocket...");
                webSocket.Close();

                var gracefulClose = Observable.FromEventPattern(
                                                  h => webSocket.Closed += h,
                                                  h => webSocket.Closed -= h)
                                              .Take(1);

                var dueTime = clock.Now + WebSocketGracefulCloseTimeout;
                gracefulClose.Timeout(dueTime).Wait();
                Log.Debug($"[WebSocket] [{liveQueryId}] Successfully closed webSocket, state: {webSocket.State}");
            }
            catch (ObjectDisposedException)
            {
            }
            catch (TimeoutException)
            {
                Log.Debug($"[WebSocket] [{liveQueryId}] Failed to close websocket, imeout occured, state: {webSocket.State}");
            }
        }

        private void HandleWebSocketOpened(WebSocket webSocket, string liveQueryId)
        {
            Log.Debug($"[WebSocket] [{liveQueryId}] Socket opened");

            var versionMessage = new WsGenericOperation {OperationType = WsOperationType.Version, Value = 3};

            WebSocketSend(webSocket, liveQueryId, versionMessage);
            WebSocketSend(webSocket, liveQueryId, "ping");
        }

        private void WebSocketSubscribeToItemUpdates(WebSocket webSocket, string liveQueryId, IPoeItem item)
        {
            Log.Debug($"[WebSocketMessage] [{liveQueryId}] Subscribing for updates of item {item.DumpToText(Formatting.None)}");
            var message = new WsGenericOperation
            {
                OperationType = WsOperationType.Subscribe,
                Value = item.Hash
            };
            WebSocketSend(webSocket, liveQueryName, message);
        }

        private void HandleWebSocketClosed(WebSocket webSocket, string liveQueryId)
        {
            Log.Debug($"[WebSocket] [{liveQueryId}] Socket closed, state: {webSocket.State}");
        }

        private void HandleWebSocketError(Exception error, string liveQueryId)
        {
            Log.Debug($"[WebSocketError] [{liveQueryId}]  Error ! {error.Message}");
            Log.HandleException(error);
            queryStateMachine.Fire(Trigger.LiveQueryFailed);
        }

        private void HandleWebSocketData(byte[] data, string liveQueryId)
        {
            Log.Debug($"[RealtimeItemsSource.Data] [{liveQueryId}] Got data: {data.Length}b");
            var rawData = Encoding.Unicode.GetString(data);
            Log.Debug($"[WebSocketData] [{liveQueryId}] Raw data:\n{rawData.DumpToText(Formatting.None)}");

            HandleWebSocketMessage(rawData, liveQueryId);
        }

        private void HandleWebSocketMessage(string rawMessage, string liveQueryId)
        {
            Log.Debug($"[WebSocketMessage] [{liveQueryId}] Got message: {rawMessage.DumpToText()}");

            var message = WsGenericOperation.Deserialize(rawMessage);
            Log.Debug($"[WebSocketData] [{liveQueryId}] Deserialized data:\n{message.DumpToText(Formatting.None)}");

            switch (message.OperationType)
            {
                case WsOperationType.Delete:
                    HandleItemRemoval(liveQueryId, message.Value as string);
                    break;
                case WsOperationType.Notify:
                    HandleLiveUpdate(liveQueryId, ((long)message.Value).ToString());
                    break;
            }
        }

        private void HandleWebSocketPingRequest(WebSocket webSocket, string liveQueryId)
        {
            Log.Debug($"[WebSocketMessage] [{liveQueryId}] Pinging (state: {webSocket.State}) ...");

            WebSocketSend(webSocket, liveQueryId, "ping");
        }

        private void WebSocketSend(WebSocket webSocket, string liveQueryId, string message)
        {
            Log.Debug($"[WebSocketMessage] [{liveQueryId}] Sending message (state: {webSocket.State}): {message}");
            webSocket.Send(message);
        }

        private void WebSocketSend(WebSocket webSocket, string liveQueryId, WsGenericOperation message)
        {
            var raw = message.Serialize();
            WebSocketSend(webSocket, liveQueryId, raw);
        }

        private void HandleItemRemoval(string liveQueryId, string itemId)
        {
            Log.Debug($"[WebSocket] [{liveQueryId}] Removing item '{itemId}' (itemsCount: {itemsList.Count})");
            itemsList.Edit(x => x.RemoveKey(itemId));
            Log.Debug($"[WebSocket] [{liveQueryId}] Resulting items count: {itemsList.Count})");
        }

        private void HandleLiveUpdate(string liveQueryId, string nextLiveUpdateId)
        {
            Log.Debug($"[WebSocket] [{liveQueryId}] Got next update Id: {nextLiveUpdateId}");
            SendLiveQuery();
            SetNextLiveQueryId(nextLiveUpdateId);
        }

        private void SendLiveQuery()
        {
            Log.Debug($"Sending next live query, name: {liveQueryName}, id: '{nextLiveQueryId}'");

            Guard.ArgumentNotNull(liveQueryName, nameof(liveQueryName));
            Guard.ArgumentNotNull(nextLiveQueryId, nameof(nextLiveQueryId));

            var client = PrepareClient();

            try
            {
                var liveQueryUri = new Uri($"{PoeTradeSearchUri}/{liveQueryName}/live");
                Log.Debug($"Issueing live query, uri: {liveQueryUri}");
                var rawData = client
                              .Post(liveQueryUri.AbsoluteUri, new NameValueCollection {{"id", nextLiveQueryId}})
                              .ToTask()
                              .Result;

                var result = JToken.Parse(rawData);
                Log.Debug($"Live query response: {result.DumpToText(Formatting.None)}");

                var nextId = result.Value<string>("newid");
                if (string.IsNullOrWhiteSpace(nextId))
                {
                    throw new DataException($"Failed to extract nextId from the supplied data:\n{result.DumpToText(Formatting.None)}");
                }

                var itemsCount = result.Value<string>("count");
                var itemsData = result.Value<string>("data");

                if (!string.IsNullOrWhiteSpace(itemsData))
                {
                    var data = parser.ParseQueryResponse(itemsData);

                    var validItems = data.ItemsList.EmptyIfNull().Where(x => !string.IsNullOrWhiteSpace(x.Hash)).ToArray();
                    Log.Debug(
                        $"Extracted {data.ItemsList.Length} from live query response(expected: {itemsCount}), of which {validItems.Length} are valid");

                    var itemsToRemove = validItems.Where(x => x.ItemState == PoeTradeState.Removed).ToArray();
                    var itemsToAdd = validItems.Where(x => x.ItemState != PoeTradeState.Removed).ToArray();

                    Log.Debug($"Items to add: {itemsToAdd.Length}, items to remove: {itemsToRemove.Length}, current list size: {itemsList.Count}");

                    itemsToAdd.ForEach(x => itemsList.Edit(y => y.AddOrUpdate(x, x.Hash)));
                    itemsToRemove.ForEach(x => itemsList.Edit(y => y.RemoveKey(x.Hash)));
                    Log.Debug($"Resulting list size: {itemsList.Count}");
                }

                queryStateMachine.Fire(toNextLiveQueryTransitionTrigger, nextId);
            }
            catch (Exception ex)
            {
                Log.Warn($"Exception occurred during live request", ex);
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

        private struct WsGenericOperation
        {
            [JsonProperty("type")]
            [JsonConverter(typeof(StringEnumConverter))]
            public WsOperationType OperationType { get; set; }

            [JsonProperty("value")]
            public object Value { get; set; }

            public string Serialize()
            {
                var result = JsonConvert.SerializeObject(this);
                return result;
            }

            public static WsGenericOperation Deserialize(string raw)
            {
                try
                {
                    return JsonConvert.DeserializeObject<WsGenericOperation>(raw);
                }
                catch (Exception)
                {
                    return new WsGenericOperation();
                }
            }
        }

        private enum WsOperationType
        {
            Unknown,

            [EnumMember(Value = "subscribe")] Subscribe,

            [EnumMember(Value = "del")] Delete,

            [EnumMember(Value = "notify")] Notify,

            [EnumMember(Value = "version")] Version,

            [EnumMember(Value = "pong")] Pong
        }
    }
}