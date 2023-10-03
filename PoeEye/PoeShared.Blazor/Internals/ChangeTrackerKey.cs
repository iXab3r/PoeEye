using System;

namespace PoeShared.Blazor.Internals;

internal sealed class ChangeTrackerKey : IChangeTrackerKey
{
    public ChangeTrackerKey(object context, string stampExpression)
    {
        Context = context;
        StampExpression = stampExpression;
    }

    public object Context { get; }

    public string StampExpression { get; }

    private bool Equals(ChangeTrackerKey other)
    {
        return Equals(Context, other.Context) && StampExpression == other.StampExpression;
    }

    public override bool Equals(object obj)
    {
        return ReferenceEquals(this, obj) || (obj is ChangeTrackerKey other && Equals(other));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Context, StampExpression);
    }
}