using PoeShared.Prism;

namespace PoeShared.Caching;

/// <summary>
/// Factory for creating caching proxies that intercept method calls and cache their results.
/// </summary>
public interface ICachingProxyFactoryConfigurator : ICachingProxyFactory
{
    /// <summary>
    /// Sets the time-to-live for the caching proxy of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the proxy to configure.</typeparam>
    /// <param name="timeToLive">The time span after which the cache entries will expire.</param>
    void SetupTimeToLive<T>(TimeSpan timeToLive) where T : class; 
}

/// <summary>
/// Factory for creating caching proxies that intercept method calls and cache their results.
/// </summary>
public interface ICachingProxyFactory
{
    /// <summary>
    /// Retrieves or creates a caching proxy for the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the proxy to create.</typeparam>
    /// <returns>The caching proxy instance.</returns>
    T GetOrCreate<T>() where T : class;
}

/// <summary>
/// Factory for creating caching proxies that intercept method calls and cache their results.
/// </summary>
public interface ICachingProxyFactory<out T> : IFactory<T>
{
}