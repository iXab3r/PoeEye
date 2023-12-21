using Castle.DynamicProxy;
using PoeShared.Prism;
using Unity;

namespace PoeShared.Caching;

/// <summary>
/// Factory for creating caching proxies that intercept method calls and cache their results.
/// </summary>
internal sealed class CachingProxyFactory<T> : ICachingProxyFactory<T> where T : class
{
    private readonly ICachingProxyFactory proxyFactory;

    public CachingProxyFactory(ICachingProxyFactory proxyFactory)
    {
        this.proxyFactory = proxyFactory;
    }

    /// <inheritdoc />
    public T Create()
    {
        return proxyFactory.GetOrCreate<T>();
    }
}