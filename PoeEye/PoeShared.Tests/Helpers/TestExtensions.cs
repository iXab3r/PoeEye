using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using ReactiveUI;
using Shouldly;

namespace PoeShared.Tests.Helpers;

public static class TestExtensions
{
    private static readonly IFluentLog Log = typeof(TestExtensions).PrepareLogger();

    public static EventArgs Raises(object raiser, string eventName, Action function)
    {
        return Raises<EventArgs>(raiser, eventName, function);
    }

    public static T Raises<T>(object raiser, string eventName, Action function) where T:EventArgs
    {
        var listener = new EventListener<T>(raiser, eventName);
        function.Invoke();
        listener.SavedArgs.Count.ShouldBe(1);
        return listener.SavedArgs[0];
    }

    public static IList<T> RaisesMany<T>(object raiser, string eventName, Action function) where T : EventArgs
    {
        var listener = new EventListener<T>(raiser, eventName);
        function.Invoke();
        return listener.SavedArgs;
    }

    public static void ValueShouldNotBecome<T, T1>(this T instance, Expression<Func<T, T1>> extractor, T1 expected, int timeout = 1000) where T : INotifyPropertyChanged
    {
        var sw = Stopwatch.StartNew();
        try
        {
            instance.ValueShouldBecome(extractor, expected, timeout);
        }
        catch (Exception e)
        {
            if (e is TimeoutException or AggregateException { InnerException: TimeoutException })
            {
                return;
            }

            throw;
        }

        var elapsed = sw.ElapsedMilliseconds;
        throw new TimeoutException($"Value {extractor} should have not been {expected}, but became in {elapsed}ms (timeout {timeout})");
    }

    public static void ShouldBecome<T, T1>(this T instance, Func<T, T1> extractor, T1 expected, int timeout = 1000)
    {
        var log = Log.WithSuffix(instance).WithSuffix(extractor.ToString()).WithSuffix($"Expected: {expected}");

        var sw = Stopwatch.StartNew();

        while (sw.ElapsedMilliseconds < timeout)
        {
            var value = extractor(instance);
            if (EqualityComparer<T1>.Default.Equals(value, expected))
            {
                break;
            }
        }
        var finalValue = extractor(instance);
        finalValue.ShouldBe(expected);
    }

    public static void ValueShouldBecome<T, T1>(this T instance, Expression<Func<T, T1>> extractor, T1 expected, int timeout = 1000) 
        where T : INotifyPropertyChanged
    {
        T1 latest = default;
        var log = Log.WithSuffix(instance).WithSuffix(extractor.ToString()).WithSuffix($"Expected: {expected}");

        instance.WhenAnyValue(extractor)
            .Subscribe(x => log.Debug(() => $"Value raised for {x}"));
        
        try
        {
            log.Debug(() => $"Awaiting for value change");
            instance.WaitForValue(extractor, x =>
            {
                latest = x;
                var equals = EqualityComparer<T1>.Default.Equals(x, expected);
                log.Debug(() => $"Value updated to {x} (isEqual: {equals})");
                return equals;
            }, TimeSpan.FromMilliseconds(timeout));
        }
        catch (Exception e)
        {
            if (e is TimeoutException or AggregateException { InnerException: TimeoutException })
            {
                throw new TimeoutException($"Value {extractor} of {instance} should've been {expected}, but was {latest} in {timeout}ms", e);
            }

            throw;
        }
    }
}