using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using PoeShared.Blazor.Scaffolding;

namespace PoeShared.Blazor.Services;

using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

internal sealed class JsPoeBlazorUtils : IJsPoeBlazorUtils
{
    public static readonly string JsUtilsFilePath = "./_content/PoeShared.Blazor/js/blazorUtils.js";

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
    
    public async Task SelectAllTextInElement(ElementReference elementRef)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("selectAllTextInElement", elementRef);
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

    public Task ScrollToTopById(string elementId, TimeSpan duration)
    {
        throw new NotImplementedException();
    }

    public async Task ScrollToTop(string elementSelector)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("scrollToTop", elementSelector);
    }

    public async Task ScrollToBottom(string elementSelector)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("scrollToBottom", elementSelector);
    }

    public async Task LoadScript(string scriptPath)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("loadScript", scriptPath);
    }
    
    public async Task LoadCss(string cssPath)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("loadCss", cssPath);
    }

    public async Task<IDynamicRootComponent> AddRootComponent(string elementId, string componentIdentifier, object initialParameters = default)
    {
        var module = await GetModuleAsync();
        var dynamicComponentRef = await module.InvokeAsync<IJSObjectReference>("addRootComponent", elementId, componentIdentifier, initialParameters);
        return new DynamicRootComponent(dynamicComponentRef, elementId, componentIdentifier);
    }

    public async Task AddKeyboardHook<THandler>(ElementReference elementRef, DotNetObjectReference<THandler> dotNetObjectReference, string methodName) where THandler : class
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("addKeyboardHook", elementRef, dotNetObjectReference, methodName);
    }
    
    public async Task RemoveKeyboardHook(ElementReference elementRef)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("removeKeyboardHook", elementRef);
    }

    public async Task<ElementKeyboardHookRef> AddKeyboardHook<THandler>(ElementReference elementRef, THandler handler, string methodName) where THandler : class
    {
        var hook = await ElementKeyboardHookRef.Create(this, elementRef, handler, methodName);
        return hook;
    }

    public async Task ScrollToTop(string elementSelector, TimeSpan duration)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("scrollToTop", elementSelector, (int)duration.TotalMilliseconds);
    }

    public async Task ScrollToBottom(string elementSelector, TimeSpan duration)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("scrollToBottom", elementSelector, (int)duration.TotalMilliseconds);
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
