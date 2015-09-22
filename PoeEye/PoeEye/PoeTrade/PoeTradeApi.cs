using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reactive.Linq;
using CsQuery;
using EasyHttp.Http;
using EasyHttp.Infrastructure;
using PoeShared;
using SmartFormat;

namespace PoeEye.PoeTrade
{
    internal sealed class PoeTradeApi : IPoeApi
    {
        private static readonly string PoeTradeUri = @"http://poe.trade/search";
            
        public IObservable<IPoeSearchResult> IssueQuery(IPoeSearchQuery query)
        {
            var client = new HttpClient();

            var queryPostData = new Dictionary<string, object>()
            {
                { "league", "Warbands" },
                { "name", "Temple map" },
            };

            client.Request.Cookies = new CookieCollection
            {
                new Cookie("interface", "simple", @"/", "poe.trade"),
                new Cookie("theme", "ancient-white", @"/", "poe.trade"),
            };
            var response = client.Post(PoeTradeUri, queryPostData, "application/x-www-form-urlencoded");

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return Observable.Throw<IPoeSearchResult>(new HttpException(response.StatusCode, "Wrong status code, expected 200 OK, got {0}".FormatSmart(response.StatusCode)));
            }

            var rawResponse = response.RawText;

            var parser = new CQ(new StringReader(rawResponse));

            return Observable.Empty<IPoeSearchResult>();
        }
    }
}