using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsHook;
using log4net;
using Microsoft.VisualBasic.Logging;
using Moq;
using NUnit.Framework;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using Shouldly;

namespace PoeShared.Tests.Native;

[TestFixture]
public class KeyboardMouseEventsProviderFixture
{
    private static readonly IFluentLog Log = typeof(KeyboardMouseEventsProviderFixture).PrepareLogger();

    private Mock<IFactory<IKeyboardMouseEvents>> globalEventsFactory;
    private Mock<IFactory<IKeyboardMouseEvents>> appEventsFactory;
        
    [SetUp]
    public void SetUp()
    {
        globalEventsFactory = new Mock<IFactory<IKeyboardMouseEvents>>();
        appEventsFactory = new Mock<IFactory<IKeyboardMouseEvents>>();
    }

    [Test]
    public void ShouldHandleUsingCorrectly()
    {
        //Given
        var resource = new SharedResource();
        var source = Observable
            .Using(() => resource.Create(), Observable.Never)
            .Publish()
            .RefCount();

        //When
        using var subscription = source.Subscribe();

        //Then
        resource.Subcriptions.ShouldBe(1);
    }

    [Test]
    public void ShouldNotHookWithoutSubscriptions()
    {
        //Given
        var instance = CreateInstance();

        //When
        //Then
        globalEventsFactory.Verify(x => x.Create(), Times.Never);
        appEventsFactory.Verify(x => x.Create(), Times.Never);
    }
        
    [Test]
    public void ShouldHookOnceSubscriptions()
    {
        //Given
        var instance = CreateInstance();

        //When
        instance.System.Subscribe();
            
        //Then
        globalEventsFactory.Verify(x => x.Create(), Times.Once);
    }
        
    [Test]
    public void ShouldDisposeHookOnUnsub()
    {
        //Given
        var hook = new Mock<IKeyboardMouseEvents>();
        globalEventsFactory.Setup(x => x.Create()).Returns(hook.Object);
        var instance = CreateInstance();

        //When
        var anchor = instance.System.Subscribe();
        anchor.Dispose();
            
        //Then
        globalEventsFactory.Verify(x => x.Create(), Times.Once);
        hook.Verify(x => x.Dispose(), Times.Once);
    }
        
    [Test]
    public void ShouldNotDisposeHookOnUnsubIfAnySubLeft()
    {
        //Given
        var hook1 = new Mock<IKeyboardMouseEvents>();
        var hook2 = new Mock<IKeyboardMouseEvents>();
        var hooks = new Queue<Mock<IKeyboardMouseEvents>>();
        hooks.Enqueue(hook1);
        hooks.Enqueue(hook2);
            
        globalEventsFactory.Setup(x => x.Create()).Returns(() => hooks.Dequeue().Object);
        var instance = CreateInstance();

        //When
        var anchor1 = instance.System.Subscribe();
        var anchor2 = instance.System.Subscribe();

        anchor1.Dispose();
            
        //Then
        globalEventsFactory.Verify(x => x.Create(), Times.Once);
        hook1.Verify(x => x.Dispose(), Times.Never);
        hook2.Verify(x => x.Dispose(), Times.Never);
    }
        
    [Test]
    public void ShouldDisposeHookOnUnsubIfNoSubLeft()
    {
        //Given
        var hook1 = new Mock<IKeyboardMouseEvents>();
        var hook2 = new Mock<IKeyboardMouseEvents>();
        var hooks = new Queue<Mock<IKeyboardMouseEvents>>();
        hooks.Enqueue(hook1);
        hooks.Enqueue(hook2);
            
        globalEventsFactory.Setup(x => x.Create()).Returns(() => hooks.Dequeue().Object);
        var instance = CreateInstance();

        //When
        var anchor1 = instance.System.Subscribe();
        anchor1.Dispose();
        var anchor2 = instance.System.Subscribe();
            
        //Then
        globalEventsFactory.Verify(x => x.Create(), Times.Exactly(2));
        hook1.Verify(x => x.Dispose(), Times.Once);
        hook2.Verify(x => x.Dispose(), Times.Never);
    }
        
    [Test]
    public void ShouldHookOnceSubscriptionsWhenMultiple()
    {
        //Given
        var instance = CreateInstance();

        //When
        instance.System.Subscribe();
        instance.System.Subscribe();
            
        //Then
        globalEventsFactory.Verify(x => x.Create(), Times.Once);
    }

