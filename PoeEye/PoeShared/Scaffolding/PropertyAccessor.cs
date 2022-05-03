using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

public readonly struct PropertyAccessor<TValue>
{
    private readonly object source;

    public PropertyAccessor([NotNull] object source, [NotNull] string propertyPath)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        PropertyPath = propertyPath ?? throw new ArgumentNullException(nameof(propertyPath));
    }

    public string PropertyPath { get; }

    public TValue Value
    {
        get => source.GetPropertyValue<TValue>(PropertyPath);
        set => source.SetPropertyValue(PropertyPath, value);
    }
}