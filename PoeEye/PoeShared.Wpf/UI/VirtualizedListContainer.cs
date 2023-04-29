using System;
using System.Threading;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.UI;

public class VirtualizedListContainer<T> : DisposableReactiveObject, IVirtualizedListContainer<T> where T : class
{
    private static readonly Binder<VirtualizedListContainer<T>> Binder = new();
    private static long GlobalIdx = 0;

    private readonly string id = $"Container#{Interlocked.Increment(ref GlobalIdx)}";

    static VirtualizedListContainer()
    {
        Binder.Bind(x => x.Value != default).To(x => x.HasValue);
    }

    public VirtualizedListContainer()
    {
        Binder.Attach(this).AddTo(Anchors);
    }

    public T Value { get; set; }
    public Type ValueType { get; set; }
    public int Index { get; set; }
    public bool HasValue { get; [UsedImplicitly] private set; }

    protected bool Equals(VirtualizedListContainer<T> other)
    {
        return id == other.id;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((VirtualizedListContainer<T>) obj);
    }

    public override int GetHashCode()
    {
        return (id != null ? id.GetHashCode() : 0);
    }

    protected override void FormatToString(ToStringBuilder builder)
    {
        builder.Append(HasValue ? $"[{Value.Dump()}]" : "<Empty>");
    }
}