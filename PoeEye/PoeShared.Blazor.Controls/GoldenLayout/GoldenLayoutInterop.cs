using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Blazor.Controls.Services;
using PoeShared.Blazor.Services;

namespace PoeShared.Blazor.Controls.GoldenLayout;

public interface IGoldenLayoutInterop
{
    ValueTask<IGoldenLayoutFacade> Create(ElementReference target);
}

public interface IGoldenLayoutHook
{
    IObservable<string> WhenFocused { get; }
}

internal class GoldenLayoutHook : IGoldenLayoutHook, IAsyncDisposable
{
    private readonly IGoldenLayoutFacade editorFacade;
    private readonly DotNetObjectReference<GoldenLayoutHook> facadeRef;
    private readonly Subject<string> whenFocused = new();

    private GoldenLayoutHook(IGoldenLayoutFacade editorFacade)
    {
        this.editorFacade = editorFacade;
        facadeRef = DotNetObjectReference.Create(this);
    }

    public IObservable<string> WhenFocused => whenFocused;

    public static async ValueTask<GoldenLayoutHook> Create(IGoldenLayoutFacade editorFacade)
    {
        var listener = new GoldenLayoutHook(editorFacade);
        await editorFacade.AddHook(listener.facadeRef);
        return listener;
    }

    public async ValueTask DisposeAsync()
    {
        await editorFacade.RemoveHook(facadeRef);
        facadeRef.DisposeJsSafe();
    }

    [JSInvokable]
    public void HandleFocusChanged(string componentId)
    {
        whenFocused.OnNext(componentId);
    }
}

public interface IGoldenLayoutFacade : IJSObjectReference
{
    ValueTask LoadLayout(object config);

    ValueTask RemoveItemById(string id);
    ValueTask<GLLocationInfo> AddBlazorChildItem(string parentId, GLBlazorComponentState state);
    ValueTask<GLLocationInfo> AddBlazorItem(GLBlazorComponentState state);
    ValueTask<GLLocationInfo> AddBlazorItemAtLocation(GLBlazorComponentState state, params GoldenLayoutLocationSelector[] selectors);

    ValueTask<GLLocationInfo> AddItem(GLNativeComponentState state);
    ValueTask<GLLocationInfo> AddChild(string parentId, GLNativeComponentState state);
    ValueTask<GLLocationInfo> AddItemAtLocation(GLNativeComponentState state, params GoldenLayoutLocationSelector[] selectors);

    IObservable<IGoldenLayoutHook> AddHook();
    ValueTask FocusById(string id);
    ValueTask AddHook<T>(DotNetObjectReference<T> hookRef) where T : class, IGoldenLayoutHook;
    ValueTask RemoveHook<T>(DotNetObjectReference<T> hookRef) where T : class, IGoldenLayoutHook;
}

