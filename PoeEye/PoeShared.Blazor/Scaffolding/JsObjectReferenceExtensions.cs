using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace PoeShared.Blazor.Scaffolding;

public static class JsObjectReferenceExtensions
{
    /// <summary>
    /// Invokes the specified JavaScript function asynchronously with JS exception handling.
    /// </summary>
    /// <param name="jsObjectReference">The <see cref="IJSObjectReference"/>.</param>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>someScope.someFunction</c> on the target instance.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous invocation operation.</returns>
    public static async ValueTask InvokeVoidSafeAsync(this IJSObjectReference jsObjectReference, string identifier, params object?[]? args)
    {
        try
        {
            await jsObjectReference.InvokeVoidAsync(identifier, args);
        }
        catch (Exception e) when (e.IsJSException())
        {
            // During disposal ignore such errors because there is a chance that browser context is already disposed
        }
    }
}