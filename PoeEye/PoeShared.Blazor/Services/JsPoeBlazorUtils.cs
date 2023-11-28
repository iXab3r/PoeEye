namespace PoeShared.Blazor.Services;

using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

internal sealed class JsPoeBlazorUtils : IJsPoeBlazorUtils
{
    public static readonly string JsUtilsFilePath = "./_content/PoeShared.Blazor/js/poeshared.utils.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;
    private readonly IJSRuntime jsRuntime;

    public JsPoeBlazorUtils(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            this.jsRuntime.InvokeAsync<IJSObjectReference>("import", JsUtilsFilePath).AsTask());
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        return await moduleTask.Value;
    }

    public async Task<string> GetClipboardText()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<string>("getClipboardText");
    }

    public async Task SetClipboardText(string text)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("setClipboardText", text);
    }

    public async Task ShowAlert(string message)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("showAlert", message);
    }
    
    public async Task SelectAllTextInElementById(string elementId)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("selectAllTextInElementById", elementId);
    }

    public async Task FocusElementById(string elementId)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("focusElementById", elementId);
    }
    
    public async Task ClickElementById(string elementId)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("clickElementById", elementId);
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
