namespace PoeShared.Http
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;

    using JetBrains.Annotations;

    public interface IHttpClient
    {
        CookieCollection Cookies { get; set; }

        IWebProxy Proxy { get; set; }

        [NotNull]
        IObservable<string> Post([NotNull] string uri, [NotNull] NameValueCollection args);

        [NotNull]
        IObservable<string> Get([NotNull] string uri);

        [NotNull]
        IObservable<Stream> GetStreamAsync([NotNull] Uri requestUri);
    }
}