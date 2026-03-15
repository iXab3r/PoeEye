using Microsoft.AspNetCore.Components;

namespace PoeShared.Blazor.Controls.Services;

/// <summary>
/// Stores per-item render fragments on the .NET side.
/// JS only passes an opaque registration id into the mounted host component.
/// </summary>
internal interface IReactiveCollectionItemRegistry
{
    ReactiveCollectionItemRegistration Register(RenderFragment content);

    ReactiveCollectionItemRegistration Update(string registrationId, RenderFragment content);

    ReactiveCollectionStoredItem Get(string registrationId);

    void Unregister(string registrationId);
}

internal readonly record struct ReactiveCollectionItemRegistration(string RegistrationId, long Version);

internal sealed record ReactiveCollectionStoredItem(RenderFragment Content, long Version);
