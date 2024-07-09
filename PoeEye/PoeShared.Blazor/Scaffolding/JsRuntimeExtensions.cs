using System.IO;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace PoeShared.Blazor.Scaffolding;

public static class JsRuntimeExtensions
{
    public static async ValueTask<IJSObjectReference> ImportModule(this IJSRuntime jsRuntime, string modulePath)
    {
        var module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", modulePath);
        if (module == null)
        {
            throw new FileLoadException($"Failed to load JS module @ {modulePath}");
        }

        return module;
    }
}