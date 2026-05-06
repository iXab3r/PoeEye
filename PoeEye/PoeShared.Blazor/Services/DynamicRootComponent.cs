using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.JSInterop;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Services;

public interface IDynamicRootComponent : IAsyncDisposable
{
    string ElementId { get; }
    string ComponentIdentifier { get; }
    ValueTask SetParameters(object parameters);
}

internal sealed class DynamicRootComponent : DisposableReactiveObject, IDynamicRootComponent
{
    private readonly IJSObjectReference jsObjectReference;

    internal DynamicRootComponent(IJSObjectReference jsObjectReference, string elementId, string componentIdentifier)
    {
        ElementId = elementId;
        ComponentIdentifier = componentIdentifier;
        this.jsObjectReference = jsObjectReference;
    }
    
    public string ElementId { get; }
    
    public string ComponentIdentifier { get; }

    public ValueTask SetParameters(object parameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        if (Anchors.IsDisposed)
        {
            throw new ObjectDisposedException($"Component is already disposed, elementId: {ElementId}, component identifier: {ComponentIdentifier}");
        }

        return jsObjectReference.InvokeVoidAsync("setParameters", parameters);
    }
    
    public async ValueTask DisposeAsync()
    {
        Anchors.DisposeJsSafe();
        await jsObjectReference.InvokeVoidSafeAsync("dispose");
        await jsObjectReference.DisposeJsSafeAsync();
    }
}
