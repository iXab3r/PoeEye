namespace ProxyProvider.ProxiesSource.FoxTools
{
    using System.Collections.Generic;

    internal class GetProxiesContent
    {
        public int PageNumber { get; set; }

        public int PageCount { get; set; }

        public List<FoxProxy> Items { get; set; }
    }
}