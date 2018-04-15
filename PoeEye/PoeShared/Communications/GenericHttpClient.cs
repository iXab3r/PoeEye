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
        private WebHeaderCollection customHeaders = new WebHeaderCollection();
        private CookieCollection cookies = new CookieCollection();
        private static readonly string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";

        public GenericHttpClient(
            [NotNull] IConverter<NameValueCollection, string> nameValueConverter)
        {
            Guard.ArgumentNotNull(nameValueConverter, nameof(nameValueConverter));

            this.nameValueConverter = nameValueConverter;
        }

        public CookieCollection Cookies
        {
            get { return cookies; }
            set { cookies = value ?? new CookieCollection(); }
        }

        public TimeSpan? Timeout { get; set; }

        public WebHeaderCollection CustomHeaders
        {
            get { return customHeaders; }
            set { customHeaders = value ?? new WebHeaderCollection(); }
        }

        public string Referer { get; set; }

        public string UserAgent { get; set; } = DefaultUserAgent;

        public IWebProxy Proxy { get; set; }

        public IObservable<Stream> GetStreamAsync(Uri requestUri)
        {
            Guard.ArgumentNotNull(requestUri, nameof(requestUri));

            var httpClient = new HttpClient();
            return httpClient.GetStreamAsync(requestUri).ToObservable();
        }

        public IObservable<string> Post(string uri, NameValueCollection args)
        {
            Guard.ArgumentNotNullOrEmpty(() => uri);
            Guard.ArgumentNotNull(args, nameof(args));

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
            httpClient.CookieContainer.Add(Cookies);
            httpClient.Headers.Add(CustomHeaders);
            httpClient.Referer = Referer;
            httpClient.UserAgent = UserAgent;
            if (Timeout != null)
            {
                httpClient.Timeout = (int)Timeout.Value.TotalMilliseconds;
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
            httpClient.CookieContainer.Add(Cookies);
            httpClient.Headers.Add(CustomHeaders);
            httpClient.Referer = Referer;
            httpClient.UserAgent = UserAgent;
            httpClient.AllowAutoRedirect = true;
            if (Timeout != null)
            {
                httpClient.Timeout = (int)Timeout.Value.TotalMilliseconds;
            }
            httpClient.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            httpClient.ContentType = "application/x-www-form-urlencoded";
            httpClient.Method = WebRequestMethods.Http.Post;

            var rawResponse = IssueRequest(httpClient, postData);

            return rawResponse;
        }

        private string IssueRequest(HttpWebRequest httpRequest, string requestData)
        {
            var proxy = Proxy;
            if (proxy != null)
            {
                Log.Instance.Debug($"[HttpClient] Using proxy {proxy} for uri '{httpRequest.RequestUri}'");
                httpRequest.Proxy = proxy;
            }

            if (httpRequest.Method == WebRequestMethods.Http.Post && !string.IsNullOrEmpty(requestData))
            {
                Log.Instance.Debug($"[HttpClient] Preparing POST data...");
                var data = Encoding.ASCII.GetBytes(requestData);
                using (var stream = httpRequest.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            
            Log.Instance.Debug($"[HttpClient] Sending {httpRequest.Method} request with timeout of {httpRequest.Timeout}ms to {httpRequest.RequestUri}");
            using (var response = (HttpWebResponse)httpRequest.GetResponse())
            using (var responseStream = response.GetResponseStream())
            {
                var rawResponse = string.Empty;

                if (responseStream != null)
                {
                    var rawBytes = responseStream.ReadToEnd();
                    Log.Instance.Debug($"[HttpClient] Received response, status: {response.StatusCode}, binary length: {rawBytes}");
                    rawResponse = Encoding.UTF8.GetString(rawBytes);
                    Log.Instance.Debug($"[HttpClient] Resulting response(string) length: {rawResponse.Length}");
                }
                else
                {
                    Log.Instance.Warn($"[HttpClient] Received null response stream ! Status: {response.StatusCode}");
                }


                CheckResponseStatusOrThrow(response);

                return rawResponse;
            }
        }

        private static void CheckResponseStatusOrThrow(HttpWebResponse response)
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            throw new HttpException((int)response.StatusCode, $"Wrong status code, expected 200 OK, got {response.StatusCode}");
        }
    }
}
