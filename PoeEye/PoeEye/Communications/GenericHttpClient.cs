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
    using System.Threading;
    using System.Web;

    using CsQuery.ExtensionMethods.Internal;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.DumpToText;
    using PoeShared.Http;

    using Properties;

    using ProxyProvider;

    using TypeConverter;

    using HttpClient = System.Net.Http.HttpClient;
    using Log = PoeShared.Log;

    internal sealed class GenericHttpClient : IHttpClient
    {
        private readonly IConverter<NameValueCollection, string> nameValueConverter;
        private readonly IProxyProvider proxyProvider;
        private static readonly int MaxSimultaneousRequestsCount;
        private static readonly TimeSpan DelayBetweenRequests;
        private static readonly SemaphoreSlim RequestsSemaphore;

        public GenericHttpClient(
            [NotNull] IConverter<NameValueCollection, string> nameValueConverter,
            [NotNull] IProxyProvider proxyProvider)
        {
            Guard.ArgumentNotNull(() => nameValueConverter);
            Guard.ArgumentNotNull(() => proxyProvider);

            this.nameValueConverter = nameValueConverter;
            this.proxyProvider = proxyProvider;
        }

        static GenericHttpClient()
        {
            MaxSimultaneousRequestsCount = Settings.Default.MaxSimultaneousRequestsCount;
            DelayBetweenRequests = Settings.Default.DelayBetweenRequests;
            Log.Instance.Debug($"[GenericHttpClient..staticctor] {new { MaxSimultaneousRequestsCount, DelayBetweenRequests }}");
            RequestsSemaphore = new SemaphoreSlim(MaxSimultaneousRequestsCount);
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
            try
            {
                var postData = nameValueConverter.Convert(args);
                Log.Instance.Debug($"[HttpClient] Querying uri '{uri}', args: \r\nPOST: {postData}");
                Log.Instance.Trace($"[HttpClient] Splitted POST data dump: {postData.SplitClean('&').DumpToTextValue()}");
                Log.Instance.Trace($"[HttpClient] Awaiting for semaphore slot (max: {MaxSimultaneousRequestsCount}, atm: {RequestsSemaphore.CurrentCount})");

                RequestsSemaphore.Wait();

                var httpClient = WebRequest.CreateHttp(uri);
                httpClient.CookieContainer = new CookieContainer();
                httpClient.CookieContainer.Add(Cookies);
                httpClient.ContentType = "application/x-www-form-urlencoded";
                httpClient.Method = WebRequestMethods.Http.Post;

                IProxyToken proxyToken;
                if (proxyProvider.TryGetProxy(out proxyToken))
                {
                    Log.Instance.Debug($"[HttpClient] Using proxy {proxyToken.Proxy} for uri '{uri}'");
                    httpClient.Proxy = proxyToken.Proxy;
                }

                try
                {

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
                catch (WebException ex)
                {
                    if (proxyToken == null)
                    {
                        throw;
                    }
                    proxyToken.ReportBroken();
                    throw new WebException($"{ex.Message} (Proxy {proxyToken.Proxy})");
                }
            }
            finally
            {
                Log.Instance.Trace($"[HttpClient] Awainting {DelayBetweenRequests.TotalSeconds}s before releasing semaphore slot...");
                Thread.Sleep(DelayBetweenRequests);
                RequestsSemaphore.Release();
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