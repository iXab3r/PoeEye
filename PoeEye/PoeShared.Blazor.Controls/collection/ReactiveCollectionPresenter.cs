using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PoeShared.Blazor;
using PoeShared.Blazor.Controls.Services;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Controls;

public partial class ReactiveCollectionPresenter<TItem, TKey> : BlazorReactiveComponent
    where TItem : notnull
    where TKey : notnull
{
    public static readonly string JsFilePath = "./_content/PoeShared.Blazor.Controls/js/ReactiveCollectionPresenter.js";

    private readonly SemaphoreSlim applyGate = new(1, 1);
    private readonly SerialDisposable frameSubscription = new();
    private readonly TaskCompletionSource<bool> sessionReadySource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly JsonSerializerOptions keySerializerOptions = new(JsonSerializerDefaults.Web);

    private Lazy<Task<IJSObjectReference>>? moduleTask;
    private IJSObjectReference? sessionReference;
    private ElementReference emptyStateElementRef;
    private Dictionary<string, ReactiveCollectionPresenterItemState<TItem, TKey>> itemsByTransportKey = new();
    private IObservable<ReactiveCollectionFrame<TItem, TKey>>? subscribedFrames;
    private TimeSpan subscribedBatchDelay;

    [Inject] internal IReactiveCollectionItemRegistry ItemRegistry { get; init; } = default!;

    [Parameter] public IObservable<ReactiveCollectionFrame<TItem, TKey>>? Frames { get; set; }

    [Parameter, EditorRequired] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Optional shell content shown when there are no visible items in the presented collection.
    /// This is owned by the presenter because emptiness is collection state; callers should not
    /// need an extra reactive wrapper just to toggle an empty-state message.
    /// </summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    [Parameter] public TimeSpan BatchDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Advanced entry point: the host element is separate from the rendered island and can be styled independently.
    /// The tag is stable for the lifetime of an item. Changing it requires a remove/add and is intentionally not supported by update ops.
    /// </summary>
    [Parameter] public string ItemTagName { get; set; } = "div";

    [Parameter] public string? ItemClass { get; set; }

    [Parameter] public string? ItemStyle { get; set; }

    [Parameter] public Func<TItem, string?>? ItemClassSelector { get; set; }

    [Parameter] public Func<TItem, string?>? ItemStyleSelector { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateFrameSubscription();
    }

    private readonly AtomicFlag isRendered = new AtomicFlag();

    protected override bool ShouldRender()
    {
        return isRendered.Set();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        moduleTask ??= new Lazy<Task<IJSObjectReference>>(() =>
            JsRuntime.InvokeAsync<IJSObjectReference>("import", JsFilePath).AsTask());

        try
        {
            var module = await moduleTask.Value;
            object? emptyStateElement = EmptyTemplate == null ? null : emptyStateElementRef;
            sessionReference = await module.InvokeAsync<IJSObjectReference>(
                "createSession",
                ElementRef,
                emptyStateElement,
                ReactiveCollectionItemHost.ComponentIdentifier);
            sessionReadySource.TrySetResult(true);
        }
        catch (Exception e)
        {
            sessionReadySource.TrySetException(e);
            throw;
        }
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            sessionReadySource.TrySetCanceled();
            frameSubscription.Dispose();

            if (sessionReference != null)
            {
                await sessionReference.InvokeVoidAsync("dispose");
                await sessionReference.DisposeJsSafeAsync();
                sessionReference = null;
            }

            if (moduleTask is { IsValueCreated: true })
            {
                var module = await moduleTask.Value;
                await module.DisposeJsSafeAsync();
            }
        }
        catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or InvalidOperationException)
        {
            // During shutdown/reload the circuit may already be gone.
        }
        finally
        {
            itemsByTransportKey.Clear();
            applyGate.Dispose();
        }

        await base.DisposeAsync();
    }

    private void UpdateFrameSubscription()
    {
        if (ReferenceEquals(subscribedFrames, Frames) && subscribedBatchDelay == BatchDelay)
        {
            return;
        }

        subscribedFrames = Frames;
        subscribedBatchDelay = BatchDelay;

        // This presenter is intentionally render-once after initialization, so the frame pipeline
        // must be updated explicitly from parameters rather than relying on incidental rerenders.
        frameSubscription.Disposable = CreateFrameBatchStream(Frames, BatchDelay)
            .Where(x => x.Count > 0)
            .SubscribeAsync(ApplyFrameBatchAsync);
    }

    private IObservable<IReadOnlyList<ReactiveCollectionFrame<TItem, TKey>>> CreateFrameBatchStream(
        IObservable<ReactiveCollectionFrame<TItem, TKey>>? frames,
        TimeSpan batchDelay)
    {
        if (frames == null)
        {
            return Observable.Empty<IReadOnlyList<ReactiveCollectionFrame<TItem, TKey>>>();
        }

        var nonEmptyFrames = frames.Where(x => x != null && x.HasOperations);
        return batchDelay > TimeSpan.Zero
            ? nonEmptyFrames.Buffer(batchDelay).Where(x => x.Count > 0).Select(x => (IReadOnlyList<ReactiveCollectionFrame<TItem, TKey>>)x)
            : nonEmptyFrames.Select(x => (IReadOnlyList<ReactiveCollectionFrame<TItem, TKey>>)new[] { x });
    }

    private async Task ApplyFrameBatchAsync(IReadOnlyList<ReactiveCollectionFrame<TItem, TKey>> frameBatch)
    {
        if (frameBatch.Count == 0)
        {
            return;
        }

        await applyGate.WaitAsync();
        try
        {
            await sessionReadySource.Task;

            if (sessionReference == null)
            {
                return;
            }

            var workingState = new Dictionary<string, ReactiveCollectionPresenterItemState<TItem, TKey>>(itemsByTransportKey);
            var jsFrames = new List<ReactiveCollectionJsFrame>(frameBatch.Count);

            foreach (var frame in frameBatch)
            {
                var jsOperations = new List<ReactiveCollectionJsOperation>(frame.Operations.Count);
                foreach (var operation in frame.Operations)
                {
                    TranslateOperation(operation, workingState, jsOperations);
                }

                if (jsOperations.Count > 0)
                {
                    jsFrames.Add(new ReactiveCollectionJsFrame
                    {
                        Operations = jsOperations
                    });
                }
            }

            if (jsFrames.Count == 0)
            {
                itemsByTransportKey = workingState;
                return;
            }

            await sessionReference.InvokeVoidAsync("applyFrames", jsFrames);
            itemsByTransportKey = workingState;
        }
        finally
        {
            applyGate.Release();
        }
    }

    private void TranslateOperation(
        ReactiveCollectionOperation<TItem, TKey> operation,
        Dictionary<string, ReactiveCollectionPresenterItemState<TItem, TKey>> workingState,
        List<ReactiveCollectionJsOperation> jsOperations)
    {
        switch (operation)
        {
            case ReactiveCollectionAddOperation<TItem, TKey> addOperation:
                TranslateAdd(addOperation, workingState, jsOperations);
                break;
            case ReactiveCollectionUpdateOperation<TItem, TKey> updateOperation:
                TranslateUpdate(updateOperation, workingState, jsOperations);
                break;
            case ReactiveCollectionMoveOperation<TItem, TKey> moveOperation:
                TranslateMove(moveOperation, jsOperations);
                break;
            case ReactiveCollectionRemoveOperation<TItem, TKey> removeOperation:
                TranslateRemove(removeOperation, workingState, jsOperations);
                break;
            case ReactiveCollectionClearOperation<TItem, TKey>:
                workingState.Clear();
                jsOperations.Add(new ReactiveCollectionJsOperation
                {
                    Kind = ReactiveCollectionJsOperationKind.Clear
                });
                break;
            case ReactiveCollectionResetOperation<TItem, TKey> resetOperation:
                TranslateReset(resetOperation, workingState, jsOperations);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation), operation, $"Unsupported collection operation {operation.GetType().Name}.");
        }
    }

    private void TranslateAdd(
        ReactiveCollectionAddOperation<TItem, TKey> operation,
        Dictionary<string, ReactiveCollectionPresenterItemState<TItem, TKey>> workingState,
        List<ReactiveCollectionJsOperation> jsOperations)
    {
        var transportKey = FormatKey(operation.Key);
        var beforeTransportKey = operation.BeforeKey == null ? null : FormatKey(operation.BeforeKey);
        var registration = ItemRegistry.Register(CreateItemContent(operation.Item));
        var hostMetadata = CreateHostMetadata(operation.Item);

        workingState[transportKey] = new ReactiveCollectionPresenterItemState<TItem, TKey>(
            operation.Key,
            operation.Item,
            registration.RegistrationId,
            registration.Version,
            hostMetadata.TagName,
            hostMetadata.ClassName,
            hostMetadata.Style);

        jsOperations.Add(new ReactiveCollectionJsOperation
        {
            Kind = ReactiveCollectionJsOperationKind.Add,
            Key = transportKey,
            BeforeKey = beforeTransportKey,
            RegistrationId = registration.RegistrationId,
            RegistrationVersion = registration.Version,
            TagName = hostMetadata.TagName,
            ClassName = hostMetadata.ClassName,
            Style = hostMetadata.Style
        });
    }

    private void TranslateUpdate(
        ReactiveCollectionUpdateOperation<TItem, TKey> operation,
        Dictionary<string, ReactiveCollectionPresenterItemState<TItem, TKey>> workingState,
        List<ReactiveCollectionJsOperation> jsOperations)
    {
        var transportKey = FormatKey(operation.Key);
        if (!workingState.TryGetValue(transportKey, out var existingItemState))
        {
            // Advanced callers may emit an update before the initial reset/add.
            // Converting that case to an add keeps the presenter usable without silently recreating existing roots.
            TranslateAdd(new ReactiveCollectionAddOperation<TItem, TKey>(operation.Key, operation.Item), workingState, jsOperations);
            return;
        }

        var registration = ItemRegistry.Update(existingItemState.RegistrationId, CreateItemContent(operation.Item));
        var hostMetadata = CreateHostMetadata(operation.Item);

        // The host tag is intentionally stable for the lifetime of an item.
        // If a selector starts returning a different tag for an existing item, we keep the original shell
        // so update and move operations can preserve the mounted Blazor island instance.
        workingState[transportKey] = existingItemState with
        {
            Item = operation.Item,
            RegistrationVersion = registration.Version,
            ClassName = hostMetadata.ClassName,
            Style = hostMetadata.Style
        };

        jsOperations.Add(new ReactiveCollectionJsOperation
        {
            Kind = ReactiveCollectionJsOperationKind.Update,
            Key = transportKey,
            RegistrationId = registration.RegistrationId,
            RegistrationVersion = registration.Version,
            ClassName = hostMetadata.ClassName,
            Style = hostMetadata.Style
        });
    }

    private void TranslateMove(
        ReactiveCollectionMoveOperation<TItem, TKey> operation,
        List<ReactiveCollectionJsOperation> jsOperations)
    {
        jsOperations.Add(new ReactiveCollectionJsOperation
        {
            Kind = ReactiveCollectionJsOperationKind.Move,
            Key = FormatKey(operation.Key),
            BeforeKey = operation.BeforeKey == null ? null : FormatKey(operation.BeforeKey)
        });
    }

    private void TranslateRemove(
        ReactiveCollectionRemoveOperation<TItem, TKey> operation,
        Dictionary<string, ReactiveCollectionPresenterItemState<TItem, TKey>> workingState,
        List<ReactiveCollectionJsOperation> jsOperations)
    {
        var transportKey = FormatKey(operation.Key);
        workingState.Remove(transportKey);
        jsOperations.Add(new ReactiveCollectionJsOperation
        {
            Kind = ReactiveCollectionJsOperationKind.Remove,
            Key = transportKey
        });
    }

    private void TranslateReset(
        ReactiveCollectionResetOperation<TItem, TKey> operation,
        Dictionary<string, ReactiveCollectionPresenterItemState<TItem, TKey>> workingState,
        List<ReactiveCollectionJsOperation> jsOperations)
    {
        workingState.Clear();

        var resetItems = new List<ReactiveCollectionJsItem>(operation.Items.Count);
        foreach (var itemSnapshot in operation.Items)
        {
            var transportKey = FormatKey(itemSnapshot.Key);
            var registration = ItemRegistry.Register(CreateItemContent(itemSnapshot.Item));
            var hostMetadata = CreateHostMetadata(itemSnapshot.Item);

            workingState[transportKey] = new ReactiveCollectionPresenterItemState<TItem, TKey>(
                itemSnapshot.Key,
                itemSnapshot.Item,
                registration.RegistrationId,
                registration.Version,
                hostMetadata.TagName,
                hostMetadata.ClassName,
                hostMetadata.Style);

            resetItems.Add(new ReactiveCollectionJsItem
            {
                Key = transportKey,
                RegistrationId = registration.RegistrationId,
                RegistrationVersion = registration.Version,
                TagName = hostMetadata.TagName,
                ClassName = hostMetadata.ClassName,
                Style = hostMetadata.Style
            });
        }

        jsOperations.Add(new ReactiveCollectionJsOperation
        {
            Kind = ReactiveCollectionJsOperationKind.Reset,
            Items = resetItems
        });
    }

    private RenderFragment CreateItemContent(TItem item)
    {
        if (ItemTemplate == null)
        {
            throw new InvalidOperationException($"{GetType().Name} requires {nameof(ItemTemplate)} to be set.");
        }

        return ItemTemplate(item);
    }

    private ReactiveCollectionHostMetadata CreateHostMetadata(TItem item)
    {
        var resolvedTagName = string.IsNullOrWhiteSpace(ItemTagName) ? "div" : ItemTagName;
        var resolvedClassName = ItemClassSelector?.Invoke(item) ?? ItemClass;
        var resolvedStyle = ItemStyleSelector?.Invoke(item) ?? ItemStyle;
        return new ReactiveCollectionHostMetadata(resolvedTagName, resolvedClassName, resolvedStyle);
    }

    private string FormatKey(TKey key)
    {
        return JsonSerializer.Serialize(key, keySerializerOptions);
    }

    private sealed record ReactiveCollectionPresenterItemState<TValue, TValueKey>(
        TValueKey Key,
        TValue Item,
        string RegistrationId,
        long RegistrationVersion,
        string TagName,
        string? ClassName,
        string? Style)
        where TValueKey : notnull;

    private readonly record struct ReactiveCollectionHostMetadata(string TagName, string? ClassName, string? Style);

    private sealed class ReactiveCollectionJsFrame
    {
        [JsonPropertyName("operations")]
        public required List<ReactiveCollectionJsOperation> Operations { get; init; }
    }

    private sealed class ReactiveCollectionJsItem
    {
        [JsonPropertyName("key")]
        public required string Key { get; init; }

        [JsonPropertyName("registrationId")]
        public required string RegistrationId { get; init; }

        [JsonPropertyName("registrationVersion")]
        public required long RegistrationVersion { get; init; }

        [JsonPropertyName("tagName")]
        public required string TagName { get; init; }

        [JsonPropertyName("className")]
        public string? ClassName { get; init; }

        [JsonPropertyName("style")]
        public string? Style { get; init; }
    }

    private sealed class ReactiveCollectionJsOperation
    {
        [JsonPropertyName("kind")]
        public required string Kind { get; init; }

        [JsonPropertyName("key")]
        public string? Key { get; init; }

        [JsonPropertyName("beforeKey")]
        public string? BeforeKey { get; init; }

        [JsonPropertyName("registrationId")]
        public string? RegistrationId { get; init; }

        [JsonPropertyName("registrationVersion")]
        public long? RegistrationVersion { get; init; }

        [JsonPropertyName("tagName")]
        public string? TagName { get; init; }

        [JsonPropertyName("className")]
        public string? ClassName { get; init; }

        [JsonPropertyName("style")]
        public string? Style { get; init; }

        [JsonPropertyName("items")]
        public List<ReactiveCollectionJsItem>? Items { get; init; }
    }

    private static class ReactiveCollectionJsOperationKind
    {
        public const string Add = "add";
        public const string Update = "update";
        public const string Move = "move";
        public const string Remove = "remove";
        public const string Clear = "clear";
        public const string Reset = "reset";
    }
}
