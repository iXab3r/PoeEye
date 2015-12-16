namespace ProxyProvider
{
    using System.Net;

    using JetBrains.Annotations;

    public interface IProxyToken 
    {
        IWebProxy Proxy { [NotNull] get; }

        void ReportBroken();
    }
}