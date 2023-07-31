using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace PoeShared.Blazor;

public abstract class ReactiveComponentBase : ComponentBase, IReactiveComponent
{
    private static long GlobalIdx;

    private readonly Lazy<IFluentLog> logSupplier;

    protected ReactiveComponentBase()
    {
        ObjectId = $"Cmp#{Interlocked.Increment(ref GlobalIdx)}";

        logSupplier = new Lazy<IFluentLog>(PrepareLogger);
        
        WhenRefresh
            .Sample(RefreshPeriod) //FIXME UI throttling
            .SubscribeAsync(async x => await Refresh()).AddTo(Anchors);
    }

    public TimeSpan RefreshPeriod { get; set; } = TimeSpan.FromMilliseconds(100);

    public ISubject<object> WhenRefresh { get; } = new Subject<object>();

    protected IFluentLog Log => logSupplier.Value;
    
    protected string ObjectId { get; }

    protected virtual IFluentLog PrepareLogger()
    {
        return GetType().PrepareLogger().WithSuffix(ObjectId);
    }

    public void Dispose()
    {
        Anchors.Dispose();
        GC.SuppressFinalize(this);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public CompositeDisposable Anchors { get; } = new();

    protected async Task Refresh()
    {
        await InvokeAsync(StateHasChanged);
    }

    protected override bool ShouldRender()
    {
        return base.ShouldRender();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        return base.OnAfterRenderAsync(firstRender);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected override Task OnInitializedAsync()
    {
        return base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        return base.SetParametersAsync(parameters);
    }

    protected override Task OnParametersSetAsync()
    {
        return base.OnParametersSetAsync();
    }

    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

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
        ref TRet backingField,
        TRet newValue,
        [CallerMemberName] string propertyName = null)
    {
        backingField = newValue;
        RaisePropertyChanged(propertyName);
        return newValue;
    }
}