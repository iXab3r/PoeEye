using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Blazor.Wpf;

public abstract class BlazorReactiveComponent : ComponentBase, IDisposableReactiveObject
{
    [Inject]
    public IJSRuntime JsRuntime { get; init; }
    
    public void Dispose()
    {
        Anchors.Dispose();
        GC.SuppressFinalize(this);
    }
    
    public object DataContext { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
    
    public CompositeDisposable Anchors { get; } = new();
    
    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public abstract class BlazorReactiveComponent<T> : BlazorReactiveComponent where T : DisposableReactiveComponent
{
    private static readonly IFluentLog Log = typeof(BlazorReactiveComponent<T>).PrepareLogger();

    protected BlazorReactiveComponent()
    {
        this.WhenAnyValue(x => x.DataContext)
            .Where(dataContext => dataContext != null)
            .Subscribe(dataContext =>
            {
                Log.Debug(() => $"Initializing instance of {this} ({GetType()})");
        
                dataContext.JsRuntime = JsRuntime;
                dataContext.PropertyChanged += async (sender, args) =>
                {
                    Log.Debug(() => $"Component property has changed: {args.PropertyName}, requesting redraw");
                    await InvokeAsync(StateHasChanged);
                    Log.Debug(() => $"Redraw completed after property change: {args.PropertyName}");
                };
                Log.Debug(() => $"Initialized instance of {this} ({GetType()})");
            })
            .AddTo(Anchors);
    }

    public new T DataContext
    {
        get => (T) base.DataContext;
        set => base.DataContext = value;
    }
}