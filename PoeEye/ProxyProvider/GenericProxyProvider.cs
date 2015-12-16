namespace ProxyProvider
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    using ProxiesSource;
    using ProxiesSource.FoxTools;
    using ProxiesSource.RawList;

    public sealed class GenericProxyProvider : IProxyProvider
    {
        public static readonly TimeSpan DefaultRecheckTimeout = TimeSpan.FromMinutes(10);

        private readonly TimeSpan proxiesActualizationPeriod;
        private readonly IProxiesSource[] proxiesSources;

        private readonly Timer proxiesRequestTimer;

        private readonly ConcurrentDictionary<IWebProxy, WebProxyToken> proxiesList = new ConcurrentDictionary<IWebProxy, WebProxyToken>();

        public GenericProxyProvider() : this(new RawProxiesSource())
        {
        }

        public GenericProxyProvider(params IProxiesSource[] proxiesSources) : this(DefaultRecheckTimeout, proxiesSources)
        {
        }

        public GenericProxyProvider(TimeSpan proxiesActualizationPeriod, params IProxiesSource[] proxiesSources)
        {
            this.proxiesActualizationPeriod = proxiesActualizationPeriod;
            this.proxiesSources = proxiesSources;
            if (proxiesSources == null)
            {
                throw new ArgumentNullException(nameof(proxiesSources));
            }

            proxiesRequestTimer = new Timer(state => ActualizeProxiesList());
            proxiesRequestTimer.Change(0, Timeout.Infinite);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TryGetProxy(out IProxyToken proxy)
        {
            proxy = null;

            var activeProxies = proxiesList.Values.Where(x => !x.IsBroken).ToArray();
            if (!activeProxies.Any())
            {
                Log.Instance.Warn($"[GenericProxyProvider.TryGetProxy] Could not find active proxy among {proxiesList.Count} items");
                return false;
            }
            
            var proxyToReturn = activeProxies.PickRandom();
            Log.Instance.Debug($"[GenericProxyProvider.TryGetProxy({activeProxies.Length} / {proxiesList.Count})] Returning proxy {proxyToReturn}");

            proxy = proxyToReturn;
            return true;
        }

        public int ActiveProxiesCount => proxiesList.Count(x => !x.Value.IsBroken);

        public int TotalProxiesCount => proxiesList.Count();

        private void ActualizeProxiesList()
        {
            try
            {
                Log.Instance.Debug($"[GenericProxyProvider] Rechecking proxies list(sourcesCount: {proxiesSources.Length}) ...");
                var proxiesToCheck = proxiesSources
                    .SelectMany(x => x.GetProxies())
                    .Select(x => new WebProxyToken(x))
                    .ToArray();

                Log.Instance.Debug($"[GenericProxyProvider] Proxies to check: \r\n\t{string.Join<object>("\r\n\t", proxiesToCheck)}");

                Parallel.ForEach(proxiesToCheck, new ParallelOptions() { MaxDegreeOfParallelism = 4}, (proxy,_,__) => CheckProxySafe(proxy));

                Log.Instance.Debug($"[GenericProxyProvider] Proxies checked, active: {proxiesList.Count(x => !x.Value.IsBroken)} / {proxiesList.Count}\r\n\tActive proxies:\r\n\t{string.Join<object>("\r\n\t", proxiesList.Values.Where(x => !x.IsBroken))}");
            }
            finally
            {
                proxiesRequestTimer.Change((int)proxiesActualizationPeriod.TotalMilliseconds, Timeout.Infinite);
            }
        }

        private void CheckProxySafe(WebProxyToken proxyToken)
        {
            try
            {
                if (!proxiesList.ContainsKey(proxyToken.Proxy))
                {
                    proxiesList[proxyToken.Proxy] = proxyToken;
                }

                var client = new WebClient { Proxy = proxyToken.Proxy };

                var response = client.DownloadString(@"http://poe.trade/");

                if (response.Contains("Path of Exile shops indexer"))
                {
                    proxyToken.ReportSuccess();
                    Log.Instance.Debug($"[GenericProxyProvider.CheckProxySafe] Proxy is active, {proxyToken.Proxy}");
                }
                else
                {
                    proxyToken.ReportBroken();
                    Log.Instance.Debug($"[GenericProxyProvider.CheckProxySafe] Failed to get expected result from proxy {proxyToken.Proxy}");
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Debug($"[GenericProxyProvider.CheckProxySafe] Proxy failed({proxyToken.Proxy}),  msg '{ex.Message}'");
                proxyToken.ReportBroken();
            }
        }

        private sealed class WebProxyToken : IProxyToken
        {
            public WebProxyToken(IWebProxy proxy)
            {
                Proxy = proxy;
            }

            public IWebProxy Proxy { get; }

            public bool IsBroken { get; private set; }

            public void ReportBroken()
            {
                IsBroken = true;
            }

            public void ReportSuccess()
            {
                IsBroken = false;
            }

            public override string ToString()
            {
                return $"[WebProxyToken] Proxy: {Proxy}, IsBroken: {IsBroken}";
            }
        }

    }
}