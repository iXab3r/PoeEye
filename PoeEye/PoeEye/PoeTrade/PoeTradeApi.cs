namespace PoeEye.PoeTrade
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reactive.Linq;

    using EasyHttp.Http;
    using EasyHttp.Infrastructure;

    using JetBrains.Annotations;

    using PoeShared;

    using SmartFormat;

    internal sealed class PoeTradeApi : IPoeApi
    {

        private static readonly string PoeTradeUri = @"http://poe.trade/search";

        private readonly IPoeTradeParser poeTradeParser;

        public PoeTradeApi([NotNull] IPoeTradeParser poeTradeParser)
        {
            if (poeTradeParser == null)
            {
                throw new ArgumentNullException(nameof(poeTradeParser));
            }
            this.poeTradeParser = poeTradeParser;
        }

        public IObservable<IPoeSearchResult> IssueQuery(IPoeSearchQuery query)
        {
            var client = CreateClient();

            var queryPostData = new Dictionary<string, object>
            {
                {"league", "Warbands"},
                {"name", "Temple map"}
            };

            var response = client.Post(PoeTradeUri, queryPostData, HttpContentTypes.ApplicationXWwwFormUrlEncoded);
            CheckResponseStatusOrThrow(response);

            var rawResponse = response.RawText;
            var results = poeTradeParser.ParseQueryResult(rawResponse);

            Log.Instance.DebugFormat("Known currencies:\r\n\t{0}", 
                string.Join("\r\n\t", results.CurrenciesList.Select(x => x.ToString())));
            Log.Instance.DebugFormat("Known mods:\r\n\t{0}",
                string.Join("\r\n\t", results.ModsList.Select(x => x.ToString())));
            Log.Instance.DebugFormat("Items list:\r\n\t{0}",
               string.Join("\r\n\t", results.ItemsList.Select(x => x.ToString())));

            Log.Instance.DebugFormat("Buyout list:\r\n\t{0}",
               string.Join("\r\n\t", results.ItemsList.Where(x => !string.IsNullOrWhiteSpace(x.Price)).Select(x => x.ToString())));

            var item = results.ItemsList.First(x => !string.IsNullOrWhiteSpace(x.Price) && x.Mods.Any());
            Log.Instance.DebugFormat("First item:\r\n\t{0}\r\n\tMods:\r\n\t{1}", item, string.Join("\r\n\t", item.Mods.Select(x => x.ToString())));


            return Observable.Empty<IPoeSearchResult>();
        }

        private HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.Request.Cookies = new CookieCollection
            {
                new Cookie("interface", "simple", @"/", "poe.trade"),
                new Cookie("theme", "modern", @"/", "poe.trade")
            };

            return client;
        }

        public string[] ExtractCurrenciesList()
        {
            var client = CreateClient();

            var response = client.Get(PoeTradeUri);
            CheckResponseStatusOrThrow(response);

            var rawResponse = response.RawText;

            return null;
        }

        private void CheckResponseStatusOrThrow(HttpResponse response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpException(response.StatusCode,
                    "Wrong status code, expected 200 OK, got {0}".FormatSmart(response.StatusCode));
            }
        }
    }
}