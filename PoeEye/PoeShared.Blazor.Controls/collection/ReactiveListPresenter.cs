using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.AspNetCore.Components;
using PoeShared.Blazor;

namespace PoeShared.Blazor.Controls;

public partial class ReactiveListPresenter<TItem, TKey> : BlazorReactiveComponent
    where TItem : notnull
    where TKey : notnull
{
    private object? normalizedFrameSourceIdentity;
    private IObservableList<TItem>? connectedSource;
    private IObservable<IChangeSet<TItem>>? connectedChanges;

    /// <summary>
    /// Preferred entry point for ordered collections.
    /// The presenter owns the ordering semantics and translates list-specific operations, including Move, without recreating item roots.
    /// </summary>
    [Parameter] public IObservableList<TItem>? Source { get; set; }

    /// <summary>
    /// Advanced entry point for callers that already have a list change stream.
    /// If both <see cref="Source"/> and <see cref="Changes"/> are provided, <see cref="Changes"/> wins.
    /// </summary>
    [Parameter] public IObservable<IChangeSet<TItem>>? Changes { get; set; }

    [Parameter, EditorRequired] public Func<TItem, TKey>? KeySelector { get; set; }

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
        var nextKeySelector = KeySelector;
        var nextIdentity = (object?)Changes ?? Source;

        if (nextIdentity == null || nextKeySelector == null)
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
            // Keep the Connect() result stable for the lifetime of the bound list source.
            // Parent rerenders should not manufacture a fresh initial Reset and recreate islands.
            if (!ReferenceEquals(connectedSource, Source) || connectedChanges == null)
            {
                connectedSource = Source;
                connectedChanges = Source?.Connect();
            }
        }

        // KeySelector is intentionally captured when the logical source binding is established.
        // Callers often provide inline lambdas in Razor, which would otherwise rebuild the stream on every parent rerender.
        NormalizedFrames = CreateFrames(connectedChanges, nextKeySelector);
    }

    private static IObservable<ReactiveCollectionFrame<TItem, TKey>>? CreateFrames(
        IObservable<IChangeSet<TItem>>? items,
        Func<TItem, TKey>? keySelector)
    {
        if (items == null || keySelector == null)
        {
            return null;
        }

        return Observable.Defer(() =>
        {
            var orderedItems = new List<ReactiveListStateItem<TItem, TKey>>();
            var isInitialFrame = true;

            return items.Select(changeSet =>
            {
                if (isInitialFrame)
                {
                    ApplyChangeSet(changeSet, orderedItems, keySelector, operations: null);
                    isInitialFrame = false;

                    var snapshots = orderedItems
                        .Select(x => new ReactiveCollectionItemSnapshot<TItem, TKey>(x.Key, x.Item))
                        .ToArray();
                    return ReactiveCollectionFrame<TItem, TKey>.From(
                        new ReactiveCollectionResetOperation<TItem, TKey>(snapshots));
                }

                var operations = new List<ReactiveCollectionOperation<TItem, TKey>>();
                ApplyChangeSet(changeSet, orderedItems, keySelector, operations);
                return operations.Count == 0
                    ? ReactiveCollectionFrame<TItem, TKey>.Empty
                    : new ReactiveCollectionFrame<TItem, TKey>(operations);
            });
        });
    }

    private static void ApplyChangeSet(
        IChangeSet<TItem> changeSet,
        List<ReactiveListStateItem<TItem, TKey>> orderedItems,
        Func<TItem, TKey> keySelector,
        List<ReactiveCollectionOperation<TItem, TKey>>? operations)
    {
        foreach (var change in changeSet)
        {
            switch (change.Reason)
            {
                case ListChangeReason.Add:
                    AddItem(orderedItems, change.Item.Current, change.Item.CurrentIndex, keySelector, operations);
                    break;
                case ListChangeReason.AddRange:
                    AddRange(orderedItems, change.Range, keySelector, operations);
                    break;
                case ListChangeReason.Remove:
                    RemoveItem(orderedItems, change.Item.Current, change.Item.CurrentIndex, keySelector, operations);
                    break;
                case ListChangeReason.RemoveRange:
                    RemoveRange(orderedItems, change.Range, keySelector, operations);
                    break;
                case ListChangeReason.Replace:
                    ReplaceItem(orderedItems, change, keySelector, operations);
                    break;
                case ListChangeReason.Moved:
                    MoveItem(orderedItems, change.Item.Current, change.Item.PreviousIndex, change.Item.CurrentIndex, keySelector, operations);
                    break;
                case ListChangeReason.Clear:
                    orderedItems.Clear();
                    operations?.Add(new ReactiveCollectionClearOperation<TItem, TKey>());
                    break;
                case ListChangeReason.Refresh:
                    RefreshItem(orderedItems, change.Item.Current, keySelector, operations);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(change), change.Reason, $"Unsupported list change reason: {change.Reason}");
            }
        }
    }

    private static void AddItem(
        List<ReactiveListStateItem<TItem, TKey>> orderedItems,
        TItem item,
        int currentIndex,
        Func<TItem, TKey> keySelector,
        List<ReactiveCollectionOperation<TItem, TKey>>? operations)
    {
        var normalizedIndex = NormalizeInsertIndex(currentIndex, orderedItems.Count);
        var beforeKey = normalizedIndex < orderedItems.Count ? orderedItems[normalizedIndex].Key : default;
        var key = keySelector(item);

        orderedItems.Insert(normalizedIndex, new ReactiveListStateItem<TItem, TKey>(key, item));
        operations?.Add(new ReactiveCollectionAddOperation<TItem, TKey>(key, item, beforeKey));
    }

    private static void AddRange(
        List<ReactiveListStateItem<TItem, TKey>> orderedItems,
        RangeChange<TItem> range,
        Func<TItem, TKey> keySelector,
        List<ReactiveCollectionOperation<TItem, TKey>>? operations)
    {
        var normalizedIndex = NormalizeInsertIndex(range.Index, orderedItems.Count);
        foreach (var item in range)
        {
            AddItem(orderedItems, item, normalizedIndex, keySelector, operations);
            normalizedIndex++;
        }
    }

    private static void RemoveItem(
        List<ReactiveListStateItem<TItem, TKey>> orderedItems,
        TItem item,
        int currentIndex,
        Func<TItem, TKey> keySelector,
        List<ReactiveCollectionOperation<TItem, TKey>>? operations)
    {
        var key = keySelector(item);
        RemoveInternal(orderedItems, key, currentIndex);
        operations?.Add(new ReactiveCollectionRemoveOperation<TItem, TKey>(key));
    }

    private static void RemoveRange(
        List<ReactiveListStateItem<TItem, TKey>> orderedItems,
        RangeChange<TItem> range,
        Func<TItem, TKey> keySelector,
        List<ReactiveCollectionOperation<TItem, TKey>>? operations)
    {
        var currentIndex = range.Index;
        foreach (var item in range)
        {
            RemoveItem(orderedItems, item, currentIndex, keySelector, operations);
        }
    }

    private static void ReplaceItem(
        List<ReactiveListStateItem<TItem, TKey>> orderedItems,
        Change<TItem> change,
        Func<TItem, TKey> keySelector,
        List<ReactiveCollectionOperation<TItem, TKey>>? operations)
    {
        var currentChange = change.Item;
        var currentKey = keySelector(currentChange.Current);
        var previousKey = currentChange.Previous.HasValue ? keySelector(currentChange.Previous.Value) : currentKey;
        var replaceIndex = currentChange.CurrentIndex >= 0 ? currentChange.CurrentIndex : FindIndex(orderedItems, previousKey);

        if (EqualityComparer<TKey>.Default.Equals(previousKey, currentKey))
        {
            if (replaceIndex >= 0 && replaceIndex < orderedItems.Count)
            {
                orderedItems[replaceIndex] = new ReactiveListStateItem<TItem, TKey>(currentKey, currentChange.Current);
            }

            operations?.Add(new ReactiveCollectionUpdateOperation<TItem, TKey>(currentKey, currentChange.Current));
            return;
        }

        RemoveInternal(orderedItems, previousKey, currentChange.PreviousIndex);
        operations?.Add(new ReactiveCollectionRemoveOperation<TItem, TKey>(previousKey));

        AddItem(orderedItems, currentChange.Current, replaceIndex, keySelector, operations);
    }

    private static void MoveItem(
        List<ReactiveListStateItem<TItem, TKey>> orderedItems,
        TItem item,
        int previousIndex,
        int currentIndex,
        Func<TItem, TKey> keySelector,
        List<ReactiveCollectionOperation<TItem, TKey>>? operations)
    {
        var key = keySelector(item);
        var entry = RemoveInternal(orderedItems, key, previousIndex);
        if (entry == null)
        {
            return;
        }

        var normalizedIndex = NormalizeInsertIndex(currentIndex, orderedItems.Count);
        var beforeKey = normalizedIndex < orderedItems.Count ? orderedItems[normalizedIndex].Key : default;
        orderedItems.Insert(normalizedIndex, entry.Value);
        operations?.Add(new ReactiveCollectionMoveOperation<TItem, TKey>(key, beforeKey));
    }

    private static void RefreshItem(
        List<ReactiveListStateItem<TItem, TKey>> orderedItems,
        TItem item,
        Func<TItem, TKey> keySelector,
        List<ReactiveCollectionOperation<TItem, TKey>>? operations)
    {
        var key = keySelector(item);
        var existingIndex = FindIndex(orderedItems, key);
        if (existingIndex >= 0)
        {
            orderedItems[existingIndex] = new ReactiveListStateItem<TItem, TKey>(key, item);
        }

        operations?.Add(new ReactiveCollectionUpdateOperation<TItem, TKey>(key, item));
    }

    private static ReactiveListStateItem<TItem, TKey>? RemoveInternal(
        List<ReactiveListStateItem<TItem, TKey>> orderedItems,
        TKey key,
        int currentIndex)
    {
        var index = currentIndex >= 0 && currentIndex < orderedItems.Count && EqualityComparer<TKey>.Default.Equals(orderedItems[currentIndex].Key, key)
            ? currentIndex
            : FindIndex(orderedItems, key);

        if (index < 0)
        {
            return null;
        }

        var existingItem = orderedItems[index];
        orderedItems.RemoveAt(index);
        return existingItem;
    }

    private static int FindIndex(List<ReactiveListStateItem<TItem, TKey>> orderedItems, TKey key)
    {
        return orderedItems.FindIndex(x => EqualityComparer<TKey>.Default.Equals(x.Key, key));
    }

    private static int NormalizeInsertIndex(int currentIndex, int count)
    {
        if (currentIndex < 0 || currentIndex > count)
        {
            return count;
        }

        return currentIndex;
    }

    private readonly record struct ReactiveListStateItem<TValue, TValueKey>(TValueKey Key, TValue Item)
        where TValueKey : notnull;
}