public readonly record struct GLNativeComponentState
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public required string Query { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Title { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Id { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool? Closeable { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool? ReorderEnabled { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Size { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? MinSize { get; init; }
}

public readonly record struct GLBlazorComponentState
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public DynamicComponentId DynamicComponentId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public DynamicComponentId? TabDynamicComponentId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Title { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Id { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool? Closeable { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool? ReorderEnabled { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Size { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? MinSize { get; init; }
}

public readonly record struct GoldenLayoutLocationSelector
{
    public TypeId TypeId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int? Index { get; init; }
}

public readonly record struct GLLocationInfo
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string Id { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string ItemType { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int? Index { get; init; }
}

public enum TypeId
{
    /// <summary>
    /// Stack with focused Item. Index specifies offset from index of focused item (e.g., 1 is the position after focused item)
    /// </summary>
    FocusedItem = 0,

    /// <summary>
    /// Stack with focused Item. Index specifies ContentItem's index
    /// </summary>
    FocusedStack = 1,

    /// <summary>
    /// First stack found in layout
    /// </summary>
    FirstStack = 2,

    /// <summary>
    /// First Row or Column found in layout (rows are searched first)
    /// </summary>
    FirstRowOrColumn = 3,

    /// <summary>
    /// First Row in layout
    /// </summary>
    FirstRow = 4,

    /// <summary>
    /// First Column in layout
    /// </summary>
    FirstColumn = 5,

    /// <summary>
    /// Finds a location if layout is empty. The found location will be the root ContentItem.
    /// </summary>
    Empty = 6,

    /// <summary>
    /// Finds root if layout is empty, otherwise a child under root
    /// </summary>
    Root = 7
}

internal sealed class GoldenLayoutFacade : IGoldenLayoutFacade
{
    private readonly IJSObjectReference jsObjectReference;

    public GoldenLayoutFacade(IJSObjectReference jsObjectReference)
    {
        this.jsObjectReference = jsObjectReference;
    }

    public async ValueTask LoadLayout(object config)
    {
        await jsObjectReference.InvokeVoidAsync("loadLayout", config);
    }

    public async ValueTask RemoveItemById(string id)
    {
        await jsObjectReference.InvokeVoidAsync("removeItemById", id);
    }

    public async ValueTask<GLLocationInfo> AddBlazorChildItem(string parentId, GLBlazorComponentState state)
    {
        return await jsObjectReference.InvokeAsync<GLLocationInfo>("addBlazorChildItem", parentId, state);
    }

    public async ValueTask<GLLocationInfo> AddBlazorItem(GLBlazorComponentState state)
    {
        return await jsObjectReference.InvokeAsync<GLLocationInfo>("addBlazorItem", state);
    }

    public async ValueTask<GLLocationInfo> AddBlazorItemAtLocation(GLBlazorComponentState state, params GoldenLayoutLocationSelector[] selectors)
    {
        return await jsObjectReference.InvokeAsync<GLLocationInfo>("addBlazorItemAtLocation", state, selectors);
    }

    public async ValueTask<GLLocationInfo> AddItem(GLNativeComponentState state)
    {
        return await jsObjectReference.InvokeAsync<GLLocationInfo>("addItem", state);
    }

    public async ValueTask<GLLocationInfo> AddChild(string parentId, GLNativeComponentState state)
    {
        return await jsObjectReference.InvokeAsync<GLLocationInfo>("addChildItem", parentId, state);
    }

    public async ValueTask<GLLocationInfo> AddItemAtLocation(GLNativeComponentState state, params GoldenLayoutLocationSelector[] selectors)
    {
        return await jsObjectReference.InvokeAsync<GLLocationInfo>("addItemAtLocation", state, selectors);
    }

    public IObservable<IGoldenLayoutHook> AddHook()
    {
        return Observable.Create<IGoldenLayoutHook>(async observer =>
        {
            var hook = await GoldenLayoutHook.Create(this);
            observer.OnNext(hook);
          
            // ReSharper disable once AsyncVoidLambda
            return async () =>
            {
                await hook.DisposeJsSafeAsync();
            };
        });
    }

    public async ValueTask FocusById(string id)
    {
        await jsObjectReference.InvokeVoidAsync("focusById", id);
    }

    public async ValueTask AddHook<T>(DotNetObjectReference<T> hookRef) where T : class, IGoldenLayoutHook
    {
        await jsObjectReference.InvokeVoidAsync("addHook", hookRef);
    }

    public async ValueTask RemoveHook<T>(DotNetObjectReference<T> hookRef) where T : class, IGoldenLayoutHook
    {
        await jsObjectReference.InvokeVoidAsync("removeHook", hookRef);
    }

    public async ValueTask DisposeAsync()
    {
        await jsObjectReference.InvokeVoidSafeAsync("dispose");
        await jsObjectReference.DisposeJsSafeAsync();
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        return jsObjectReference.InvokeAsync<TValue>(identifier, args);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        return jsObjectReference.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }
}

internal sealed class GoldenLayoutInterop(IJSRuntime jsRuntime, IJsPoeBlazorUtils jsPoeBlazorUtils) : IAsyncDisposable, IGoldenLayoutInterop
{
    readonly Lazy<Task<IJSObjectReference>> moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PoeShared.Blazor.Controls/js/GoldenLayout.js").AsTask());

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeJsSafeAsync();
        }
    }

    public async ValueTask<IGoldenLayoutFacade> Create(ElementReference target)
    {
        await jsPoeBlazorUtils.LoadCss("./_content/PoeShared.Blazor.Controls/css/goldenlayout-base.css");
        await jsPoeBlazorUtils.LoadCss("./_content/PoeShared.Blazor.Controls/css/goldenlayout-dark-theme.css");

        var module = await moduleTask.Value;
        var jsRef = await module.InvokeAsync<IJSObjectReference>("create", target);
        return new GoldenLayoutFacade(jsRef);
    }
}
