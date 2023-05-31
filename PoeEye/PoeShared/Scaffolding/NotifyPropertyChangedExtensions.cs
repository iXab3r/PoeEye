using System.ComponentModel;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using DynamicData;
using JetBrains.Annotations;
using ReactiveUI;

namespace PoeShared.Scaffolding;

public static class NotifyPropertyChangedExtensions
{
    private static readonly IFluentLog Log = typeof(NotifyPropertyChangedExtensions).PrepareLogger();

    public static IObservable<EventPattern<PropertyChangedEventArgs>> WhenAnyProperty<TObject>([NotNull] this TObject instance, params string[] propertiesToMonitor)
        where TObject : INotifyPropertyChanged
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }
            
        return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                handler => instance.PropertyChanged += handler,
                handler => instance.PropertyChanged -= handler
            )
            .Where(x => propertiesToMonitor == null || propertiesToMonitor.Length == 0 || propertiesToMonitor.Contains(x.EventArgs.PropertyName));
    }
        
    public static string GetPropertyName<TObject, T1>([NotNull] this TObject instance,
        Expression<Func<TObject, T1>> instancePropertyExtractor)
        where TObject : INotifyPropertyChanged
    {
        return Reflection.ExpressionToPropertyNames(instancePropertyExtractor.Body);
    }

    public static IObservable<EventPattern<PropertyChangedEventArgs>> WhenAnyProperty<TObject, T1>([NotNull] this TObject instance,
        Expression<Func<TObject, T1>> instancePropertyExtractor)
        where TObject : INotifyPropertyChanged
    {
        return WhenAnyProperty(instance, GetPropertyName(instance, instancePropertyExtractor));
    }
        
    public static IObservable<EventPattern<PropertyChangedEventArgs>> WhenAnyProperty<TObject, T1, T2>([NotNull] this TObject instance,
        Expression<Func<TObject, T1>> ex1,
        Expression<Func<TObject, T2>> ex2)
        where TObject : INotifyPropertyChanged
    {
        return WhenAnyProperty(
            instance, 
            Reflection.ExpressionToPropertyNames(ex1.Body),
            Reflection.ExpressionToPropertyNames(ex2.Body));
    }
        
    public static IObservable<EventPattern<PropertyChangedEventArgs>> WhenAnyProperty<TObject, T1, T2, T3>([NotNull] this TObject instance,
        Expression<Func<TObject, T1>> ex1,
        Expression<Func<TObject, T2>> ex2,
        Expression<Func<TObject, T3>> ex3)
        where TObject : INotifyPropertyChanged
    {
        return WhenAnyProperty(
            instance, 
            Reflection.ExpressionToPropertyNames(ex1.Body),
            Reflection.ExpressionToPropertyNames(ex2.Body),
            Reflection.ExpressionToPropertyNames(ex3.Body));
    }
        
    public static IObservable<EventPattern<PropertyChangedEventArgs>> WhenAnyProperty<TObject, T1, T2, T3, T4>([NotNull] this TObject instance,
        Expression<Func<TObject, T1>> ex1,
        Expression<Func<TObject, T2>> ex2,
        Expression<Func<TObject, T3>> ex3,
        Expression<Func<TObject, T4>> ex4)
        where TObject : INotifyPropertyChanged
    {
        return WhenAnyProperty(
            instance, 
            Reflection.ExpressionToPropertyNames(ex1.Body),
            Reflection.ExpressionToPropertyNames(ex2.Body),
            Reflection.ExpressionToPropertyNames(ex3.Body),
            Reflection.ExpressionToPropertyNames(ex4.Body));
    }
        
    public static IObservable<EventPattern<PropertyChangedEventArgs>> WhenAnyProperty<TObject, T1, T2, T3, T4, T5>([NotNull] this TObject instance,
        Expression<Func<TObject, T1>> ex1,
        Expression<Func<TObject, T2>> ex2,
        Expression<Func<TObject, T3>> ex3,
        Expression<Func<TObject, T4>> ex4,
        Expression<Func<TObject, T5>> ex5)
        where TObject : INotifyPropertyChanged
    {
        return WhenAnyProperty(
            instance, 
            Reflection.ExpressionToPropertyNames(ex1.Body),
            Reflection.ExpressionToPropertyNames(ex2.Body),
            Reflection.ExpressionToPropertyNames(ex3.Body),
            Reflection.ExpressionToPropertyNames(ex4.Body),
            Reflection.ExpressionToPropertyNames(ex5.Body));
    }

    public static void WaitForValue<TObject, T1>(
        this TObject instance, 
        Expression<Func<TObject, T1>> ex1,
        Predicate<T1> condition,
        TimeSpan timeout)
        where TObject : INotifyPropertyChanged
    {
        WaitForValueAsync(instance, ex1, condition, timeout).Wait();
    }
    
    public static Task<T1> WaitForAsync<TObject, T1>(
        this TObject instance, 
        Func<TObject, T1> extractor,
        Predicate<T1> condition,
        TimeSpan timeout)
        where TObject : INotifyPropertyChanged
    {
        var source = Observable.Timer(DateTimeOffset.Now, timeout / 5).Select(_ => extractor.Invoke(instance));
        if (timeout <= TimeSpan.Zero)
        {
            return source
                .Select(x => condition(x) == false ? throw new TimeoutException($"Value {x} does not satisfy condition") : x)
                .Take(1)
                .ToTask();
        }

        var reactiveResult = source.Where(x => condition(x)).Take(1);
        if (timeout < TimeSpan.MaxValue)
        {
            var timeoutSource = Observable
                .Return(default(T1))
                .Delay(timeout)
                .Select(_ => Observable.Throw<T1>(new TimeoutException($"Value did not satisfy condition in {timeout}")))
                .Switch();
            reactiveResult = reactiveResult.Amb(timeoutSource);
        }

        return reactiveResult.ToTask();
    }
        
    public static Task<T1> WaitForValueAsync<TObject, T1>(
        this TObject instance, 
        Expression<Func<TObject, T1>> ex1,
        Predicate<T1> condition,
        TimeSpan timeout)
        where TObject : INotifyPropertyChanged
    {
        var extractor = ex1.Compile();

        var source = Observable.Merge(
                instance.WhenAnyValue(ex1), 
                Observable.Return(Unit.Default).Select(_ => extractor.Invoke(instance)));
            
        if (timeout <= TimeSpan.Zero)
        {
            return source
                .Select(x => condition(x) == false ? throw new TimeoutException($"Value {x} does not satisfy condition") : x)
                .Take(1)
                .ToTask();
        }

        var reactiveResult = source.Where(x => condition(x)).Take(1);
        if (timeout < TimeSpan.MaxValue)
        {
            var timeoutSource = Observable
                .Return(default(T1))
                .Delay(timeout)
                .Select(_ => Observable.Throw<T1>(new TimeoutException($"Value did not satisfy condition in {timeout}")))
                .Switch();
            reactiveResult = reactiveResult.Amb(timeoutSource);
        }

        return reactiveResult.ToTask();
    }
        
    public static bool TryWaitForValue<TObject, T1>(
        this TObject instance, 
        Expression<Func<TObject, T1>> ex1,
        Predicate<T1> condition,
        TimeSpan timeout)
        where TObject : INotifyPropertyChanged
    {
        try
        {
            WaitForValue(instance, ex1, condition, timeout);
            return true;
        }
        catch (AggregateException)
        {
            return false;
        }
        catch (TimeoutException)
        {
            return false;
        };
    }
}