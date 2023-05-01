namespace PoeShared.Scaffolding;

public sealed class ProxyServiceProvider : DisposableReactiveObjectWithLogger, IServiceProvider
{
    public ProxyServiceProvider()
    {
    }
    
    public IServiceProvider ServiceProvider { get; set; }

    public object GetService(Type serviceType)
    {
        var provider = ServiceProvider;
        if (provider == null)
        {
            throw new InvalidOperationException("Service provider is not ready");
        }

        return provider.GetService(serviceType);
    }
}