using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PoeShared.Blazor.Controls;

/// <summary>
/// Represents a batch of collection operations that should be applied in order.
/// The presenter intentionally preserves every operation in the frame and does not compact them.
/// </summary>
public sealed record ReactiveCollectionFrame<TItem, TKey> where TKey : notnull
{
    public ReactiveCollectionFrame(IReadOnlyList<ReactiveCollectionOperation<TItem, TKey>> operations)
    {
        Operations = operations ?? Array.Empty<ReactiveCollectionOperation<TItem, TKey>>();
    }

    public IReadOnlyList<ReactiveCollectionOperation<TItem, TKey>> Operations { get; }

    public bool HasOperations => Operations.Count > 0;

    public static ReactiveCollectionFrame<TItem, TKey> Empty { get; } =
        new(Array.Empty<ReactiveCollectionOperation<TItem, TKey>>());

    public static ReactiveCollectionFrame<TItem, TKey> From(params ReactiveCollectionOperation<TItem, TKey>[] operations)
    {
        return new ReactiveCollectionFrame<TItem, TKey>(operations);
    }
}

public readonly record struct ReactiveCollectionItemSnapshot<TItem, TKey>(TKey Key, TItem Item) where TKey : notnull;

public abstract record ReactiveCollectionOperation<TItem, TKey> where TKey : notnull;

public sealed record ReactiveCollectionAddOperation<TItem, TKey>(TKey Key, TItem Item, TKey? BeforeKey = default)
    : ReactiveCollectionOperation<TItem, TKey> where TKey : notnull;

public sealed record ReactiveCollectionUpdateOperation<TItem, TKey>(TKey Key, TItem Item)
    : ReactiveCollectionOperation<TItem, TKey> where TKey : notnull;

public sealed record ReactiveCollectionMoveOperation<TItem, TKey>(TKey Key, TKey? BeforeKey = default)
    : ReactiveCollectionOperation<TItem, TKey> where TKey : notnull;

public sealed record ReactiveCollectionRemoveOperation<TItem, TKey>(TKey Key)
    : ReactiveCollectionOperation<TItem, TKey> where TKey : notnull;

public sealed record ReactiveCollectionClearOperation<TItem, TKey>()
    : ReactiveCollectionOperation<TItem, TKey> where TKey : notnull;

public sealed record ReactiveCollectionResetOperation<TItem, TKey>(IReadOnlyList<ReactiveCollectionItemSnapshot<TItem, TKey>> Items)
    : ReactiveCollectionOperation<TItem, TKey> where TKey : notnull
{
    public ReactiveCollectionResetOperation(IEnumerable<ReactiveCollectionItemSnapshot<TItem, TKey>> items)
        : this(new ReadOnlyCollection<ReactiveCollectionItemSnapshot<TItem, TKey>>(items?.ToList() ?? new List<ReactiveCollectionItemSnapshot<TItem, TKey>>()))
    {
    }
}
