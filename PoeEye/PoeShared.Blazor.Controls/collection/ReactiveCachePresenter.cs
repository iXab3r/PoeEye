using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.AspNetCore.Components;
using PoeShared.Blazor;

namespace PoeShared.Blazor.Controls;

public partial class ReactiveCachePresenter<TItem, TKey> : BlazorReactiveComponent
    where TItem : notnull
    where TKey : notnull
{
    private object? normalizedFrameSourceIdentity;
    private IObservableCache<TItem, TKey>? connectedSource;
    private IObservable<IChangeSet<TItem, TKey>>? connectedChanges;

    /// <summary>
    /// Preferred entry point for keyed unordered collections such as overlay registries.
    /// Ordering is intentionally ignored at this layer.
    /// </summary>
    [Parameter] public IObservableCache<TItem, TKey>? Source { get; set; }

    /// <summary>
    /// Advanced entry point for callers that already have a cache change stream.
    /// If both <see cref="Source"/> and <see cref="Changes"/> are provided, <see cref="Changes"/> wins.
    /// </summary>
    [Parameter] public IObservable<IChangeSet<TItem, TKey>>? Changes { get; set; }

    [Parameter, EditorRequired] public RenderFragment<TItem>? ItemTemplate { get; set; }

    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    [Parameter] public TimeSpan BatchDelay { get; set; } = TimeSpan.Zero;

    [Parameter] public string ItemTagName { get; set; } = "div";

    [Parameter] public string? ItemClass { get; set; }

    [Parameter] public string? ItemStyle { get; set; }

    [Parameter] public Func<TItem, string?>? ItemClassSelector { get; set; }

    [Parameter] public Func<TItem, string?>? ItemStyleSelector { get; set; }

    protected IObservable<ReactiveCollectionFrame<TItem, TKey>>? NormalizedFrames { get; private set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateNormalizedFrames();
    }

    private void UpdateNormalizedFrames()
    {
        var nextIdentity = (object?)Changes ?? Source;
        if (nextIdentity == null)
        {
            normalizedFrameSourceIdentity = null;
            connectedSource = null;
            connectedChanges = null;
            NormalizedFrames = null;
            return;
        }

        if (ReferenceEquals(normalizedFrameSourceIdentity, nextIdentity) && NormalizedFrames != null)
        {
            return;
        }

        normalizedFrameSourceIdentity = nextIdentity;

        if (Changes != null)
        {
            connectedSource = null;
            connectedChanges = Changes;
        }
        else
        {
            // Cache presenters follow the same rule as list presenters: parent rerenders must not
            // rebuild the logical change stream and accidentally replay the initial snapshot.
            if (!ReferenceEquals(connectedSource, Source) || connectedChanges == null)
            {
                connectedSource = Source;
                connectedChanges = Source?.Connect();
            }
        }

        NormalizedFrames = CreateFrames(connectedChanges);
    }

    private static IObservable<ReactiveCollectionFrame<TItem, TKey>>? CreateFrames(
        IObservable<IChangeSet<TItem, TKey>>? items)
    {
        if (items == null)
        {
            return null;
        }

        return Observable.Defer(() =>
        {
            var itemsByKey = new Dictionary<TKey, TItem>();
            var isInitialFrame = true;

            return items.Select(changeSet =>
            {
                if (isInitialFrame)
                {
                    ApplyChangeSet(changeSet, itemsByKey, operations: null);
                    isInitialFrame = false;

                    var snapshots = itemsByKey
                        .Select(x => new ReactiveCollectionItemSnapshot<TItem, TKey>(x.Key, x.Value))
                        .ToArray();
                    return ReactiveCollectionFrame<TItem, TKey>.From(
                        new ReactiveCollectionResetOperation<TItem, TKey>(snapshots));
                }

                var operations = new List<ReactiveCollectionOperation<TItem, TKey>>();
                ApplyChangeSet(changeSet, itemsByKey, operations);
                return operations.Count == 0
                    ? ReactiveCollectionFrame<TItem, TKey>.Empty
                    : new ReactiveCollectionFrame<TItem, TKey>(operations);
            });
        });
    }

    private static void ApplyChangeSet(
        IChangeSet<TItem, TKey> changeSet,
        Dictionary<TKey, TItem> itemsByKey,
        List<ReactiveCollectionOperation<TItem, TKey>>? operations)
    {
        foreach (var change in changeSet)
        {
            switch (change.Reason)
            {
                case ChangeReason.Add:
                    itemsByKey[change.Key] = change.Current;
                    operations?.Add(new ReactiveCollectionAddOperation<TItem, TKey>(change.Key, change.Current));
                    break;
                case ChangeReason.Update:
                    itemsByKey[change.Key] = change.Current;
                    operations?.Add(new ReactiveCollectionUpdateOperation<TItem, TKey>(change.Key, change.Current));
                    break;
                case ChangeReason.Remove:
                    itemsByKey.Remove(change.Key);
                    operations?.Add(new ReactiveCollectionRemoveOperation<TItem, TKey>(change.Key));
                    break;
                case ChangeReason.Refresh:
                    itemsByKey[change.Key] = change.Current;
                    operations?.Add(new ReactiveCollectionUpdateOperation<TItem, TKey>(change.Key, change.Current));
                    break;
                case ChangeReason.Moved:
                    // Cache-backed presenters are intentionally unordered.
                    // Keeping this as an update preserves the island without inventing list semantics.
                    itemsByKey[change.Key] = change.Current;
                    operations?.Add(new ReactiveCollectionUpdateOperation<TItem, TKey>(change.Key, change.Current));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(change), change.Reason, $"Unsupported cache change reason: {change.Reason}");
            }
        }
    }
}
