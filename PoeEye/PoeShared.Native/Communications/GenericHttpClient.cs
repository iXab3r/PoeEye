using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;

using JetBrains.Annotations;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.Communications;

internal sealed class GenericHttpClient : IHttpClient
{
    private static readonly IFluentLog Log = typeof(GenericHttpClient).PrepareLogger();

    private static readonly string DefaultUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";

    private readonly IConverter<NameValueCollection, string> nameValueConverter;
    private CookieCollection cookies = new CookieCollection();
    private WebHeaderCollection customHeaders = new WebHeaderCollection();

    public GenericHttpClient(
        [NotNull] IConverter<NameValueCollection, string> nameValueConverter)
    {
        Guard.ArgumentNotNull(nameValueConverter, nameof(nameValueConverter));

        this.nameValueConverter = nameValueConverter;
    }

    public CookieCollection Cookies
    {
        get => cookies;
        set => cookies = value ?? new CookieCollection();
    }

    public TimeSpan? Timeout { get; set; }

    public WebHeaderCollection CustomHeaders
    {
        get => customHeaders;
        set => customHeaders = value ?? new WebHeaderCollection();
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
        Log.Debug($"[HttpClient] Querying uri '{uri}' (GET)");

        var httpClient = WebRequest.CreateHttp(uri);

        httpClient.CookieContainer = new CookieContainer();
        httpClient.CookieContainer.Add(Cookies);
        httpClient.Headers.Add(CustomHeaders);
        httpClient.Referer = Referer;
        httpClient.UserAgent = UserAgent;
        if (Timeout != null)
        {
            httpClient.Timeout = (int) Timeout.Value.TotalMilliseconds;
        }

        httpClient.Method = WebRequestMethods.Http.Get;
        var rawResponse = IssueRequest(httpClient, string.Empty);

        return rawResponse;
    }

    private string PostQueryInternal(string uri, NameValueCollection args)
    {
        var postData = nameValueConverter.Convert(args);
        Log.Debug($"[HttpClient] Querying uri '{uri}', args: \r\nPOST: {postData}");
        if (Log.IsDebugEnabled)
        {
            Log.Debug($"[HttpClient] POST data dump: {postData.SplitTrim('&').DumpToString()}");
        }

        var httpClient = WebRequest.CreateHttp(uri);

        httpClient.CookieContainer = new CookieContainer();
        httpClient.CookieContainer.Add(Cookies);
        httpClient.Headers.Add(CustomHeaders);
        httpClient.Referer = Referer;
        httpClient.UserAgent = UserAgent;
        httpClient.AllowAutoRedirect = true;
        if (Timeout != null)
        {
            httpClient.Timeout = (int) Timeout.Value.TotalMilliseconds;
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
            Log.Debug($"[HttpClient] Using proxy {proxy} for uri '{httpRequest.RequestUri}'");
            httpRequest.Proxy = proxy;
        }

        if (httpRequest.Method == WebRequestMethods.Http.Post && !string.IsNullOrEmpty(requestData))
        {
            Log.Debug("[HttpClient] Preparing POST data...");
            var data = Encoding.ASCII.GetBytes(requestData);
            using (var stream = httpRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
        }

        Log.Debug($"[HttpClient] Sending {httpRequest.Method} request with timeout of {httpRequest.Timeout}ms to {httpRequest.RequestUri}");
        using (var response = (HttpWebResponse) httpRequest.GetResponse())
        using (var responseStream = response.GetResponseStream())
        {
            var rawResponse = string.Empty;

            if (responseStream != null)
            {
                var rawBytes = responseStream.ReadToEnd() ?? new byte[0];
                Log.Debug($"[HttpClient] Received response, status: {response.StatusCode}, binary length: {rawBytes.Length}");
                rawResponse = Encoding.UTF8.GetString(rawBytes);
                Log.Debug($"[HttpClient] Resulting response(string) length: {rawResponse.Length}");
            }
            else
            {
                Log.Warn($"[HttpClient] Received null response stream ! Status: {response.StatusCode}");
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

        throw new ArgumentException($"Wrong status code, expected 200 OK, got {response.StatusCode} ({response.StatusDescription})");
    }
}