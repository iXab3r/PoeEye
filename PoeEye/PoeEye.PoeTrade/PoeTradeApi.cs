using System.Reactive.Subjects;
using Microsoft.Practices.Unity.Configuration.ConfigurationHelpers;
using PoeEye.PoeTrade.Modularity;
using PoeShared.Communications;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.PoeTrade
{
    using System;
    using System.Collections.Specialized;
    using System.Net;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading;
    using System.Threading.Tasks;

    using Guards;

    using PoeShared;
    using PoeShared.Exceptions;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;
    using PoeShared.Prism;

    using ProxyProvider;

    using TypeConverter;

    internal sealed class PoeTradeApi : DisposableReactiveObject, IPoeApi
    {
        private static readonly string PoeTradeSearchUri = @"http://poe.trade/search";
        private static readonly string PoeTradeUri = @"http://poe.trade";

        private readonly SemaphoreSlim requestsSemaphore;

        private readonly IFactory<IHttpClient> httpClientFactory;

        private readonly IPoeTradeParser poeTradeParser;
        private readonly IProxyProvider proxyProvider;
        private readonly IConverter<IPoeQuery, NameValueCollection> queryConverter;
        private readonly IConverter<IPoeQueryInfo, IPoeQuery> queryInfoToQueryConverter;

        private PoeTradeConfig config = new PoeTradeConfig();
        
        public PoeTradeApi(
            IPoeTradeParser poeTradeParser,
            IProxyProvider proxyProvider,
            IFactory<IHttpClient> httpClientFactory,
            IConverter<IPoeQueryInfo, IPoeQuery> queryInfoToQueryConverter,
            IConverter<IPoeQuery, NameValueCollection> queryConverter,
            IConfigProvider<PoeTradeConfig> configProvider)
        {
            Guard.ArgumentNotNull(() => poeTradeParser);
            Guard.ArgumentNotNull(() => proxyProvider);
            Guard.ArgumentNotNull(() => httpClientFactory);
            Guard.ArgumentNotNull(() => queryInfoToQueryConverter);
            Guard.ArgumentNotNull(() => queryConverter);
            Guard.ArgumentNotNull(() => configProvider);

            this.poeTradeParser = poeTradeParser;
            this.proxyProvider = proxyProvider;
            this.queryConverter = queryConverter;
            this.httpClientFactory = httpClientFactory;
            this.queryInfoToQueryConverter = queryInfoToQueryConverter;

            configProvider
                .WhenAnyValue(x => x.ActualConfig)
                .Subscribe(x => config = x)
                .AddTo(Anchors);
            Log.Instance.Debug($"[PoeTradeApi..ctor] {config.DumpToText()}");
            requestsSemaphore = new SemaphoreSlim(config.MaxSimultaneousRequestsCount);
        }

        public Guid Id { get; } = Guid.Parse("8BC98570-F07A-4925-B8A4-EC0BAAF2222C");

        public string Name { get; } = "poe.trade";

        public async Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo queryInfo)
        {
            Guard.ArgumentNotNull(() => queryInfo);

            var query = queryInfoToQueryConverter.Convert(queryInfo);
            var queryPostData = queryConverter.Convert(query);
            return await IssueQuery(PoeTradeSearchUri, queryPostData);
        }

        public Task<IPoeStaticData> RequestStaticData()
        {
            var client = CreateClientWithoutProxy();
            return client
                .Get(PoeTradeUri)
                .Select(ThrowIfNotParseable)
                .Select(poeTradeParser.ParseStaticData)
                .ToTask();
        }

        private async Task<IPoeQueryResult> IssueQuery(string uri, NameValueCollection queryParameters)
        {
            IProxyToken proxyToken = null;
            try
            {
                var client = CreateClient(out proxyToken);

                Log.Instance.Debug($"[PoeTradeApi] Awaiting for semaphore slot (max: {config.MaxSimultaneousRequestsCount}, atm: {requestsSemaphore.CurrentCount})");
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

        private IHttpClient CreateClientWithoutProxy()
        {
            IProxyToken proxyToken;
            return CreateClient(out proxyToken);
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

            if (config.ProxyEnabled && proxyProvider.TryGetProxy(out proxyToken))
            {
                Log.Instance.Debug($"[PoeTradeApi] Got proxy {proxyToken} from proxy provider {proxyProvider}");
                client.Proxy = proxyToken.Proxy;
            }
            else
            {
                var systemProxy = WebRequest.DefaultWebProxy;
                Log.Instance.Debug($"[PoeTradeApi] Using default system web proxy: {systemProxy}");
                client.Proxy = systemProxy;
            }

            return client;
        }

        private void ReleaseSemaphore()
        {
            Log.Instance.Debug($"[PoeTradeApi] Awaiting {config.DelayBetweenRequests.TotalSeconds}s before releasing semaphore slot...");
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

        public void DisposeQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(() => query);
        }
    }
}