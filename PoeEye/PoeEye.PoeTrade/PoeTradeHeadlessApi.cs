﻿using System.Reactive.Subjects;
using Microsoft.Practices.Unity.Configuration.ConfigurationHelpers;
using PoeEye.PoeTrade.Modularity;
using PoeShared.Communications;
using PoeShared.Communications.Chromium;
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

    internal sealed class PoeTradeHeadlessApi : DisposableReactiveObject, IPoeApi
    {
        private static readonly string PoeTradeSearchUri = @"http://poe.trade/search";
        private static readonly string PoeTradeUri = @"http://poe.trade";

        private readonly SemaphoreSlim requestsSemaphore;

        private readonly IChromiumBootstrapper httpClientFactory;

        private readonly IPoeTradeParser poeTradeParser;
        private readonly IProxyProvider proxyProvider;
        private readonly IConverter<IPoeQuery, NameValueCollection> queryConverter;
        private readonly IConverter<IPoeQueryInfo, IPoeQuery> queryInfoToQueryConverter;

        private PoeTradeConfig config = new PoeTradeConfig();
        
        public PoeTradeHeadlessApi(
            IPoeTradeParser poeTradeParser,
            IProxyProvider proxyProvider,
            IChromiumBootstrapper httpClientFactory,
            IConverter<IPoeQueryInfo, IPoeQuery> queryInfoToQueryConverter,
            IConverter<IPoeQuery, NameValueCollection> queryConverter,
            IConfigProvider<PoeTradeConfig> configProvider)
        {
            Guard.ArgumentNotNull(poeTradeParser, nameof(poeTradeParser));
            Guard.ArgumentNotNull(proxyProvider, nameof(proxyProvider));
            Guard.ArgumentNotNull(httpClientFactory, nameof(httpClientFactory));
            Guard.ArgumentNotNull(queryInfoToQueryConverter, nameof(queryInfoToQueryConverter));
            Guard.ArgumentNotNull(queryConverter, nameof(queryConverter));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));

            this.poeTradeParser = poeTradeParser;
            this.proxyProvider = proxyProvider;
            this.queryConverter = queryConverter;
            this.httpClientFactory = httpClientFactory;
            this.queryInfoToQueryConverter = queryInfoToQueryConverter;

            configProvider
                .WhenChanged
                .Subscribe(x => config = x)
                .AddTo(Anchors);
            Log.Instance.Debug($"[PoeTradeHeadlessApi..ctor] {config.DumpToText()}");
            requestsSemaphore = new SemaphoreSlim(config.MaxSimultaneousRequestsCount);
        }

        public Guid Id { get; } = Guid.Parse("1F00C934-F739-4756-81E6-F2A9FA7BA44E");

        public string Name { get; } = "poe.trade Headless";

        public bool IsAvailable { get; } = true;

        public async Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo queryInfo)
        {
            Guard.ArgumentNotNull(queryInfo, nameof(queryInfo));

            var query = queryInfoToQueryConverter.Convert(queryInfo);
            var queryPostData = queryConverter.Convert(query);
            return await IssueQuery(PoeTradeSearchUri, queryPostData);
        }

        public Task<IPoeStaticData> RequestStaticData()
        {
            var client = CreateClientWithoutProxy();
            return client
                .Get(PoeTradeUri)
                .ToObservable()
                .Select(x => client.GetSource().Result)
                .Select(ThrowIfNotParseable)
                .Select(poeTradeParser.ParseStaticData)
                .Finally(() => client.Dispose())
                .ToTask();
        }

        private async Task<IPoeQueryResult> IssueQuery(string uri, NameValueCollection queryParameters)
        {
            IProxyToken proxyToken = null;
            try
            {
                var client = CreateClient(out proxyToken);

                Log.Instance.Debug($"[PoeTradeHeadlessApi] Awaiting for semaphore slot (max: {config.MaxSimultaneousRequestsCount}, atm: {requestsSemaphore.CurrentCount})");
                await requestsSemaphore.WaitAsync();

                return await client
                    .Post(uri, queryParameters)
                    .ToObservable()
                    .Select(x => client.GetSource().Result)
                    .Select(ThrowIfNotParseable)
                    .Select(poeTradeParser.ParseQueryResponse)
                    .Finally(ReleaseSemaphore)
                    .Finally(() => client.Dispose());
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

        private IPoeBrowser CreateClientWithoutProxy()
        {
            IProxyToken proxyToken;
            return CreateClient(out proxyToken);
        }

        private IPoeBrowser CreateClient(out IProxyToken proxyToken)
        {
            proxyToken = null;
            var client = httpClientFactory.CreateBrowser();

            var cookies = new CookieCollection
            {
                new Cookie("interface", "simple", @"/", "poe.trade"),
                new Cookie("theme", "modern", @"/", "poe.trade")
            };

            return client;
        }

        private void ReleaseSemaphore()
        {
            Log.Instance.Debug($"[PoeTradeHeadlessApi] Awaiting {config.DelayBetweenRequests.TotalSeconds}s before releasing semaphore slot...");
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
            Guard.ArgumentNotNull(query, nameof(query));
        }
    }
}