using PoeShared.Prism;

namespace PoeShared.Modularity;

public abstract class ConfigMetadataConverter<T1, T2> : IConverter<T1, T2>
    where T1 : IPoeEyeConfigVersioned, new()
    where T2 : IPoeEyeConfigVersioned, new()
{
    protected ConfigMetadataConverter()
    {
    }

    public abstract T2 Convert(T1 value);

    protected T2 PrepareClone(T1 value)
    {
        var result = new T2();
        value.TransferPropertiesTo(result, propertiesToExclude: nameof(value.Version));
        return result;
    }
}