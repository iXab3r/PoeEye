using Unity.Lifetime;

namespace PoeShared.Scaffolding;

public sealed class NonDisposingContainerControlledLifetimeManager : SynchronizedLifetimeManager, ITypeLifetimeManager, IFactoryLifetimeManager
{
    // Use NoValue sentinel so null can be a real value
    private object value = NoValue;

    protected override object SynchronizedGetValue(ILifetimeContainer container)
    {
        return value;
    }

    protected override void SynchronizedSetValue(object newValue, ILifetimeContainer container)
    {
        value = newValue;
    }

    public override void RemoveValue(ILifetimeContainer container = null)
    {
        // Clear the stored value but DO NOT dispose it.
        Interlocked.Exchange(ref value, NoValue);
    }

    public override bool InUse
    {
        get { return !ReferenceEquals(value, NoValue); }
    }

    protected override LifetimeManager OnCreateLifetimeManager()
    {
        return new NonDisposingContainerControlledLifetimeManager();
    }
}