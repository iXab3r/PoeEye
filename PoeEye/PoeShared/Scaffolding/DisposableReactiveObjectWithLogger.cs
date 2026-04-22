namespace PoeShared.Scaffolding;

/// <summary>
/// Disposable reactive base class that also exposes a lazily-created type logger.
/// </summary>
/// <remarks>
/// Use this for service-style objects that need their own logger. Objects that should log through
/// an owner-specific or user-facing logger can inherit from <see cref="DisposableReactiveObject"/>
/// and receive <see cref="IFluentLog"/> explicitly instead.
/// </remarks>
public abstract class DisposableReactiveObjectWithLogger : DisposableReactiveObject
{
    private readonly Lazy<IFluentLog> logSupplier;
        
    protected DisposableReactiveObjectWithLogger()
    {
        logSupplier = new Lazy<IFluentLog>(PrepareLogger);
    }

    protected IFluentLog Log => logSupplier.Value;

    protected virtual IFluentLog PrepareLogger()
    {
        return GetType().PrepareLogger();
    }
}
