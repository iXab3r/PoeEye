namespace PoeEye.Communications
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Reactive.Threading.Tasks;
    using System.Threading.Tasks;

    using DumpToText;

    using EasyHttp.Http;
    using EasyHttp.Infrastructure;

    using Guards;

    using PoeShared.Http;

    internal sealed class GenericHttpClient : IHttpClient
    {
        public CookieCollection Cookies { get; set; }

        public IObservable<string> PostQuery(string uri, IDictionary<string, object> args)
        {
            Guard.ArgumentNotNullOrEmpty(() => uri);
            Guard.ArgumentNotNull(() => args);
            
            return Task
                .Run(() => PostQueryInternal(uri, args))
                .ToObservable();
        }

        private string PostQueryInternal(string uri, IDictionary<string, object> args)
        {
            var httpClient = new HttpClient();
            httpClient.Request.Cookies = Cookies;

            Log.Instance.Debug($"[HttpClient] Querying uri '{uri}', args: \r\n{args.DumpToTextValue()}...");
            var response = httpClient.Post(uri, args, HttpContentTypes.ApplicationXWwwFormUrlEncoded);
            Log.Instance.Debug($"[HttpClient] Received response, status: {response.StatusCode}, length: {response.RawText?.Length}");

            CheckResponseStatusOrThrow(response);

            return response.RawText;
        }

        private static void CheckResponseStatusOrThrow(HttpResponse response)
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            throw new HttpException(response.StatusCode, $"Wrong status code, expected 200 OK, got {response.StatusCode}");
        }
    }
}