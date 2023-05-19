namespace PoeShared.Scaffolding;

public abstract class LazyReactiveObject<T> : DisposableReactiveObject
{
    private static readonly Lazy<T> InstanceSupplier = new();

    public static T Instance => InstanceSupplier.Value;
}