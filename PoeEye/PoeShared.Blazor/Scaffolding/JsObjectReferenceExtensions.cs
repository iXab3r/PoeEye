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
    
    /// <summary>
    /// Invokes the specified JavaScript function asynchronously with JS exception handling.
    /// <para>
    /// <see cref="T:Microsoft.JSInterop.JSRuntime" /> will apply timeouts to this operation based on the value configured in <see cref="P:Microsoft.JSInterop.JSRuntime.DefaultAsyncTimeout" />. To dispatch a call with a different, or no timeout,
    /// consider using <see cref="M:Microsoft.JSInterop.IJSObjectReference.InvokeAsync``1(System.String,System.Threading.CancellationToken,System.Object[])" />.
    /// </para>
    /// </summary>
    /// <param name="jsObjectReference">The <see cref="IJSObjectReference"/>.</param>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>someScope.someFunction</c> on the target instance.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An instance of <typeparamref name="TValue" /> obtained by JSON-deserializing the return value.</returns>
    public static async ValueTask InvokeVoidSafeAsync<TValue>(this IJSObjectReference jsObjectReference, string identifier, params object?[]? args)
    {
        try
        {
            await jsObjectReference.InvokeAsync<TValue>(identifier, args);
        }
        catch (Exception e) when (e.IsJSException())
        {
            // During disposal ignore such errors because there is a chance that browser context is already disposed
        }
    }
}