using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CsQuery.ExtensionMethods;
using Guards;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PoeShared;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;
using WebSocketSharp;
using WebSocketSharp.Net;

namespace PoeEye.PathOfExileTrade.TradeApi
{
    internal sealed class PathOfExileTradeLiveAdapter : DisposableReactiveObject, IPathOfExileTradeLiveAdapter
    {
        private static readonly int MaxItemsToReplay = 99;
        
        private static readonly TimeSpan WebSocketGracefulCloseTimeout = TimeSpan.FromSeconds(5);
        private static readonly string WebSocketUri = @"wss://www.pathofexile.com/api/trade/live/Delve";

        private static readonly string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";

        private readonly IClock clock;
        private readonly IPoeQueryResult initialData;
        private readonly IPoeItemSource itemSource;
        private readonly Uri liveUri;

        private readonly string queryId;

        private readonly ISubject<IPoeQueryResult> resultSink = new ReplaySubject<IPoeQueryResult>(MaxItemsToReplay);

        private readonly SerialDisposable webSocketAnchors = new SerialDisposable();
        private readonly ISubject<string> itemsToFetch = new Subject<string>();
        
        public PathOfExileTradeLiveAdapter(
            [NotNull] IClock clock,
            [NotNull] IPoeItemSource itemSource,
            [NotNull] ISchedulerProvider schedulerProvider,
            [NotNull] IPoeQueryResult initialData)
        {
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(itemSource, nameof(itemSource));
            Guard.ArgumentNotNull(schedulerProvider, nameof(schedulerProvider));
            Guard.ArgumentNotNull(initialData, nameof(initialData));

            this.clock = clock;
            this.itemSource = itemSource;
            this.initialData = initialData;
            queryId = initialData.Id;

            liveUri = new Uri(new Uri(WebSocketUri), $"{initialData.Query.League}/{queryId}");
            Log.Instance.Debug($"QueryId URI: {liveUri}");

            resultSink.OnNext(initialData);

            webSocketAnchors.AddTo(Anchors);
            
            const int maxItemsPerRequest = 5;
            itemsToFetch
                .Buffer(TimeSpan.FromSeconds(1), maxItemsPerRequest)
                .ObserveOn(schedulerProvider.GetOrCreate("PathOfExileTradeFetchItemsScheduler"))
                .Where(x => x.Count > 0)
                .Subscribe(OnNext)
                .AddTo(Anchors);

            Disposable.Create(() => Log.Instance.Debug($"[WebSocket] [{queryId}] Disposing Live provider for query {queryId}")).AddTo(Anchors);

            Task.Run(() => Initialize()).ToObservable().Subscribe(_ => { Log.Instance.Debug($"[WebSocket] [{queryId}] Initialized connection"); },
                                                                  exception => resultSink.OnError(exception));
        }

        private async void OnNext(IList<string> itemIds)
        {
            try
            {
                Log.Instance.Debug($"[WebSocket] [{queryId}] Requesting ({itemIds.Count} items)");
                var items = await itemSource.FetchItems(initialData, itemIds.ToList());
                Log.Instance.Debug($"[WebSocket] [{queryId}] Successfully retrieved ({items.ItemsList.Length} items)");
                items.ItemsList.ForEach(x => x.ItemState = PoeTradeState.New);
                resultSink.OnNext(items);
            }
            catch (Exception e)
            {
                Log.Instance.Error($"[WebSocket] [{queryId}] Failed to get items pack {itemIds.DumpToTextRaw()}", e);
            }
        }

        public IObservable<IPoeQueryResult> Updates => resultSink;

        private void Initialize()
        {
            var anchors = new CompositeDisposable();
            webSocketAnchors.Disposable = anchors;

            var webSocket = new WebSocket(liveUri.ToString())
            {
                EnableRedirection = true,
                Compression = CompressionMethod.Deflate,
                Origin = "https://www.pathofexile.com",
                EmitOnPing = true,
                Log = {Level = LogLevel.Trace, Output = (data, s) => Log.Instance.Debug($"[WebSocket] [{queryId}] Inner: {s}, data: {data.DumpToTextRaw()}")}
            };
            webSocket.SetCookie(new Cookie("POESESSID", "64e5acf7aa8fdc51af716a1f73b8db64"));

            Disposable.Create(() => WebSocketCloseSafe(webSocket)).AddTo(anchors);
            webSocket.AddTo(anchors);

            Observable.FromEventPattern<MessageEventArgs>(
                          h => webSocket.OnMessage += h,
                          h => webSocket.OnMessage -= h)
                      .Where(x => x.EventArgs.Data != null)
                      .Select(x => x.EventArgs)
                      .Subscribe(HandleWebSocketData, HandleWebSocketError)
                      .AddTo(anchors);
            ;

            Observable.FromEventPattern<ErrorEventArgs>(
                          h => webSocket.OnError += h,
                          h => webSocket.OnError -= h)
                      .Select(x => x.EventArgs.Exception)
                      .Subscribe(HandleWebSocketError, HandleWebSocketError)
                      .AddTo(anchors);

            Observable.FromEventPattern(
                          h => webSocket.OnOpen += h,
                          h => webSocket.OnOpen -= h)
                      .Subscribe(_ => HandleWebSocketOpened(webSocket), HandleWebSocketError)
                      .AddTo(anchors);

            Observable.FromEventPattern<CloseEventArgs>(
                          h => webSocket.OnClose += h,
                          h => webSocket.OnClose -= h)
                      .Subscribe(_ => HandleWebSocketClosed(webSocket), HandleWebSocketError)
                      .AddTo(anchors);
            webSocket.Connect();
        }

