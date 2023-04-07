using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using DynamicData.Binding;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Blazor;

public abstract class BlazorReactiveComponent : ComponentBase, IDisposableReactiveObject
{
    protected static readonly ConcurrentDictionary<Type, PropertyInfo[]> CollectionProperties = new();

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

public abstract class BlazorReactiveComponent<T> : BlazorReactiveComponent where T : IDisposableReactiveObject
{
    private static readonly IFluentLog Log = typeof(BlazorReactiveComponent<T>).PrepareLogger();

    protected BlazorReactiveComponent()
    {
        
    }

    public new T DataContext
    {
        get => (T) base.DataContext;
        set => base.DataContext = value;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        this.WhenAnyValue(x => x.DataContext)
            .Where(dataContext => dataContext != null)
            .Subscribe(dataContext =>
            {
                Log.Debug(() => $"Initializing instance of {this} ({GetType()})");
                
                var properties = CollectionProperties.GetOrAdd(dataContext.GetType(), type => type
                    .GetAllProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => typeof(INotifyCollectionChanged).IsAssignableFrom(x.PropertyType))
                    .Where(x => x.CanRead)
                    .ToArray());
                if (properties.Any())
                {
                    properties.Select(property =>
                        {
                            return dataContext
                                .WhenAnyProperty(property.Name)
                                .StartWithDefault()
                                .Select(_ => property.GetValue(dataContext) as INotifyCollectionChanged)
                                .Select(x => x.ObserveCollectionChanges())
                                .Switch()
                                .Select(x => new {property.Name, x.EventArgs.Action, x.EventArgs});
                        }).Merge()
                        .SubscribeSafe(async x =>
                        {
                            Log.Debug(() => $"Component collection has changed: {x.Name}, requesting redraw");
                            await InvokeAsync(StateHasChanged);
                            Log.Debug(() => $"Redraw completed after property change: {x.Name}");
                        }, Log.HandleException)
                        .AddTo(Anchors);
                }
        
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
}