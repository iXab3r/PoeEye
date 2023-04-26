using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

    [Inject] public IJSRuntime JsRuntime { get; init; }

    protected BlazorReactiveComponent()
    {
    }
    
    public void Dispose()
    {
        Anchors.Dispose();
        GC.SuppressFinalize(this);
    }

    [Parameter] public object DataContext { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    public CompositeDisposable Anchors { get; } = new();

    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

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

        Observable.Merge(
                this.WhenAnyValue(x => x.DataContext)
                    .Select(x => x != null ? RaiseOnPropertyChanges(x) : Observable.Empty<PropertyChangedEventArgs>())
                    .Switch(),
                RaiseOnPropertyChanges(this)
            )
            .Sample(TimeSpan.FromMilliseconds(250))//FIXME UI throttling
            .Subscribe(() => InvokeAsync(StateHasChanged))
            .AddTo(Anchors);
    }

    private static IObservable<PropertyChangedEventArgs> RaiseOnPropertyChanges(INotifyPropertyChanged source)
    {
        Log.Debug(() => $"Initializing reactive properties of {source}");

        return Observable.Create<PropertyChangedEventArgs>(observer =>
        {
            var anchors = new CompositeDisposable();

            var properties = CollectionProperties.GetOrAdd(source.GetType(), GetReactiveProperties);
            if (properties.Any())
            {
                properties.Select(property =>
                    {
                        return source
                            .WhenAnyProperty(property.Name)
                            .StartWithDefault()
                            .Select(_ => property.GetValue(source) as INotifyCollectionChanged)
                            .Select(x => x.ObserveCollectionChanges())
                            .Switch()
                            .Select(x => new {property.Name, x.EventArgs.Action, x.EventArgs});
                    }).Merge()
                    .SubscribeSafe(x =>
                    {
                        //Log.Debug(() => $"Component collection has changed: {x.Name}, requesting redraw");
                        observer.OnNext(new PropertyChangedEventArgs(x.Name));
                        //Log.Debug(() => $"Redraw completed after property change: {x.Name}");
                    }, Log.HandleException)
                    .AddTo(anchors);
            }

            source.PropertyChanged += (sender, args) =>
            {
                //Log.Debug(() => $"Component property has changed: {args.PropertyName}, requesting redraw");
                observer.OnNext(args);
                //Log.Debug(() => $"Redraw completed after property change: {args.PropertyName}");
            };
            return anchors;
        });
    }

    private static PropertyInfo[] GetReactiveProperties(Type type)
    {
        return type
            .GetAllProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => typeof(INotifyCollectionChanged).IsAssignableFrom(x.PropertyType))
            .Where(x => x.CanRead)
            .ToArray();
    }
}