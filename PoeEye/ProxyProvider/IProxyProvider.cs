namespace ProxyProvider
{
    public interface IProxyProvider
    {
        bool TryGetProxy(out IProxyToken proxy);
    }
}