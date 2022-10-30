using PoeShared.Prism;

namespace PoeShared.Caching;

public interface ICachingProxyFactoryConfigurator : ICachingProxyFactory
{
    void SetupTimeToLive<T>(TimeSpan timeToLive) where T : class; 
}

public interface ICachingProxyFactory
{
    T GetOrCreate<T>() where T : class;
}

public interface ICachingProxyFactory<out T> : IFactory<T>
{
}