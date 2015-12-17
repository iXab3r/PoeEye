namespace PoeEye.Communications
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Text;
    using System.Web;

    using CsQuery.ExtensionMethods.Internal;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.DumpToText;
    using PoeShared.Http;

    using TypeConverter;

    using HttpClient = System.Net.Http.HttpClient;
    using Log = PoeShared.Log;

    internal sealed class GenericHttpClient : IHttpClient
    {
        private readonly IConverter<NameValueCollection, string> nameValueConverter;
        private readonly IWebProxy proxy;

        public GenericHttpClient(
            [NotNull] IConverter<NameValueCollection, string> nameValueConverter,
            [CanBeNull] IWebProxy proxy)
        {
            Guard.ArgumentNotNull(() => nameValueConverter);

            this.nameValueConverter = nameValueConverter;
            this.proxy = proxy;
        }

        public CookieCollection Cookies { get; set; }

        public IObservable<Stream> GetStreamAsync(Uri requestUri)
        {
            Guard.ArgumentNotNull(() => requestUri);

            var httpClient = new HttpClient();
            return httpClient.GetStreamAsync(requestUri).ToObservable();
        }

        public IObservable<string> PostQuery(string uri, NameValueCollection args)
        {
            Guard.ArgumentNotNullOrEmpty(() => uri);
            Guard.ArgumentNotNull(() => args);

            return Observable.Start(() => PostQueryInternal(uri, args), Scheduler.Default);
        }

        private string PostQueryInternal(string uri, NameValueCollection args)
        {
            var postData = nameValueConverter.Convert(args);
            Log.Instance.Debug($"[HttpClient] Querying uri '{uri}', args: \r\nPOST: {postData}");
            Log.Instance.Trace($"[HttpClient] Splitted POST data dump: {postData.SplitClean('&').DumpToTextValue()}");

            var httpClient = WebRequest.CreateHttp(uri);
            httpClient.CookieContainer = new CookieContainer();
            httpClient.CookieContainer.Add(Cookies);
            httpClient.ContentType = "application/x-www-form-urlencoded";
            httpClient.Method = WebRequestMethods.Http.Post;

            if (proxy != null)
            {
                Log.Instance.Debug($"[HttpClient] Using proxy {proxy} for uri '{uri}'");
                httpClient.Proxy = proxy;
            }

            var data = Encoding.ASCII.GetBytes(postData);
            using (var stream = httpClient.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)httpClient.GetResponse();
            var responseStream = response.GetResponseStream();

            string rawResponse = string.Empty;
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

            throw new HttpException((int)response.StatusCode, $"Wrong status code, expected 200 OK, got {response.StatusCode}");
        }
    }
}