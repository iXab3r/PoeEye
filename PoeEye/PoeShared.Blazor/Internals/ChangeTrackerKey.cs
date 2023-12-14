using System;
using System.Linq.Expressions;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Internals;

internal sealed class ChangeTrackerKey : IChangeTrackerKey
{
    public ChangeTrackerKey(object context, Expression expression)
    {
        Context = context;
        Expression = expression;
    }

    public object Context { get; }

    public Expression Expression { get; }

    private bool Equals(ChangeTrackerKey other)
    {
        return Equals(Context, other.Context) && ExpressionEqualityComparer.Instance.Equals(Expression, other.Expression);
    }

    public override bool Equals(object obj)
    {
        return ReferenceEquals(this, obj) || (obj is ChangeTrackerKey other && Equals(other));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Context, ExpressionEqualityComparer.Instance.GetHashCode(Expression));
    }
}