namespace PoeShared.Http
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    using JetBrains.Annotations;

    public interface IHttpClient
    {
        [NotNull] 
        IObservable<string> PostQuery([NotNull] string uri, [NotNull] IDictionary<string, object> args);

        CookieCollection Cookies { get; set; } 
    }
}