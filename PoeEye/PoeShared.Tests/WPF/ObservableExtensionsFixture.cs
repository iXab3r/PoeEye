using NUnit.Framework;
using AutoFixture;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
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

        Log.Debug(() => $"Pushing value");
        source.OnNext(1);
        Log.Debug(() => $"Pushed value");

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
            Log.Debug(() => $"Pushing value");
            source.OnNext(1);
            Log.Debug(() => $"Pushed value");
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
            Log.Debug(() => $"Pushing value");
            source.OnNext(1);
            Log.Debug(() => $"Pushed value");
        });

        //Then
        Log.Debug($"Awaiting for result");
        completed.WaitOne();
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
            .TimeoutAfter(TimeSpan.FromMilliseconds(100));

        //Then
        action.ShouldNotThrow();
    }

    private sealed record TestClass : ReactiveRecord
    {
        public int Value { get; set; }
    }
}