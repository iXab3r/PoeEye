using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PoeShared;
using PoeShared.Common;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace PoeEye.PathOfExileTrade.TradeApi
{
    public class PathOfExileTradeLiveApi : DisposableReactiveObject, IPathOfExileTradeLiveApi
    {
        private static readonly TimeSpan WebSocketGracefulCloseTimeout = TimeSpan.FromSeconds(1);
        private static readonly string WebSocketUri = @"wss://www.pathofexile.com/api/trade/live/Delve";

        private static readonly string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";

        private readonly IClock clock;
        private readonly Uri liveUri;

        private readonly string queryId;

        private readonly ISubject<IPoeQueryResult> resultSink = new ReplaySubject<IPoeQueryResult>();

        private readonly SerialDisposable webSocketAnchors = new SerialDisposable();

        public PathOfExileTradeLiveApi(
            [NotNull] IClock clock,
            [NotNull] IPoeQueryResult initialData)
        {
            this.clock = clock;
            queryId = initialData.Id;

            liveUri = new Uri(new Uri(WebSocketUri), $"{initialData.Query.League}/{queryId}");
            Log.Instance.Debug($"QueryId URI: {liveUri}");

            initialData.ItemsList.ForEach(x => x.ItemState = PoeTradeState.New);
            resultSink.OnNext(initialData);

            Task.Run(() => Initialize()).ToObservable().Subscribe(_ => { Log.Instance.Debug($"[WebSocket] [{queryId}] Initialized connection"); },
                                                                  exception => resultSink.OnError(exception));
        }

        public IObservable<IPoeQueryResult> Updates => resultSink;

        private void Initialize()
        {
            var anchors = new CompositeDisposable();
            webSocketAnchors.Disposable = anchors;

            var cookies = new List<KeyValuePair<string, string>>
            {
               //new KeyValuePair<string, string>("POESESSID", "4625dedda20f9dcfcc224b97ac444fff")
            };
            var headers = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Sec-WebSocket-Version", "13")
            };

            var webSocket = new WebSocket(
                liveUri.ToString(),
                "",
                cookies,
                headers,
                UserAgent,
                "https://www.pathofexile.com",
                WebSocketVersion.Rfc6455)
            {
                Security =
                {
                    AllowUnstrustedCertificate = true, EnabledSslProtocols = SslProtocols.Default, AllowNameMismatchCertificate = true,
                    AllowCertificateChainErrors = true
                },
            };

            Disposable.Create(() => WebSocketCloseSafe(webSocket)).AddTo(anchors);
            webSocket.AddTo(anchors);

            Observable.FromEventPattern<MessageReceivedEventArgs>(
                          h => webSocket.MessageReceived += h,
                          h => webSocket.MessageReceived -= h)
                      .Select(x => x.EventArgs.Message)
                      .Subscribe(HandleWebSocketMessage, HandleWebSocketError)
                      .AddTo(anchors);

            Observable.FromEventPattern<ErrorEventArgs>(
                          h => webSocket.Error += h,
                          h => webSocket.Error -= h)
                      .Select(x => x.EventArgs.Exception)
                      .Subscribe(HandleWebSocketError, HandleWebSocketError)
                      .AddTo(anchors);

            Observable.FromEventPattern<DataReceivedEventArgs>(
                          h => webSocket.DataReceived += h,
                          h => webSocket.DataReceived -= h)
                      .Select(x => x.EventArgs.Data)
                      .Subscribe(data => HandleWebSocketData(data ?? new byte[0]), HandleWebSocketError)
                      .AddTo(anchors);

            Observable.FromEventPattern(
                          h => webSocket.Opened += h,
                          h => webSocket.Opened -= h)
                      .Subscribe(_ => HandleWebSocketOpened(webSocket), HandleWebSocketError)
                      .AddTo(anchors);

            Observable.FromEventPattern(
                          h => webSocket.Closed += h,
                          h => webSocket.Closed -= h)
                      .Subscribe(_ => HandleWebSocketClosed(webSocket), HandleWebSocketError)
                      .AddTo(anchors);
            webSocket.Open();
        }

        private void WebSocketCloseSafe(WebSocket webSocket)
        {
            try
            {
                Log.Instance.Debug($"[WebSocket] [{queryId}] Trying to gracefully close webSocket...");
                webSocket.Close();

                var gracefulClose = Observable.FromEventPattern(
                                                  h => webSocket.Closed += h,
                                                  h => webSocket.Closed -= h)
                                              .Take(1);

                var dueTime = clock.Now + WebSocketGracefulCloseTimeout;
                gracefulClose.Timeout(dueTime).Wait();
                Log.Instance.Debug($"[WebSocket] [{queryId}] Successfully closed webSocket, state: {webSocket.State}");
            }
            catch (ObjectDisposedException)
            {
                Log.Instance.Warn($"[WebSocket] [{queryId}] Failed to close websocket gracefully, socket is disposed");
            }
            catch (TimeoutException)
            {
                Log.Instance.Error($"[WebSocket] [{queryId}] Failed to close websocket, timeout occured, state: {webSocket.State}");
            }
        }

        private void HandleWebSocketOpened(WebSocket webSocket)
        {
            Log.Instance.Debug($"[WebSocket] [{queryId}] Socket opened");
        }

        private void HandleWebSocketClosed(WebSocket webSocket)
        {
            Log.Instance.Debug($"[WebSocket] [{queryId}] Socket closed, state: {webSocket.State}");
            resultSink.OnCompleted();
        }

        private void HandleWebSocketError(Exception error)
        {
            Log.Instance.Debug($"[WebSocketError] [{queryId}]  Error ! {error.Message}");
            Log.HandleException(error);
            resultSink.OnError(error);
        }

        private void HandleWebSocketData(byte[] data)
        {
            Log.Instance.Debug($"[RealtimeItemsSource.Data] [{queryId}] Got data: {data.Length}b");
            var rawData = Encoding.Unicode.GetString(data);
            Log.Instance.Debug($"[WebSocketData] [{queryId}] Raw data:\n{rawData.DumpToText(Formatting.None)}");

            HandleWebSocketMessage(rawData);
        }

        private void HandleWebSocketMessage(string rawMessage)
        {
            Log.Instance.Debug($"[WebSocketMessage] [{queryId}] Got message: {rawMessage.DumpToText()}");
        }

        private void WebSocketSend(WebSocket webSocket, string liveQueryId, string message)
        {
            Log.Instance.Debug($"[WebSocketMessage] [{liveQueryId}] Sending message (state: {webSocket.State}): {message}");
            webSocket.Send(message);
        }
    }
}