    [Test]
    [Timeout(60000)]
    [Repeat(100)]
    public void ShouldHandleMultithreadedSubscriptions()
    {
        //Given
        var instance = CreateInstance();
 
        var start = new ManualResetEvent(false);
        var hooksCount = 8;
        var wrappersCreated = Enumerable.Range(0, hooksCount).Select(x => new ManualResetEvent(false)).ToArray();
        var wrapperDone = Enumerable.Range(0, hooksCount).Select(x => new ManualResetEvent(false)).ToArray();

        Task.Run(() =>
        {
            Log.Debug(() => $"Creating {hooksCount} hooks");

            Enumerable.Range(0, hooksCount).AsParallel()
                .WithDegreeOfParallelism(hooksCount)
                .ForAll(idx =>
                {
                    Log.Debug("Creating hook");
                    var func = new Func<IDisposable>(() => instance.System.Subscribe());
                    wrappersCreated[idx].Set();
                    Log.Debug("Awaiting for start");
                    start.WaitOne();
                    Log.Debug("Subbing");
                    func();
                    wrapperDone[idx].Set();
                    Log.Debug("Done");
                });
        });

        //When
        Log.Debug("Awaiting for all wrappers to be ready");
        WaitHandle.WaitAll(wrappersCreated);
        Log.Debug("Sending signal to all wrappers");
        start.Set();
        Log.Debug("Awaiting for all wrappers to be complete");
        WaitHandle.WaitAll(wrapperDone);

        //Then
        globalEventsFactory.Verify(x => x.Create(), Times.Once);
    }
        
    [Test]
    public void ShouldReturnNewResourceOnNewSubIfPreviousDisposed()
    {
        //Given
        var hook1 = new Mock<IKeyboardMouseEvents>();
        var hook2 = new Mock<IKeyboardMouseEvents>();
        var hooks = new Queue<Mock<IKeyboardMouseEvents>>();
        hooks.Enqueue(hook1);
        hooks.Enqueue(hook2);
            
        globalEventsFactory.Setup(x => x.Create()).Returns(() => hooks.Dequeue().Object);
        var instance = CreateInstance();

        //When
        IKeyboardMouseEvents events1 = null;
        IKeyboardMouseEvents events2 = null;
        var anchor1 = instance.System.Subscribe(x => events1 = x);
        anchor1.Dispose();
        var anchor2 = instance.System.Subscribe(x => events2 = x);

        //Then
        globalEventsFactory.Verify(x => x.Create(), Times.Exactly(2));
        events1.ShouldBeSameAs(hook1.Object);
        events2.ShouldBeSameAs(hook2.Object);
    }

    [Test]
    public void ShouldReturnExistingResourceOnNewSub()
    {
        //Given
        var hook1 = new Mock<IKeyboardMouseEvents>();
        var hook2 = new Mock<IKeyboardMouseEvents>();
        var hooks = new Queue<Mock<IKeyboardMouseEvents>>();
        hooks.Enqueue(hook1);
        hooks.Enqueue(hook2);
            
        globalEventsFactory.Setup(x => x.Create()).Returns(() => hooks.Dequeue().Object);
        var instance = CreateInstance();

        //When
        IKeyboardMouseEvents events1 = null;
        IKeyboardMouseEvents events2 = null;
        var anchor1 = instance.System.Subscribe(x => events1 = x);
        var anchor2 = instance.System.Subscribe(x => events2 = x);

        //Then
        globalEventsFactory.Verify(x => x.Create(), Times.Once);
        events1.ShouldBeSameAs(hook1.Object);
        events2.ShouldBeSameAs(events1);
    }
        
    private sealed class SharedResource
    {
        private int subcriptions = 0;
                
        public SharedResource()
        {
        }

        public int Subcriptions => subcriptions;

        public IDisposable Create()
        {
            var updatedSubscriptions = Interlocked.Increment(ref subcriptions);
            Log.Debug(() => $"Resource created, count: {updatedSubscriptions}");
            return Disposable.Create(() =>
            {
                var updatedSubscriptions = Interlocked.Decrement(ref subcriptions);
                Log.Debug(() => $"Resource Disposed, count: {updatedSubscriptions}");
            });
        }
    }

    private KeyboardMouseEventsProvider CreateInstance()
    {
        return new(globalEventsFactory: globalEventsFactory.Object, appEventsFactory: appEventsFactory.Object, Scheduler.Immediate);
    }
}