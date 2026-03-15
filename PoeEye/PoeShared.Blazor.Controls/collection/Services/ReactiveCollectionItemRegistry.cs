using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Components;

namespace PoeShared.Blazor.Controls.Services;

internal sealed class ReactiveCollectionItemRegistry : IReactiveCollectionItemRegistry
{
    private readonly ConcurrentDictionary<string, ReactiveCollectionStoredItem> itemsById = new();

    private long registrationCounter;

    public ReactiveCollectionItemRegistration Register(RenderFragment content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var registrationId = $"reactive-collection-item-{Interlocked.Increment(ref registrationCounter)}";
        var storedItem = new ReactiveCollectionStoredItem(content, Version: 1);
        itemsById[registrationId] = storedItem;
        return new ReactiveCollectionItemRegistration(registrationId, storedItem.Version);
    }

    public ReactiveCollectionItemRegistration Update(string registrationId, RenderFragment content)
    {
        ArgumentException.ThrowIfNullOrEmpty(registrationId);
        ArgumentNullException.ThrowIfNull(content);

        while (true)
        {
            if (!itemsById.TryGetValue(registrationId, out var current))
            {
                throw new KeyNotFoundException($"Failed to resolve reactive collection item registration '{registrationId}'.");
            }

            var updated = current with
            {
                Content = content,
                Version = current.Version + 1
            };

            if (itemsById.TryUpdate(registrationId, updated, current))
            {
                return new ReactiveCollectionItemRegistration(registrationId, updated.Version);
            }
        }
    }

    public ReactiveCollectionStoredItem Get(string registrationId)
    {
        ArgumentException.ThrowIfNullOrEmpty(registrationId);

        if (!itemsById.TryGetValue(registrationId, out var storedItem))
        {
            throw new KeyNotFoundException($"Failed to resolve reactive collection item registration '{registrationId}'.");
        }

        return storedItem;
    }

    public void Unregister(string registrationId)
    {
        if (string.IsNullOrWhiteSpace(registrationId))
        {
            return;
        }

        itemsById.TryRemove(registrationId, out _);
    }
}
