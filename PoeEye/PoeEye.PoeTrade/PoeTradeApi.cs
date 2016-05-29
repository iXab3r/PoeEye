using PoeShared.Communications;

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

    using Properties;

    using ProxyProvider;

    using TypeConverter;

    internal sealed class PoeTradeApi : IPoeApi
    {
        private static readonly string PoeTradeSearchUri = @"http://poe.trade/search";
        private static readonly string PoeTradeUri = @"http://poe.trade";

        private static readonly int MaxSimultaneousRequestsCount;
        private static readonly bool ProxyEnabled;
        private static readonly TimeSpan DelayBetweenRequests;
        private static readonly SemaphoreSlim RequestsSemaphore;

        private readonly IFactory<IHttpClient> httpClientFactory;

        private readonly IPoeTradeParser poeTradeParser;
        private readonly IProxyProvider proxyProvider;
        private readonly IConverter<IPoeQuery, NameValueCollection> queryConverter;

        static PoeTradeApi()
        {
            MaxSimultaneousRequestsCount = Settings.Default.MaxSimultaneousRequestsCount;
            DelayBetweenRequests = Settings.Default.DelayBetweenRequests;
            ProxyEnabled = Settings.Default.ProxyEnabled;
            Log.Instance.Debug($"[PoeTradeApi..staticctor] {new {MaxSimultaneousRequestsCount, DelayBetweenRequests, ProxyEnabled}}");
            RequestsSemaphore = new SemaphoreSlim(MaxSimultaneousRequestsCount);
        }

        public PoeTradeApi(
            IPoeTradeParser poeTradeParser,
            IProxyProvider proxyProvider,
            IFactory<IHttpClient> httpClientFactory,
            IConverter<IPoeQuery, NameValueCollection> queryConverter)
        {
            Guard.ArgumentNotNull(() => poeTradeParser);
            Guard.ArgumentNotNull(() => proxyProvider);
            Guard.ArgumentNotNull(() => httpClientFactory);
            Guard.ArgumentNotNull(() => queryConverter);

            this.poeTradeParser = poeTradeParser;
            this.proxyProvider = proxyProvider;
            this.queryConverter = queryConverter;
            this.httpClientFactory = httpClientFactory;
        }

        public Task<IPoeQueryResult> IssueQuery(IPoeQuery query)
        {
            Guard.ArgumentNotNull(() => query);

            var queryPostData = queryConverter.Convert(query);
            return IssueQuery(PoeTradeSearchUri, queryPostData);
        }

        public Task<IPoeStaticData> RequestStaticData()
        {
            var client = CreateClient();
            return client
                .Get(PoeTradeUri)
                .Select(ThrowIfNotParseable)
                .Select(poeTradeParser.ParseStaticData)
                .ToTask();
        }

        private Task<IPoeQueryResult> IssueQuery(string uri, NameValueCollection queryParameters)
        {
            IProxyToken proxyToken = null;
            try
            {
                var client = CreateClient();
                if (ProxyEnabled && proxyProvider.TryGetProxy(out proxyToken))
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

                try
                {
                    Log.Instance.Trace($"[PoeTradeApi] Awaiting for semaphore slot (max: {MaxSimultaneousRequestsCount}, atm: {RequestsSemaphore.CurrentCount})");
                    RequestsSemaphore.Wait();

                    return client
                        .Post(uri, queryParameters)
                        .Select(ThrowIfNotParseable)
                        .Select(poeTradeParser.ParseQueryResponse)
                        .ToTask();
                }
                finally
                {
                    Log.Instance.Trace($"[PoeTradeApi] Awaiting {DelayBetweenRequests.TotalSeconds}s before releasing semaphore slot...");
                    Thread.Sleep(DelayBetweenRequests);
                    RequestsSemaphore.Release();
                }
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

        private IHttpClient CreateClient()
        {
            var client = httpClientFactory.Create();
            var cookies = new CookieCollection
            {
                new Cookie("interface", "simple", @"/", "poe.trade"),
                new Cookie("theme", "modern", @"/", "poe.trade")
            };
            client.Cookies = cookies;
            return client;
        }

        private string ThrowIfNotParseable(string queryResult)
        {
            if (string.IsNullOrWhiteSpace(queryResult))
            {
                throw new ApplicationException("Malformed query result - empty string");
            }

            if (IsCaptcha(queryResult))
            {
                throw new CaptchaException("CAPTCHA detected, query will not be processed", "http://poe.trade/search");
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