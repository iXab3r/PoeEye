namespace PoeEye.PoeTrade
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Reactive.Linq;

    using Factory;

    using PoeShared;
    using PoeShared.Http;
    using PoeShared.Query;

    using TypeConverter;

    using Guard = Guards.Guard;

    internal sealed class PoeTradeApi : IPoeApi
    {
        private static readonly string PoeTradeUri = @"http://poe.trade/search";

        private readonly IFactory<IHttpClient> httpClientFactory;
        private readonly IConverter<IPoeQuery, IDictionary<string, object>> queryConverter;

        private readonly IPoeTradeParser poeTradeParser;

        public PoeTradeApi(
            IPoeTradeParser poeTradeParser,
            IFactory<IHttpClient> httpClientFactory,
            IConverter<IPoeQuery, IDictionary<string, object>> queryConverter)
        {
            Guard.ArgumentNotNull(() => poeTradeParser);
            Guard.ArgumentNotNull(() => httpClientFactory);
            Guard.ArgumentNotNull(() => queryConverter);
            
            this.poeTradeParser = poeTradeParser;
            this.httpClientFactory = httpClientFactory;
            this.queryConverter = queryConverter;
        }

        public IObservable<IPoeQueryResult> IssueQuery(IPoeQuery query)
        {
            Guard.ArgumentNotNull(() => query);
            
            var client = CreateClient();

            var queryPostData = queryConverter.Convert(query);

            return client
                .PostQuery(PoeTradeUri, queryPostData)
                .Select(poeTradeParser.ParseQueryResult);
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
    }
}