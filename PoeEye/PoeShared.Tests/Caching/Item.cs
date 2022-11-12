using System;

namespace PoeShared.Tests.Caching;

public sealed class Item : DisposableReactiveObject, IComparable<Item>, IEquatable<Item>
{
    public Item(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public bool IsDisposed => Anchors.IsDisposed;

    public int CompareTo(Item other)
    {
        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        if (ReferenceEquals(null, other))
        {
            return 1;
        }

        return Value.CompareTo(other.Value);
    }

    public bool Equals(Item other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Value == other.Value;
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

        return Equals((Item) obj);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.AppendParameter(nameof(Value), Value);
    }
}