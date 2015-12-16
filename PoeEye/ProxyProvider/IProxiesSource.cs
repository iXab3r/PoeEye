namespace ProxyProvider
{
    using System.Collections.Generic;
    using System.Net;

    using JetBrains.Annotations;

    public interface IProxiesSource
    {
        [NotNull] 
        IEnumerable<IWebProxy> GetProxies();
    }
}