using System;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PoeShared.Blazor.Scaffolding;
using ReactiveUI;

namespace PoeShared.Blazor.Services;

public readonly record struct ElementKeyboardHookRef : IAsyncDisposable
{
    private readonly IJsPoeBlazorUtils blazorUtils;
    private readonly CompositeDisposable anchors;

    private ElementKeyboardHookRef(
        IJsPoeBlazorUtils blazorUtils, 
        ElementReference elementRef,
        CompositeDisposable anchors)
    {
        ElementRef = elementRef;
        this.blazorUtils = blazorUtils;
        this.anchors = anchors;
    }
    
    public ElementReference ElementRef { get; init; }

    public static async ValueTask<ElementKeyboardHookRef> Create<THandler>(
        IJsPoeBlazorUtils blazorUtils,  
        ElementReference elementRef,
        THandler handler, 
        string methodName) where THandler : class
    {
        var dotnetRef = DotNetObjectReference.Create(handler);
        var anchors = new CompositeDisposable() {dotnetRef};
        await blazorUtils.AddKeyboardHook(elementRef, dotnetRef, methodName);
        return new ElementKeyboardHookRef(blazorUtils, elementRef, anchors);
    }

    public async ValueTask DisposeAsync()
    {
        if (anchors?.IsDisposed == false)
        {
            await blazorUtils.RemoveKeyboardHook(ElementRef);
        }
        anchors?.Dispose();
    }
}