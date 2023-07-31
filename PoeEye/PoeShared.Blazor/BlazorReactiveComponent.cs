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
using PoeShared.Logging;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Blazor;

public abstract class BlazorReactiveComponent : BlazorReactiveComponentBase
{
    //FIXME Rewrite to ExpressionTrees, should be faster even with recent reflection optimizations
    private static readonly IDictionary<int, MethodInfo> ListenToMethodByParamCount = typeof(BlazorReactiveComponent)
        .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
        .Where(x => x.Name == nameof(ListenTo))
        .ToDictionary(x => x.GetGenericArguments().Length, x => x);
    
    protected static IObservable<EventPattern<PropertyChangedEventArgs>> ListenTo(object value, PropertyInfo propertyInfo)
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

    protected static IObservable<PropertyChangedEventArgs> ListenTo<TParam, TParam2>(object value, PropertyInfo propertyInfo)
    {
        if (value is IObservableCache<TParam, TParam2> observableCache)
        {
            return observableCache.Connect()
                .Select(x => new PropertyChangedEventArgs(propertyInfo.Name));
        }

        return Observable.Never<PropertyChangedEventArgs>();
    }

    protected static IObservable<PropertyChangedEventArgs> ListenTo<TParam>(object value, PropertyInfo propertyInfo)
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
            .Where(x => x.CanRead);
    }
}

public abstract class BlazorReactiveComponent<T> : BlazorReactiveComponent
{
    private readonly Subject<object> whenChanged = new();

    public new T DataContext
    {
        get => (T) base.DataContext;
        set => base.DataContext = value;
    }

    public IObservable<object> WhenChanged => whenChanged;

    protected BlazorReactiveComponent()
    {
        Observable.Merge(
                RaiseOnPropertyChanges(Log, this),
                this.WhenAnyValue(x => x.DataContext)
                    .Select(x => x is INotifyPropertyChanged reactiveComponent ? RaiseOnChanges(Log, reactiveComponent) : Observable.Empty<EventPattern<PropertyChangedEventArgs>>())
                    .Switch(),
                this.WhenAnyValue(x => x.DataContext)
                    .Select(x => x is INotifyPropertyChanged reactiveComponent ? RaiseOnPropertyChanges(Log, reactiveComponent) : Observable.Empty<EventPattern<PropertyChangedEventArgs>>())
                    .Switch(),
                this.WhenAnyValue(x => x.DataContext)
                    .Select(x => x is IRefreshableComponent refreshableComponent ? refreshableComponent.WhenRefresh : Observable.Empty<object>())
                    .Switch()
            )
            .Subscribe(x => { whenChanged.OnNext(x); })
            .AddTo(Anchors);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        whenChanged.Subscribe(WhenRefresh).AddTo(Anchors);
    }

    private static IObservable<EventPattern<PropertyChangedEventArgs>> RaiseOnChanges(IFluentLog log, INotifyPropertyChanged source)
    {
        log.Debug(() => $"Initializing reactive collections of {source}");
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
                source.WhenAnyProperty(property.Name)
                    .StartWithDefault()
                    .Select(x => property.GetValue(source))
                    .Select(x => ListenTo(x, property))
                    .Switch()
                    .SubscribeSafe(args =>
                    {
                        //log.Debug(() => $"Component property of {args.Sender} has changed: {args.EventArgs.PropertyName}, requesting redraw");
                        observer.OnNext(args);
                    }, log.HandleException)
                    .AddTo(anchors);
            }

            return anchors;
        });
    }

    private static IObservable<EventPattern<PropertyChangedEventArgs>> RaiseOnPropertyChanges(IFluentLog log, INotifyPropertyChanged source)
    {
        log.Debug(() => $"Initializing reactive properties of {source}");
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