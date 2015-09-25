namespace PoeShared.Http
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    public interface IHttpClient
    {
        IObservable<string> PostQuery(string uri, IDictionary<string, object> args);

        CookieCollection Cookies { get; set; } 
    }
}