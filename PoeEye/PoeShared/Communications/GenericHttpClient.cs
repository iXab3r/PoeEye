using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Web;
using CsQuery.ExtensionMethods.Internal;
using Guards;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using TypeConverter;

namespace PoeShared.Communications
{
    internal sealed class GenericHttpClient : IHttpClient
    {
        private readonly IConverter<NameValueCollection, string> nameValueConverter;

        public GenericHttpClient(
            [NotNull] IConverter<NameValueCollection, string> nameValueConverter)
        {
            Guard.ArgumentNotNull(() => nameValueConverter);

            this.nameValueConverter = nameValueConverter;
        }

        public CookieCollection Cookies { get; set; } = new CookieCollection();

        public IWebProxy Proxy { get; set; }

        public IObservable<Stream> GetStreamAsync(Uri requestUri)
        {
            Guard.ArgumentNotNull(() => requestUri);

            var httpClient = new HttpClient();
            return httpClient.GetStreamAsync(requestUri).ToObservable();
        }

        public IObservable<string> Post(string uri, NameValueCollection args)
        {
            Guard.ArgumentNotNullOrEmpty(() => uri);
            Guard.ArgumentNotNull(() => args);

            return Observable.Start(() => PostQueryInternal(uri, args), Scheduler.Default);
        }

        public IObservable<string> Get(string uri)
        {
            Guard.ArgumentNotNullOrEmpty(() => uri);

            return Observable.Start(() => GetInternal(uri), Scheduler.Default);
        }

        private string GetInternal(string uri)
        {
            Log.Instance.Debug($"[HttpClient] Querying uri '{uri}' (GET)");

            var httpClient = WebRequest.CreateHttp(uri);
            httpClient.CookieContainer = new CookieContainer();
            if (Cookies != null)
            {
                httpClient.CookieContainer.Add(Cookies);
            }
            httpClient.Method = WebRequestMethods.Http.Get;


            var rawResponse = IssueRequest(httpClient, string.Empty);

            return rawResponse;
        }

        private string PostQueryInternal(string uri, NameValueCollection args)
        {
            var postData = nameValueConverter.Convert(args);
            Log.Instance.Debug($"[HttpClient] Querying uri '{uri}', args: \r\nPOST: {postData}");
            Log.Instance.Trace($"[HttpClient] Splitted POST data dump: {postData.SplitClean('&').DumpToText()}");

            var httpClient = WebRequest.CreateHttp(uri);
            httpClient.CookieContainer = new CookieContainer();
            if (Cookies != null)
            {
                httpClient.CookieContainer.Add(Cookies);
            }
            httpClient.ContentType = "application/x-www-form-urlencoded";
            httpClient.Method = WebRequestMethods.Http.Post;

            var rawResponse = IssueRequest(httpClient, postData);

            return rawResponse;
        }

        private string IssueRequest(HttpWebRequest httpClient, string requestData)
        {
            var proxy = Proxy;
            if (proxy != null)
            {
                Log.Instance.Debug($"[HttpClient] Using proxy {proxy} for uri '{httpClient.RequestUri}'");
                httpClient.Proxy = proxy;
            }

            if (httpClient.Method == WebRequestMethods.Http.Post && !string.IsNullOrEmpty(requestData))
            {
                Log.Instance.Debug($"[HttpClient] Preparing POST data...");
                var data = Encoding.ASCII.GetBytes(requestData);
                using (var stream = httpClient.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }

            var response = (HttpWebResponse) httpClient.GetResponse();
            var responseStream = response.GetResponseStream();

            var rawResponse = string.Empty;
            if (responseStream != null)
            {
                rawResponse = new StreamReader(responseStream).ReadToEnd();
            }

            Log.Instance.Debug(
                $"[HttpClient] Received response, status: {response.StatusCode}, length: {rawResponse?.Length}");

            CheckResponseStatusOrThrow(response);

            return rawResponse;
        }

        private static void CheckResponseStatusOrThrow(HttpWebResponse response)
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            throw new HttpException((int) response.StatusCode, $"Wrong status code, expected 200 OK, got {response.StatusCode}");
        }
    }
}