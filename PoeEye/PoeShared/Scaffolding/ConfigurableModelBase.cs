using PoeShared.Modularity;

namespace PoeShared.Scaffolding;

public interface IConfigurableReactiveObject<out TProperties> : IDisposableReactiveObject
    where TProperties : IPoeEyeConfigVersioned, IHasValidation, new()
{
    TProperties Properties { get; }
}

public abstract class ConfigurableReactiveObjectWithProperties<TProperties> 
    : ReactiveObjectWithProperties<TProperties>, IConfigurableReactiveObject<TProperties> 
    where TProperties : IPoeEyeConfigVersioned, IHasValidation, new()
{
    protected ConfigurableReactiveObjectWithProperties(TProperties properties) : base(properties)
    {
    }

    public TProperties Properties => SaveProperties();
    
    protected abstract void VisitSave(TProperties target);
    
    private TProperties SaveProperties()
    {
        var result = new TProperties();
        VisitSave(result);
        return result;
    }
}