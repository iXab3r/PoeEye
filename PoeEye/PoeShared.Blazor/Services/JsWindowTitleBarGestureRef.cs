using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using PoeShared.Blazor.Scaffolding;

namespace PoeShared.Blazor.Services;

/// <summary>
/// Represents an installed DOM gesture recognizer for a Blazor window title bar.
/// </summary>
public readonly record struct JsWindowTitleBarGestureRef : IAsyncDisposable
{
    private readonly IJSObjectReference? jsObjectReference;

    public JsWindowTitleBarGestureRef(IJSObjectReference jsObjectReference)
    {
        this.jsObjectReference = jsObjectReference ?? throw new ArgumentNullException(nameof(jsObjectReference));
    }

    public async ValueTask DisposeAsync()
    {
        if (jsObjectReference == null)
        {
            return;
        }

        await jsObjectReference.InvokeVoidSafeAsync("dispose");
        await jsObjectReference.DisposeJsSafeAsync();
    }
}
