namespace ProxyProvider
{
    using System.Collections.Generic;
    using System.Net;

    public interface IProxyProvider
    {
        bool TryGetProxy(out IProxyToken proxy); 

        int ActiveProxiesCount { get; }

        int TotalProxiesCount { get; }
    }
}