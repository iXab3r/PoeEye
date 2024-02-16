using NUnit.Framework;
using AutoFixture;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.Tests.Helpers;
using Shouldly;

namespace PoeShared.Tests.WPF;

[TestFixture]
public class ObservableExtensionsFixture : FixtureBase
{
    private SchedulerProvider schedulerProvider;

    protected override void SetUp()
    {
        schedulerProvider = new SchedulerProvider();
    }

    [Test]
    [Timeout(1000)]
    public void ShouldObserveOnIfNotOnDispatcherThread()
    {
        //Given
        var source = new Subject<int>();
        var completed = new ManualResetEvent(false);
        var dispatcher = PrepareDispatcher(out var dispatcherThread);

        //When
        source.ObserveOnIfNeeded(dispatcher).Subscribe(x =>
        {
            Log.Debug($"onNext: {x}");
            Thread.CurrentThread.ShouldBeSameAs(dispatcherThread);
            completed.Set();
        });
        ;

        Log.Debug($"Pushing value");
        source.OnNext(1);
        Log.Debug($"Pushed value");

        //Then
        Log.Debug($"Awaiting for result");
        completed.WaitOne();
    }

    [Test]
    [Timeout(1000)]
    public void ShouldObserveOnIfOnDispatcherThreadViaBegin()
    {
        //Given
        var source = new Subject<int>();
        var completed = new ManualResetEvent(false);
        var dispatcher = PrepareDispatcher(out var dispatcherThread);

        //When
        source.ObserveOnIfNeeded(dispatcher).Subscribe(x =>
        {
            Log.Debug($"onNext: {x}");
            Thread.CurrentThread.ShouldBeSameAs(dispatcherThread);
            completed.Set();
        });

        dispatcher.BeginInvoke(() =>
        {
            Log.Debug($"Pushing value");
            source.OnNext(1);
            Log.Debug($"Pushed value");
        });

        //Then
        Log.Debug($"Awaiting for result");
        completed.WaitOne();
    }

    [Test]
    [Timeout(1000)]
    public void ShouldObserveOnIfOnDispatcherThreadViaInvoke()
    {
        //Given
        var source = new Subject<int>();
        var completed = new ManualResetEvent(false);
        var dispatcher = PrepareDispatcher(out var dispatcherThread);

        //When
        source.ObserveOnIfNeeded(dispatcher).Subscribe(x =>
        {
            Log.Debug($"onNext: {x}");
            Thread.CurrentThread.ShouldBeSameAs(dispatcherThread);
            completed.Set();
        });

        dispatcher.Invoke(() =>
        {
            Log.Debug($"Pushing value");
            source.OnNext(1);
            Log.Debug($"Pushed value");
        });

        //Then
        Log.Debug($"Awaiting for result");
        completed.WaitOne();
    }

    [Test]
    [Theory]
    public void ShouldObserveOnDispatcherSequence(bool useObserveOn)
    {
        //Given
        var source = new Subject<int>();

        var completed = new ManualResetEvent(false);
        var start = new ManualResetEvent(false);
        var dispatcher = PrepareDispatcher(out var dispatcherThread);

        //When
        var task = Task.Run(() =>
        {
            start.WaitOne();
            Log.Debug($"Started");
            for (int i = 0; i < 1000; i++)
            {
                source.OnNext(i);
            }
            completed.Set();
            Log.Debug($"All done");
        });

        var counter = 0;
        (useObserveOn 
                ? PoeShared.Scaffolding.DispatcherObservable.ObserveOn(source, dispatcher) 
                : source.ObserveOnIfNeeded(dispatcher))
            .Subscribe(x =>
            {
                Log.Debug($"onNext: {x}");
                counter++;
                Thread.CurrentThread.ShouldBeSameAs(dispatcherThread);

            });

        //Then
        start.Set();
        completed.WaitOne();
        task.Wait();
        counter.ShouldBecome(x => counter, 1000, timeout: 5000);
    }

    private Dispatcher PrepareDispatcher(out Thread dispatcherThread)
    {
        Dispatcher dispatcher = default;
        var dispatcherReady = new ManualResetEventSlim(false);
        dispatcherThread = new Thread(() =>
        {
            Log.Debug($"Starting dispatcher");
            dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            Log.Debug($"Dispatcher is ready");
            dispatcherReady.Set();
            Dispatcher.Run();
        })
        {
            Name = "Dispatcher",
            IsBackground = true
        };
        dispatcherThread.Start();

        Log.Debug($"Awaiting for dispatcher");
        dispatcherReady.Wait();
        return dispatcher;
    }

    [Test]
    [Repeat(100)]
    public async Task ShouldWaitForValue()
    {
        //Given
        var holder = new TestClass() {Value = 1};

        //When
        Log.Debug("Setting the value");
        var rng = new Random(0xF0CC);
        Task.Run(() =>
        {
            var sleep = rng.Next(0, 1);
            Thread.Sleep(sleep);
            holder.Value = 2;
        });

        Log.Debug("Awaiting for value");
        var action = async () => await holder
            .WaitForValueAsync(x => x.Value, x => x == 2, TimeSpan.FromSeconds(1))
            .WithTimeout(TimeSpan.FromMilliseconds(100));

        //Then
        action.ShouldNotThrow();
    }

    private sealed record TestClass : ReactiveRecord
    {
        public int Value { get; set; }
    }
}