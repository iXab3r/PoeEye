namespace PoeShared.Http
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;

    using JetBrains.Annotations;

    public interface IHttpClient
    {
        [NotNull] 
        IObservable<string> PostQuery([NotNull] string uri, [NotNull] NameValueCollection args);

        CookieCollection Cookies { get; set; }

        [NotNull]
        IObservable<Stream> GetStreamAsync([NotNull] Uri requestUri);
    }
}