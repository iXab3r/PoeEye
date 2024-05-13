using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using PoeShared.Logging;
using PoeShared.Scaffolding;

// ReSharper disable RedundantOverriddenMember Some extra code in this class is expected to help with debugging/hooking/IL rewriting
namespace PoeShared.Blazor;

/// <summary>
/// Represents the base class for a reactive component in a Blazor application.
/// </summary>
public abstract class ReactiveComponentBase : ComponentBase, IReactiveComponent
{
    private static long globalIdx;

    private readonly Lazy<IFluentLog> logSupplier;
    private long refreshRequestCount;
    private long refreshCount;
    private long renderCount;
    private long shouldRenderCount;
    private long unrenderedChangeCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveComponentBase"/> class.
    /// </summary>
    protected ReactiveComponentBase()
    {
        ComponentId = new ReactiveComponentId($"reactive-{Interlocked.Increment(ref globalIdx)}");

        logSupplier = new Lazy<IFluentLog>(PrepareLogger);
        
        WhenRefresh
            .Do(x => Interlocked.Increment(ref refreshRequestCount))
            .Sample(RefreshPeriod) //FIXME UI throttling
            .Do(reason =>
            {
                Interlocked.Increment(ref refreshCount);
                Interlocked.Increment(ref unrenderedChangeCount);
            })
            .SubscribeAsync(async reason => await Refresh(reason))
            .AddTo(Anchors);
    }
    
    /// <summary>
    /// Gets the unique identifier for the reactive component.
    /// </summary>
    public ReactiveComponentId ComponentId { get; } 

    /// <summary>
    /// Gets or sets the refresh period for the component.
    /// </summary>
    public TimeSpan RefreshPeriod { get; set; } = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Gets the subject that triggers when the component should refresh.
    /// </summary>
    public ISubject<object> WhenRefresh { get; } = new Subject<object>();

    /// <summary>
    /// Gets the logger for this component.
    /// </summary>
    protected IFluentLog Log => logSupplier.Value;
    
    /// <summary>
    /// Gets the total count of refreshes that have occurred.
    /// </summary>
    public long RefreshCount => refreshCount;
    
    /// <summary>
    /// Gets the total count of ShouldRender requests that have occurred.
    /// </summary>
    public long ShouldRenderCount => shouldRenderCount;
    
    /// <summary>
    /// Gets the total count of successful renders that have occurred.
    /// </summary>
    public long RenderCount => renderCount;
    
    /// <summary>
    /// Gets the count of refresh requests that have been made. Not all of them will be served - throttling/sampling, filtering, etc.
    /// </summary>
    public long RefreshRequestCount => refreshRequestCount;
    
    /// <summary>
    /// Gets or sets the name of the component.
    /// </summary>
    [Parameter]
    public string Name { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Gets the collection of disposables for this component.
    /// </summary>
    public CompositeDisposable Anchors { get; } = new();
    
    /// <summary>
    /// Indicates whether the component is loaded. Set after the first OnParametersSet call.
    /// </summary>
    public bool IsComponentLoaded { get; private set; }
    
    /// <summary>
    /// Indicates whether the component is rendered. Set after the first OnAfterRender call.
    /// </summary>
    public bool IsComponentRendered { get; private set; }
    
    /// <summary>
    /// Controls rendering process - if set, StateHasChanged will be called only when there is at least one change detected
    /// either by ChangeDetector or manually
    /// </summary>
    public bool RenderOnlyWhenChanged { get; set; }
    
    /// <summary>
    /// Disposes of the resources used by this component.
    /// </summary>
    public void Dispose()
    {
        Anchors.Dispose();
        GC.SuppressFinalize(this);
    }
    
    public virtual async ValueTask DisposeAsync()
    {
        Dispose();
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Prepares the logger for this component.
    /// </summary>
    protected virtual IFluentLog PrepareLogger()
    {
        return GetType().PrepareLogger().WithSuffix(ComponentId);
    }

    /// <summary>
    /// Refreshes the component state, equivalent of StateHasChanged but with additional information about reason(if possible).
    /// </summary>
    protected async Task Refresh(object tag = null)
    {
        await InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        Interlocked.Increment(ref shouldRenderCount);

        bool shouldRender;
        if (RenderOnlyWhenChanged)
        {
            shouldRender = unrenderedChangeCount > 0;
        }
        else
        {
            shouldRender = base.ShouldRender();
        }

        return shouldRender;
    }
    
    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        Interlocked.Increment(ref renderCount);
        Interlocked.Exchange(ref unrenderedChangeCount, 0);
        base.OnAfterRender(firstRender);
    }

    /// <summary>
    /// Method invoked first time the component has been rendered. Note that the component does
    /// not automatically re-render after the completion of any returned <see cref="Task"/>, because
    /// that would cause an infinite render loop.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    /// <remarks>
    /// The <see cref="OnAfterRender(bool)"/> and <see cref="OnAfterRenderAsync(bool)"/> lifecycle methods
    /// are useful for performing interop, or interacting with values received from <c>@ref</c>.
    /// </remarks>
    protected virtual async Task OnAfterFirstRenderAsync()
    {
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await OnAfterFirstRenderAsync();
            IsComponentRendered = true;
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    /// <inheritdoc />
    protected override Task OnInitializedAsync()
    {
        return base.OnInitializedAsync();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        IsComponentLoaded = true;
    }

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override Task OnParametersSetAsync()
    {
        return base.OnParametersSetAsync();
    }

    /// <summary>
    /// Raises the PropertyChanged event for a given property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Raises a property changed event if the specified field is changed.
    /// Sets the field to the new value if changed.
    /// </summary>
    /// <typeparam name="TRet">The type of the property.</typeparam>
    /// <param name="backingField">The field backing the property.</param>
    /// <param name="newValue">The new value for the property.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The new value.</returns>
    [UsedImplicitly]
    protected TRet RaiseAndSetIfChanged<TRet>(ref TRet backingField,
        TRet newValue,
        [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
        {
            return newValue;
        }

        return RaiseAndSet(ref backingField, newValue, propertyName);
    }

    protected TRet RaiseAndSet<TRet>(
        [SuppressMessage("ReSharper", "RedundantAssignment", Justification = "Expected - initial value is not checked in this method")] ref TRet backingField,
        TRet newValue,
        [CallerMemberName] string propertyName = null)
    {
        backingField = newValue;
        RaisePropertyChanged(propertyName);
        return newValue;
    }
}