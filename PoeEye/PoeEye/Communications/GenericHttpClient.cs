namespace PoeEye.Communications
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading;

    using CsQuery.ExtensionMethods.Internal;

    using EasyHttp.Http;
    using EasyHttp.Infrastructure;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.DumpToText;
    using PoeShared.Http;

    using Properties;

    using TypeConverter;

    using HttpClient = System.Net.Http.HttpClient;
    using Log = PoeShared.Log;

    internal sealed class GenericHttpClient : IHttpClient
    {
        private readonly IConverter<NameValueCollection, string> nameValueConverter;
        private static readonly int MaxSimultaneousRequestsCount;
        private static readonly TimeSpan DelayBetweenRequests;
        private static readonly SemaphoreSlim RequestsSemaphore;

        public GenericHttpClient(
            [NotNull] IConverter<NameValueCollection, string> nameValueConverter)
        {
            Guard.ArgumentNotNull(() => nameValueConverter);

            this.nameValueConverter = nameValueConverter;
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

                var httpClient = new EasyHttp.Http.HttpClient();
                httpClient.Request.Cookies = Cookies;

                var response = httpClient.Post(uri, postData, HttpContentTypes.ApplicationXWwwFormUrlEncoded);
                Log.Instance.Debug(
                    $"[HttpClient] Received response, status: {response.StatusCode}, length: {response.RawText?.Length}");

                CheckResponseStatusOrThrow(response);

                return response.RawText;
            }
            finally
            {
                Log.Instance.Trace($"[HttpClient] Awainting {DelayBetweenRequests.TotalSeconds}s before releasing semaphore slot...");
                Thread.Sleep(DelayBetweenRequests);
                RequestsSemaphore.Release();
            }
            
        }

        private static void CheckResponseStatusOrThrow(HttpResponse response)
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            throw new HttpException(response.StatusCode,
                $"Wrong status code, expected 200 OK, got {response.StatusCode}");
        }
    }
}