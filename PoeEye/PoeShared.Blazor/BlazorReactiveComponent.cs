using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using DynamicData.Binding;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Blazor;

public abstract class BlazorReactiveComponent : BlazorReactiveComponentBase
{
}

public abstract class BlazorReactiveComponent<T> : BlazorReactiveComponent where T : IDisposableReactiveObject
{
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
                    .Select(x => x != null ? RaiseOnPropertyChanges(Log, x) : Observable.Empty<PropertyChangedEventArgs>())
                    .Switch(),
                RaiseOnPropertyChanges(Log, this)
            )
            .Sample(TimeSpan.FromMilliseconds(250)) //FIXME UI throttling
            .Subscribe(() => InvokeAsync(StateHasChanged))
            .AddTo(Anchors);
    }

    private static IObservable<PropertyChangedEventArgs> RaiseOnPropertyChanges(IFluentLog log, INotifyPropertyChanged source)
    {
        log.Debug(() => $"Initializing reactive properties of {source}");

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
                    }, log.HandleException)
                    .AddTo(anchors);
            }

            source.PropertyChanged += (_, args) =>
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