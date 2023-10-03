using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Blazor.Internals;

[Obsolete("Remove as it is super classed by binder-based version")]
internal sealed class InstanceChangeDetector<TContext> : DisposableReactiveObject
{
    public IFluentLog Log { get; }
    private readonly Subject<object> whenChanged = new();

    /// <summary>
    /// Tracks changes in objects referenced by this component
    /// FIXME Disabled by default - it is extremely inefficient and has some issues like stack overflow in some cases
    /// </summary>
    public bool TrackReferencedObjects { get; set; } = false;
    
    public bool TrackChanges { get; set; } = false;
    
    public TContext DataContext { get; set; }
    
    public IObservable<object> WhenChanged => whenChanged;

    public InstanceChangeDetector(IFluentLog log)
    {
        Log = log;
        var propertyUpdate = 
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.DataContext), 
                    this.WhenAnyValue(x => x.TrackReferencedObjects), (dataContext, trackNestedObjects) => (dataContext, trackNestedObjects))
                .Select(x => x.dataContext is INotifyPropertyChanged reactiveComponent ? ListenToObject(Log, reactiveComponent, x.trackNestedObjects) : Observable.Empty<EventPattern<PropertyChangedEventArgs>>())
                .Switch();
    }

    //FIXME Rewrite to ExpressionTrees, should be faster even with recent reflection optimizations
    private static readonly IDictionary<int, MethodInfo> ListenToMethodByParamCount = typeof(BlazorReactiveComponent)
        .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
        .Where(x => x.Name == nameof(ListenToPropertyChanges))
        .ToDictionary(x => x.GetGenericArguments().Length, x => x);
    
    protected static IObservable<EventPattern<PropertyChangedEventArgs>> ListenToPropertyChanges(object value, PropertyInfo propertyInfo)
    {
        if (value == null)
        {
            return Observable.Never<EventPattern<PropertyChangedEventArgs>>();
        }

        var valueType = value.GetType();
        if (!valueType.IsGenericType)
        {
            return Observable.Never<EventPattern<PropertyChangedEventArgs>>();
        }

        var genericTypeArguments = valueType.GetGenericArguments();
        if (!ListenToMethodByParamCount.TryGetValue(genericTypeArguments.Length, out var listenToMethod))
        {
            return Observable.Never<EventPattern<PropertyChangedEventArgs>>();
        }

        var genericMethod = listenToMethod.MakeGenericMethod(genericTypeArguments);
        var events = (IObservable<PropertyChangedEventArgs>) genericMethod.Invoke(null, new[] {value, propertyInfo});
        if (events == null)
        {
            throw new ArgumentException($"Something went really wrong - failed to get events using {genericMethod} from {value}");
        }

        return events.Select(x => new EventPattern<PropertyChangedEventArgs>(value, x));
    }

    [UsedImplicitly]
    protected static IObservable<PropertyChangedEventArgs> ListenToPropertyChanges<TParam, TParam2>(object value, PropertyInfo propertyInfo)
    {
        if (value is IObservableCache<TParam, TParam2> observableCache)
        {
            return observableCache.Connect()
                .Select(x => new PropertyChangedEventArgs(propertyInfo.Name));
        }

        return Observable.Never<PropertyChangedEventArgs>();
    }

    [UsedImplicitly]
    protected static IObservable<PropertyChangedEventArgs> ListenToPropertyChanges<TParam>(object value, PropertyInfo propertyInfo)
    {
        if (value is INotifyCollectionChanged notifyCollectionChanged)
        {
            return notifyCollectionChanged.ObserveCollectionChanges()
                .Select(x => new PropertyChangedEventArgs(propertyInfo.Name));
        }

        if (value is IObservableList<TParam> observableList)
        {
            return observableList.Connect()
                .Select(x => new PropertyChangedEventArgs(propertyInfo.Name));
        }

        return Observable.Never<PropertyChangedEventArgs>();
    }

    protected static IEnumerable<PropertyInfo> GetCompatibleProperties(Type type)
    {
        return type
            .GetAllProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.CanRead && !x.IsIndexedProperty());
    }
    
    protected static IObservable<EventPattern<PropertyChangedEventArgs>> ListenToObject(
        IFluentLog log, 
        INotifyPropertyChanged source,
        bool trackChildObjects)
    {
        return Observable.Create<EventPattern<PropertyChangedEventArgs>>(observer =>
        {
            var properties = GetCompatibleProperties(source.GetType()).ToArray();
            if (!properties.Any())
            {
                return Disposable.Empty;
            }

            var anchors = new CompositeDisposable();
            foreach (var property in properties)
            {
                var propertyListener = source.WhenAnyProperty(property.Name).Publish();
                propertyListener
                    .Subscribe(args =>
                    {
                        //log.Debug(() => $"Component property of {args.Sender} has changed: {args.EventArgs.PropertyName}, requesting redraw");
                        observer.OnNext(args);
                    })
                    .AddTo(anchors);
                
                var propertyValueListener = propertyListener
                    .StartWithDefault()
                    .Select(_ =>
                    {
                        try
                        {
                            var propertyValue = property.GetValue(source);
                            return propertyValue;
                        }
                        catch (Exception e)
                        {
                            throw new ArgumentException($"Failed to get value of property {property} of object {source} ({source.GetType()})", e);
                        }
                    })
                    .Publish();
                
                propertyValueListener
                    .Select(x => ListenToPropertyChanges(x, property))
                    .Switch()
                    .SubscribeSafe(args =>
                    {
                        //log.Debug(() => $"Component property of {args.Sender} has notified about changes: {args.EventArgs.PropertyName}, requesting redraw");
                        observer.OnNext(args);
                    }, log.HandleException)
                    .AddTo(anchors);

                if (trackChildObjects)
                {
                    propertyValueListener
                        .Select(x => x as INotifyPropertyChanged)
                        .Select(x => x != null ? ListenToObject(log, x, true) : Observable.Empty<EventPattern<PropertyChangedEventArgs>>())
                        .Switch()
                        .Subscribe(args =>
                        {
                            //log.Debug(() => $"Nested component property of {args.Sender} has changed: {args.EventArgs.PropertyName}, requesting redraw");
                            observer.OnNext(args);
                        })
                        .AddTo(anchors);
                }

                propertyListener.Connect().AddTo(anchors);
                propertyValueListener.Connect().AddTo(anchors);
            }

            return anchors;
        });
    }

    protected static IObservable<EventPattern<PropertyChangedEventArgs>> RaiseOnPropertyChanges(IFluentLog log, INotifyPropertyChanged source)
    {
        //log.Debug(() => $"Initializing reactive properties of {source}");
        return Observable.Create<EventPattern<PropertyChangedEventArgs>>(observer =>
        {
            var anchors = new CompositeDisposable();

            source.WhenAnyProperty()
                .SubscribeSafe(args =>
                {
                    //log.Debug(() => $"Component property of {args.Sender} has changed: {args.EventArgs.PropertyName}, requesting redraw");
                    observer.OnNext(args);
                }, log.HandleException)
                .AddTo(anchors);

            return anchors;
        });
    }
}