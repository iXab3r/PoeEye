using System;
using System.Collections.Specialized;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using DynamicData;
using DynamicData.Alias;
using Guards;
using JetBrains.Annotations;
using PoeEye.PoeTrade.Modularity;
using PoeEye.PoeTrade.TradeApi;
using PoeShared.Communications;
using PoeShared.Exceptions;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ProxyProvider;
using TypeConverter;

namespace PoeEye.PoeTrade
{
    internal sealed class PoeTradeApi : DisposableReactiveObject, IPoeApi
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeTradeApi));

        private static readonly string PoeTradeSearchUri = @"http://poe.trade/search";

        private readonly IFactory<PoeTradeHeadlessApi> headlessApiFactory;

        private readonly IFactory<IHttpClient> httpClientFactory;
        private readonly IPoeTradeParser poeTradeParser;
        private readonly IProxyProvider proxyProvider;
        private readonly IConverter<IPoeQuery, NameValueCollection> queryConverter;
        private readonly IConverter<IPoeQueryInfo, IPoeQuery> queryInfoToQueryConverter;
        private readonly IFactory<IPoeTradeLiveAdapter, IPoeQueryResult> liveSourceFactory;

        private readonly SemaphoreSlim requestsSemaphore;

        private PoeTradeConfig config = new PoeTradeConfig();

        public PoeTradeApi(
            [NotNull] IFactory<PoeTradeHeadlessApi> headlessApiFactory,
            [NotNull] IPoeTradeParser poeTradeParser,
            [NotNull] IProxyProvider proxyProvider,
            [NotNull] IFactory<IHttpClient> httpClientFactory,
            [NotNull] IConverter<IPoeQueryInfo, IPoeQuery> queryInfoToQueryConverter,
            [NotNull] IFactory<IPoeTradeLiveAdapter, IPoeQueryResult> liveSourceFactory,
            [NotNull] IConverter<IPoeQuery, NameValueCollection> queryConverter,
            [NotNull] IConfigProvider<PoeTradeConfig> configProvider)
        {
            Guard.ArgumentNotNull(headlessApiFactory, nameof(headlessApiFactory));
            Guard.ArgumentNotNull(poeTradeParser, nameof(poeTradeParser));
            Guard.ArgumentNotNull(proxyProvider, nameof(proxyProvider));
            Guard.ArgumentNotNull(httpClientFactory, nameof(httpClientFactory));
            Guard.ArgumentNotNull(queryInfoToQueryConverter, nameof(queryInfoToQueryConverter));
            Guard.ArgumentNotNull(queryConverter, nameof(queryConverter));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(liveSourceFactory, nameof(liveSourceFactory));

            this.headlessApiFactory = headlessApiFactory;
            this.poeTradeParser = poeTradeParser;
            this.proxyProvider = proxyProvider;
            this.queryConverter = queryConverter;
            this.httpClientFactory = httpClientFactory;
            this.queryInfoToQueryConverter = queryInfoToQueryConverter;
            this.liveSourceFactory = liveSourceFactory;

            configProvider
                .WhenChanged
                .Subscribe(x => config = x)
                .AddTo(Anchors);
            Log.Debug($"Using config {config.DumpToTextRaw()}");
            requestsSemaphore = new SemaphoreSlim(config.MaxSimultaneousRequestsCount);
        }

        public Guid Id { get; } = Guid.Parse("8BC98570-F07A-4925-B8A4-EC0BAAF2222C");

        public string Name { get; } = "poe.trade";

        public bool IsAvailable { get; } = true;

        public IObservable<IPoeQueryResult> SubscribeToLiveUpdates(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));

            return IssueQuery(query)
                   .ToObservable()
                   .Select(queryResult =>
                   {
                       Log.Debug($"Initializing live subscription for query {queryResult.Id}");
                       return Observable.Using(() => liveSourceFactory.Create(queryResult), api => api.Updates);
                   })
                   .Switch();
        }

        public async Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo queryInfo)
        {
            Guard.ArgumentNotNull(queryInfo, nameof(queryInfo));

            var query = queryInfoToQueryConverter.Convert(queryInfo);
            var queryPostData = queryConverter.Convert(query);
            var rawData = await IssueQuery(PoeTradeSearchUri, queryPostData);
            return new PoeQueryResult(rawData)
            {
                Query = queryInfo,
                ConvertedQuery = queryPostData.DumpToTextRaw(),
            };
        }

        public Task<IPoeStaticData> RequestStaticData()
        {
            return headlessApiFactory.Create().RequestStaticData();
        }

        public void DisposeQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));
        }

        private async Task<IPoeQueryResult> IssueQuery(string uri, NameValueCollection queryParameters)
        {
            IProxyToken proxyToken = null;
            try
            {
                var client = CreateClient(out proxyToken);

                Log.Debug(
                    $"Awaiting for semaphore slot (max: {config.MaxSimultaneousRequestsCount}, atm: {requestsSemaphore.CurrentCount})");
                await requestsSemaphore.WaitAsync();

                return await client
                             .Post(uri, queryParameters)
                             .Select(ThrowIfNotParseable)
                             .Select(poeTradeParser.ParseQueryResponse)
                             .Finally(ReleaseSemaphore);
            }
            catch (WebException ex)
            {
                if (proxyToken == null)
                {
                    throw;
                }

                proxyToken.ReportBroken();
                throw new WebException($"{ex.Message} (Proxy {proxyToken.Proxy})", ex);
            }
        }

        private IHttpClient CreateClient(out IProxyToken proxyToken)
        {
            proxyToken = null;
            var client = httpClientFactory.Create();

            var cookies = new CookieCollection
            {
                new Cookie("interface", "simple", @"/", "poe.trade"),
                new Cookie("theme", "modern", @"/", "poe.trade")
            };
            client.Cookies = cookies;
            client.Timeout = config.RequestTimeout;

            if (config.ProxyEnabled && proxyProvider.TryGetProxy(out proxyToken))
            {
                Log.Debug($"Got proxy {proxyToken} from proxy provider {proxyProvider}");
                client.Proxy = proxyToken.Proxy;
            }
            else
            {
                var systemProxy = WebRequest.DefaultWebProxy;
                Log.Debug($"Using default system web proxy: {systemProxy}");
                client.Proxy = systemProxy;
            }

            return client;
        }

        private void ReleaseSemaphore()
        {
            Log.Debug($"Awaiting {config.DelayBetweenRequests.TotalSeconds}s before releasing semaphore slot...");
            Thread.Sleep(config.DelayBetweenRequests);
            requestsSemaphore.Release();
        }

        private string ThrowIfNotParseable(string queryResult)
        {
            if (string.IsNullOrWhiteSpace(queryResult))
            {
                throw new ApplicationException("Malformed query result - empty string");
            }

            if (IsCaptcha(queryResult))
            {
                throw new CaptchaException("CAPTCHA detected, query will not be processed", PoeTradeSearchUri);
            }

            return queryResult;
        }

        private bool IsCaptcha(string queryResult)
        {
            if (string.IsNullOrWhiteSpace(queryResult))
            {
                return false;
            }

            return queryResult.Contains("Enter captcha and press the button");
        }
    }
}