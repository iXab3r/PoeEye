using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Remoting;
using System.Threading;
using Guards;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoeEye.ExileToolsApi.Converters;
using PoeEye.ExileToolsApi.RealtimeApi.Entities;
using PoeShared;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;
using Quobject.SocketIoClientDotNet.Client;
using TypeConverter;

namespace PoeEye.ExileToolsApi.RealtimeApi
{
    internal sealed class RealtimeItemSource : IRealtimeItemSource
    {
        public static readonly TimeSpan ClientKeepAliveTimeSpan = TimeSpan.FromMinutes(1);

        private readonly string uniqueClientId = $"PoeEye {Guid.NewGuid()}";

        private readonly JArray realtimeQuery;
        private readonly IConverter<ItemConversionInfo, IPoeItem> poeItemConverter;
        private readonly IClock clock;

        private readonly Socket client;

        private readonly ConcurrentBag<IPoeItem> itemsList = new ConcurrentBag<IPoeItem>();

        private readonly CompositeDisposable anchors = new CompositeDisposable();

        private Exception lastException = null;

        private DateTime lastFetchTime;

        public RealtimeItemSource(
            [NotNull] IPoeQueryInfo query,
            [NotNull] IConverter<ItemConversionInfo, IPoeItem> poeItemConverter,
            [NotNull] IClock clock,
            [NotNull] IConverter<IPoeQueryInfo, RealtimeQuery> toQueryConverter)
        {
            Guard.ArgumentNotNull(() => query);
            Guard.ArgumentNotNull(() => poeItemConverter);
            Guard.ArgumentNotNull(() => clock);
            Guard.ArgumentNotNull(() => toQueryConverter);

            this.poeItemConverter = poeItemConverter;
            this.clock = clock;

            var convertedQuery = new RealtimeQueries
            {
                 toQueryConverter.Convert(query)
            };

            realtimeQuery = JArray.FromObject(convertedQuery);
            Log.Instance.Debug($"[BlockItemSource..ctor] Initializing RealTime query\nQuery source: {query.DumpToText(Formatting.None)}\n\nRealTime query: {realtimeQuery.DumpToText(Formatting.None)}");

            lastFetchTime = clock.Now;

            Log.Instance.Debug($"[BlockItemSource.Connect] Initializing connection, client: {client}");
            Observable
                .Timer(DateTimeOffset.Now, ClientKeepAliveTimeSpan)
                .Subscribe(Cleanup)
                .AddTo(anchors);

            var options = new IO.Options
            {
                Query = new Dictionary<string, string> { { "pwxid", uniqueClientId } },
            };

            client = IO.Socket(@"http://rtstashapi.exiletools.com", options);

            client
                .On("error", OnClientError)
                .On("connect_error", OnClientError)
                .On("item", OnClientItemReceived)
                .On("heartbeat", OnClientHeartbeat)
                .On("connect", OnClientConnected);
        }

        public bool IsDisposed { get; private set; }

        public IPoeQueryResult GetResult()
        {
            Log.Instance.Debug($"[BlockItemSource.GetResult] Retrieving items (items available: {itemsList.Count}), client: {client}...");

            lastFetchTime = clock.Now;

            var exceptionToThrow = Interlocked.Exchange(ref lastException, null);
            if (exceptionToThrow != null)
            {
                throw exceptionToThrow;
            }

            var result = itemsList.ToArray();
            Log.Instance.Debug($"[BlockItemSource.GetResult] Got {result.Length} items (items left: {itemsList.Count}), client: {client}...");
            return new PoeQueryResult()
            {
                ItemsList = result,
            };
        }

        public void Dispose()
        {
            IsDisposed = true;
            client.Disconnect();
        }

        private void OnClientConnected(object rawMessage)
        {
            Log.Instance.Debug($"[BlockItemSource.Connected] Connected successfully, greeting: {rawMessage}, client: {client}");

            client.Emit("filter", realtimeQuery);

            Log.Instance.Debug($"[BlockItemSource.Connected] Sent filter, client: {client}, filter: {realtimeQuery.DumpToText()}");
        }

        private void OnClientError(object rawError)
        {
            Log.Instance.Warn($"[BlockItemSource.Error] Error occured, client: {client}, error: {rawError}");
            lastException = new ServerException($"Exception occurred - {rawError}");

            try
            {
                Log.Instance.Warn($"[BlockItemSource.Error] Reconnecting... client: {client}");
                client.Disconnect().Connect();
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"Exception occurred", ex);
            }
        }

        private void OnClientHeartbeat(object rawHeartbeat)
        {
            Log.Instance.Info($"[BlockItemSource.Heartbeat] Heartbeat received, client: {client}, message: {rawHeartbeat}");
        }

        private void OnClientItemReceived(object rawItemData)
        {
            Log.Instance.Info($"[BlockItemSource.Item] Item received, client: {client}, item: {rawItemData}");

            if (!(rawItemData is JToken))
            {
                Log.Instance.Warn($"[BlockItemSource.Item] Expected JToken, but got something else, client: {client}, data: {rawItemData}");
                return;
            }

            try
            {
                var conversionInfo = new ItemConversionInfo((JToken)rawItemData);
                var item = poeItemConverter.Convert(conversionInfo);

                Log.Instance.Debug($"[BlockItemSource.Item] Adding new item(count: {itemsList.Count}), client: {client}, filter: {realtimeQuery.DumpToText()}");
                itemsList.Add(item);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"Exception occurred during processing item\n{rawItemData}", ex);
            }
        }

        private void Cleanup()
        {
            var timeElapsedSinceLastFetch = clock.Now - lastFetchTime;
            if (timeElapsedSinceLastFetch > ClientKeepAliveTimeSpan)
            {
                Log.Instance.Info($"[BlockItemSource.Heartbeat] Client hasn't been queried for the last {timeElapsedSinceLastFetch.TotalSeconds:F0}s, terminating connection... client {client}");
                Dispose();
            }
        }
    }
}