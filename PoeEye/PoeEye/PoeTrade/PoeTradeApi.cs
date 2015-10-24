namespace PoeEye.PoeTrade
{
    using System;
    using System.Collections.Specialized;
    using System.Net;
    using System.Reactive.Linq;

    using Factory;

    using Guards;

    using PoeShared.Http;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    using TypeConverter;

    internal sealed class PoeTradeApi : IPoeApi
    {
        private static readonly string PoeTradeUri = @"http://poe.trade/search";

        private readonly IFactory<IHttpClient> httpClientFactory;

        private readonly IPoeTradeParser poeTradeParser;
        private readonly IConverter<IPoeQuery, NameValueCollection> queryConverter;

        public PoeTradeApi(
            IPoeTradeParser poeTradeParser,
            IFactory<IHttpClient> httpClientFactory,
            IConverter<IPoeQuery, NameValueCollection> queryConverter)
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
                .Select(poeTradeParser.Parse);
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