        private async void FetchItems(string rawResponse)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<ItemUpdate>(rawResponse);
                Log.Instance.Debug($"[WebSocket] [{queryId}] Processing new items ({response.NewItemId?.EmptyIfNull().Count()} items)");
                foreach (var itemId in response.NewItemId.EmptyIfNull())
                {
                    itemsToFetch.OnNext(itemId);
                }
            }
            catch (Exception e)
            {
                Log.Instance.Error($"[WebSocket] [{queryId}] Failed to process update {rawResponse}", e);
            }
        }

        private async void WebSocketCloseSafe(WebSocket webSocket)
        {
            var anchors = new CompositeDisposable();
            try
            {
                Log.Instance.Debug($"[WebSocket] [{queryId}] Trying to gracefully close webSocket...");

                var isClosed = new SemaphoreSlim(0);
                Observable.FromEventPattern<CloseEventArgs>(
                              h => webSocket.OnClose += h,
                              h => webSocket.OnClose -= h)
                          .Take(1)
                          .Subscribe(() => isClosed.Release())
                          .AddTo(anchors);

                webSocket.CloseAsync();

                await isClosed.WaitAsync(WebSocketGracefulCloseTimeout);
                Log.Instance.Debug($"[WebSocket] [{queryId}] Successfully closed webSocket, state: {webSocket.ReadyState}");
            }
            catch (ObjectDisposedException)
            {
                Log.Instance.Warn($"[WebSocket] [{queryId}] Failed to close websocket gracefully, socket is disposed");
            }
            catch (TimeoutException)
            {
                Log.Instance.Error($"[WebSocket] [{queryId}] Failed to close websocket, timeout occured, state: {webSocket.ReadyState}");
            }
            finally
            {
                anchors.Dispose();
            }
        }

        private void HandleWebSocketOpened(WebSocket webSocket)
        {
            Log.Instance.Debug($"[WebSocket] [{queryId}] Socket opened");
        }

        private void HandleWebSocketClosed(WebSocket webSocket)
        {
            Log.Instance.Debug($"[WebSocket] [{queryId}] Socket closed, state: {webSocket.ReadyState}");
            resultSink.OnCompleted();
        }

        private void HandleWebSocketError(Exception error)
        {
            Log.Instance.Debug($"[WebSocket] [{queryId}] Error ! {error.Message}");
            Log.HandleException(error);
            resultSink.OnError(error);
        }

        private void HandleWebSocketData(MessageEventArgs eventArgs)
        {
            Log.Instance.Debug(
                $"[WebSocket] [{queryId}] Got data(isBinary: {eventArgs.IsBinary}, isText: {eventArgs.IsText}, isPing: {eventArgs.IsPing}): {eventArgs.RawData.Length}");

            if (eventArgs.IsPing)
            {
                resultSink.OnNext(new PoeQueryResult
                {
                    Id = queryId,
                    Query = initialData.Query,
                });
            }
            
            if (!eventArgs.IsText)
            {
                return;
            }
            Log.Instance.Debug($"[WebSocket] [{queryId}] Raw data:\n{eventArgs.Data?.DumpToText(Formatting.None)} (binary: {eventArgs.RawData?.Length})");

            if (string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                return;
            }

            FetchItems(eventArgs.Data);
        }

        private void WebSocketSend(WebSocket webSocket, string message)
        {
            Log.Instance.Debug($"[WebSocket] [{queryId}] Sending message (state: {webSocket.ReadyState}): {message}");
            webSocket.Send(message);
        }

        public class ItemUpdate
        {
            [JsonProperty("new")]
            public string[] NewItemId { get; set; }
        }
    }
}