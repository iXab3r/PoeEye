using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using JetBrains.Annotations;

namespace PoeShared.Communications
{
    public interface IHttpClient
    {
        CookieCollection Cookies { [NotNull] get; [NotNull] set; }

        IWebProxy Proxy { [CanBeNull] get; [CanBeNull] set; }

        string Referer { [CanBeNull] get; [CanBeNull] set; }

        string UserAgent { [CanBeNull] get; [CanBeNull] set; }

        TimeSpan? Timeout { get; set; }

        WebHeaderCollection CustomHeaders { [NotNull] get; [NotNull] set; }

        [NotNull]
        IObservable<string> Post([NotNull] string uri, [NotNull] NameValueCollection args);

        [NotNull]
        IObservable<string> Get([NotNull] string uri);

        [NotNull]
        IObservable<Stream> GetStreamAsync([NotNull] Uri requestUri);
    }
}