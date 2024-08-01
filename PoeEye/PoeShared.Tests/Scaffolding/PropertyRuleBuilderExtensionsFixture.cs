using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using PoeShared.Modularity;
using PropertyBinder;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class PropertyRuleBuilderExtensionsFixture : FixtureBase
{
    [Test]
    [Timeout(1000)]
    public void ShouldHandleCounter()
    {
        //Given
        var hasTicked = new ManualResetEventSlim();
        var instance = new TestClass();
        var mainThread = Environment.CurrentManagedThreadId;

        var binder = new Binder<TestClass>();
        
        binder.Bind(x => x.Counter).To((x, v) =>
        {
            Log.Info($"Tick: {v}");
            Environment.CurrentManagedThreadId.ShouldBe(mainThread);
            hasTicked.Set();
        });
        
        //When
        using var anchor = binder.Attach(instance);

        instance.Counter = 1;

        //Then
        instance.Counter.ShouldBe(1);
        hasTicked.Wait(100);
    }
    
    [Test]
    [Timeout(1000)]
    public void ShouldHandleCounterOnScheduler()
    {
        //Given
        var hasTicked = new ManualResetEventSlim();
        var instance = new TestClass();

        var binder = new Binder<TestClass>();

        var scheduler = SchedulerProvider.Instance.CreateDispatcherScheduler($"Test thread for {nameof(ShouldHandleCounterOnScheduler)}", ThreadPriority.Normal);
        
        binder.Bind(x => x.Counter)
            .OnScheduler(_ => scheduler)
            .To((x, v) =>
        {
            Log.Info($"Tick: {v}");
            Environment.CurrentManagedThreadId.ShouldBe(scheduler.Dispatcher.Thread.ManagedThreadId);
            hasTicked.Set();
        });
        
        //When
        using var anchor = binder.Attach(instance);

        instance.Counter = 1;

        //Then
        instance.Counter.ShouldBe(1);
        hasTicked.Wait(100);
    }
    
    [Test]
    [Timeout(1000)]
    [Ignore("Could not reach stability for this one")]
    public void ShouldHandleCancellationOnScheduler()
    {
        //the idea is to make binder execute at a very specific moment when dispatcher has started shutting down
        //but did not complete yet, i.e. is still accepts new items, but there is a chance that it will cancel them
        
        //Given
        var hasTicked = new ManualResetEventSlim(false);
        var startSignal = new ManualResetEventSlim(false);
        var instance = new TestClass();
        var mainThread = Environment.CurrentManagedThreadId;

        var binder = new Binder<TestClass>();

        var scheduler = SchedulerProvider.Instance.CreateDispatcherScheduler($"Test thread for {nameof(ShouldHandleCancellationOnScheduler)} - {Guid.NewGuid()}", ThreadPriority.Normal);

        binder.Bind(x => x.Counter)
            .OnScheduler(_ => scheduler)
            .To((x, v) =>
            {
                Environment.CurrentManagedThreadId.ShouldNotBe(mainThread);
                Environment.CurrentManagedThreadId.ShouldBe(scheduler.Dispatcher.Thread.ManagedThreadId);
                Log.Info($"Tick: {v}");
                hasTicked.Set();
            });
        
        //When
        scheduler.Dispatcher.ShutdownStarted += (sender, args) =>
        {
            Log.Info("Dispatcher is shutting down");
        };
        
        scheduler.Dispatcher.ShutdownFinished += (sender, args) =>
        {
            Log.Info("Dispatcher is shut down");
            startSignal.Set();
            Thread.Sleep(100);
        };
        
        Task.Run(() =>
        {
            startSignal.Wait();
            binder.Attach(instance);
        });
        
        scheduler.Dispatcher.InvokeShutdown();

        //Then
        Log.Info("Awaiting for tick");
        hasTicked.Wait(110).ShouldBeFalse();
    }
    
    [Test]
    public void ShouldHandleCancellation()
    {
        //Given
        

        //When


        //Then

    }

    private sealed class TestClass : DisposableReactiveObject
    {
        public int Counter { get; set; }
    }
}