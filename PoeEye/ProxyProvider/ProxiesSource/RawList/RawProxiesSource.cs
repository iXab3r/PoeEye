namespace ProxyProvider.ProxiesSource.RawList
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    internal sealed class RawProxiesSource : IProxiesSource
    {
        private readonly string[] proxiesRaw = { };

        public IEnumerable<IWebProxy> GetProxies()
        {
            return proxiesRaw.Select(x => new WrappedProxy(new Uri($"http://{x}", UriKind.RelativeOrAbsolute)));
        }
    }
}