using Castle.DynamicProxy;
using PoeShared.Prism;
using Unity;

namespace PoeShared.Caching;

internal sealed class CachingProxyFactory<T> : ICachingProxyFactory<T> where T : class
{
    private readonly ICachingProxyFactory proxyFactory;

    public CachingProxyFactory(ICachingProxyFactory proxyFactory)
    {
        this.proxyFactory = proxyFactory;
    }

    public T Create()
    {
        return proxyFactory.GetOrCreate<T>();
    }
}

internal sealed class CachingProxyFactory : DisposableReactiveObjectWithLogger, ICachingProxyFactoryConfigurator
{
    private readonly IUnityContainer container;
    private readonly ConcurrentDictionary<Type, ProxyInfo> cache = new();
    private readonly ConcurrentDictionary<Type, CachingInterceptor> interceptorByType = new();
    private readonly ProxyGenerator proxyGenerator = new();
    private readonly IClock clock;

    public CachingProxyFactory(IUnityContainer container)
    {
        this.container = container;
        clock = container.Resolve<IClock>();
    }

    public T GetOrCreate<T>() where T : class
    {
        var proxyInfo = cache.GetOrAdd(typeof(T), key => CreateCachingProxy<T>());
        return (T) proxyInfo.Proxy;
    }

    public void SetupTimeToLive<T>(TimeSpan timeToLive) where T : class
    {
        Log.Debug(() => $"Setting TTL of proxy for type {typeof(T)} to {timeToLive}");
        var interceptor = interceptorByType.GetOrAdd(typeof(T), _ => new CachingInterceptor(Log, clock));
        interceptor.TimeToLive = timeToLive;
    }

    private ProxyInfo CreateCachingProxy<T>() where T : class
    {
        Log.Debug(() => $"Creating new proxy for type {typeof(T)}");
        var factory = container.Resolve<IFactory<T>>();
        var result = factory.Create();
        var interceptor = interceptorByType.GetOrAdd(typeof(T), _ => new CachingInterceptor(Log, clock));
        var proxy = proxyGenerator.CreateInterfaceProxyWithTarget(result, interceptor);
        return new ProxyInfo(interceptor, proxy);
    }

    private readonly record struct ProxyInfo(CachingInterceptor Interceptor, object Proxy)
    {
        public CachingInterceptor Interceptor { get; } = Interceptor;
        public object Proxy { get; } = Proxy;
    }
}