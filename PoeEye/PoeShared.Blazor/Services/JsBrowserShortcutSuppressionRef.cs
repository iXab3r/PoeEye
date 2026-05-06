using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using PoeShared.Blazor.Scaffolding;

namespace PoeShared.Blazor.Services;

/// <summary>
/// Represents an installed DOM browser-shortcut suppression listener.
/// </summary>
public readonly record struct JsBrowserShortcutSuppressionRef : IAsyncDisposable
{
    private readonly IJSObjectReference? jsObjectReference;

    public JsBrowserShortcutSuppressionRef(IJSObjectReference jsObjectReference)
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
