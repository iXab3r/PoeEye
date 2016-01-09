namespace PoeShared.Http
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    public interface IHttpClient
    {
        [NotNull] 
        IObservable<string> Post([NotNull] string uri, [NotNull] NameValueCollection args);

        [NotNull]
        IObservable<string> Get([NotNull] string uri);

        CookieCollection Cookies { get; set; }

        IWebProxy Proxy { get; set; }

        [NotNull]
        IObservable<Stream> GetStreamAsync([NotNull] Uri requestUri);
    }
}