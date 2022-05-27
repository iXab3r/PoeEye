using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using PoeShared.Logging;
using PoeShared.Tests.Helpers;
using PropertyChanged;
using ReactiveUI;

namespace PoeShared.Tests.Bindings;

[TestFixture]
public class RxTests : FixtureBase
{

    [Test]
    [Ignore("Proves that there is race condition in WhenAnyValue")]
    [Repeat(1000)]
    public void ShouldHaveRaceConditionInsideWhenAny()
    {
        //Given
        var fake = new Fake();

        var consumer = new ConcurrentQueue<int>();

        var task = Task.Run(() =>
        {
            fake.Value = 1;
        });

        //When
        fake.WhenAnyValue(x => x.Value)
            .Subscribe(consumer.Enqueue);

        //Then
        task.Wait();
        consumer.ShouldContain(1);
    }
    
    [Test]
    [TestCase(4)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    public void ShouldProcessCombineLatestInSerializedWay(int max)
    {
        //Given
        var sink1 = new Subject<int>();
        var sink2 = new Subject<string>();
        var consumer = new ConcurrentQueue<string>();
        var allStart = new ManualResetEventSlim(false);

        Observable.CombineLatest(sink1, sink2, (i, s) => new {i, s})
            .Subscribe(x =>
            {
                Log.Debug($"Adding {x}");
                consumer.Enqueue($"{x} enter");
                Thread.Sleep(1);
                consumer.Enqueue($"{x} exit");
                Log.Debug($"Added {x}");
            });

        //When
        var tasks1 = Enumerable.Range(0, max)
            .Select(x => Task.Factory.StartNew(() =>
            {
                allStart.Wait();
                Log.Debug($"Producing {x}");
                sink1.OnNext(x);
                Log.Debug($"Produced {x}");
            }));
        
        var tasks2 = Enumerable.Range(0, max)
            .Select(x => Task.Factory.StartNew(() =>
            {
                allStart.Wait();
                Log.Debug($"Producing {x}");
                sink2.OnNext($"#{x}");
                Log.Debug($"Produced {x}");
            }));

        //Then
        allStart.Set();
        Task.WaitAll(tasks1.ToArray());
        Task.WaitAll(tasks2.ToArray());
        consumer.ShouldBecome(x => x.Count, max * 2, timeout: max * 20);
        var resultingItems = consumer.ToArray();
        for (var i = 0; i < resultingItems.Length; i += 2)
        {
            resultingItems[i].ShouldContain("enter");
            resultingItems[i + 1].ShouldContain("exit");
        }
    }
    
    [Test]
    [TestCase(4)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    public void ShouldProcessMergeInSerializedWay(int max)
    {
        //Given
        var sink1 = new Subject<int>();
        var sink2 = new Subject<string>();
        var consumer = new ConcurrentQueue<string>();
        var allStart = new ManualResetEventSlim(false);

        Observable.Merge(sink1.Select(x => x.ToString()), sink2)
            .Subscribe(x =>
            {
                Log.Debug($"Adding {x}");
                consumer.Enqueue($"{x} enter");
                Thread.Sleep(1);
                consumer.Enqueue($"{x} exit");
                Log.Debug($"Added {x}");
            });

        //When
        var tasks1 = Enumerable.Range(0, max)
            .Select(x => Task.Factory.StartNew(() =>
            {
                allStart.Wait();
                Log.Debug($"Producing {x}");
                sink1.OnNext(x);
                Log.Debug($"Produced {x}");
            }));
        
        var tasks2 = Enumerable.Range(0, max)
            .Select(x => Task.Factory.StartNew(() =>
            {
                allStart.Wait();
                Log.Debug($"Producing {x}");
                sink2.OnNext($"#{x}");
                Log.Debug($"Produced {x}");
            }));

        //Then
        allStart.Set();
        Task.WaitAll(tasks1.ToArray());
        Task.WaitAll(tasks2.ToArray());
        consumer.ShouldBecome(x => x.Count, max * 4, timeout: max * 20);
        var resultingItems = consumer.ToArray();
        for (var i = 0; i < resultingItems.Length; i += 2)
        {
            resultingItems[i].ShouldContain("enter");
            resultingItems[i + 1].ShouldContain("exit");
        }
    }
    
    [Test]
    [TestCase(8, false, true, false)]
    [TestCase(8, false, false, true)]
    [TestCase(8, true, false, false)]
    [TestCase(32, false, true, false)]
    [TestCase(32, false, false, true)]
    [TestCase(32, true, false, false)]
    public void ShouldObserveInSerializedWay(
        int max,
        bool bgScheduler,
        bool synchronizePreScheduler,
        bool synchronizePostScheduler)
    {
        //Given
        var sink = new Subject<int>();
        var consumer = new ConcurrentQueue<string>();
        var allStart = new ManualResetEventSlim(false);
        IObservable<int> producer = sink;
        if (synchronizePreScheduler)
        {
            producer = producer.Synchronize();
        }

        if (bgScheduler)
        {
            producer = producer.ObserveOn(Scheduler.TaskPool);
        }

        if (synchronizePostScheduler)
        {
            producer = producer.Synchronize();
        }

        producer
            .Subscribe(x =>
            {
                Log.Debug($"Adding {x}");
                consumer.Enqueue($"{x} enter");
                Thread.Sleep(1);
                consumer.Enqueue($"{x} exit");
                Log.Debug($"Added {x}");
            });

        //When
        var tasks = Enumerable.Range(0, max)
            .Select(x => Task.Factory.StartNew(() =>
            {
                allStart.Wait();
                Log.Debug($"Producing {x}");
                sink.OnNext(x);
                Log.Debug($"Produced {x}");
            }));

        //Then
        allStart.Set();
        Task.WaitAll(tasks.ToArray());
        consumer.ShouldBecome(x => x.Count, max * 2, timeout: max * 20);
        var resultingItems = consumer.ToArray();
        for (var i = 0; i < resultingItems.Length; i += 2)
        {
            resultingItems[i].ShouldContain("enter");
            resultingItems[i + 1].ShouldContain("exit");
        }
    }

    [Test]
    [Repeat(10000)]
    public async Task WhenAnyShouldBeThreadSafe()
    {
        var anyReactiveObject = new Fake() { Value = 1 };
        var startEvent = new ManualResetEventSlim(false);
        var task = Task.Run(() =>
        {
            Log.Debug($"Waiting");
            startEvent.Wait();
            Log.Debug($"Assigning");
            anyReactiveObject.Value = 2;
            Log.Debug($"Assigned");
        });
        var received = new ConcurrentQueue<int>();
        anyReactiveObject
            .WhenAnyValue(x => x.Value)
            .Subscribe(received.Enqueue);
        Log.Debug($"Setting");
        startEvent.Set();
        await task;

        received.Count.ShouldBe(2);
        received.CollectionSequenceShouldBe(1, 2);
    }
    
    [Test]
    [Retry(10000)]
    public async Task ShouldHaveRaceConditionDuringEventSubscriptionViaWhenPropertyChanged()
    {
        //Given
        var itemsSource = new SourceListEx<Fake>();
        var item = new Fake(){ Value = 1 }.AddTo(itemsSource);

        var received = new ConcurrentQueue<int>();
        var startEvent = new ManualResetEventSlim(false);
        IDisposable anchor;
        var subscriber = Task.Run(() =>
        {
            Log.Debug($"Waiting");
            startEvent.Wait();
            Log.Debug($"Subscribing");
            anchor = item
                .WhenPropertyChanged(y => y.Value, notifyOnInitialValue: true)
                .Subscribe(x =>
                {
                    Log.Debug($"Received {x.Value}");
                    received.Enqueue(x.Value);
                });
            Log.Debug($"Subscribed");
        });
        
        var setter = Task.Run(() =>
        {
            Log.Debug($"Waiting");
            startEvent.Wait();
            Log.Debug($"Assigning");
            item.Value = 2;
            Log.Debug($"Assigned");
        });
        
        //When
        startEvent.Set();
        Task.WaitAll(subscriber, setter);

        //Then
        item.Value.ShouldBe(2);
        Assert.That(received.Last() != 2); // this shows the problem, it must be 2 in all cases !
    }
    
    [Test]
    [Retry(10000)]
    [TestCase(true)]
    [TestCase(false)]
    public async Task ShouldHaveRaceConditionInNpc(bool expectToSucceeed)
    {
        //Given
        var itemsSource = new SourceListEx<Fake>();
        var item = new Fake(){ Value = 1 }.AddTo(itemsSource);

        var received = new ConcurrentQueue<int>();
        var startEvent = new ManualResetEventSlim(false);
        IDisposable anchor;
        var subscriber = Task.Run(() =>
        {
            Log.Debug($"Waiting");
            startEvent.Wait();
            Log.Debug($"Subscribing");

            var eventSubscription = Observable.Defer(() =>
            {
                return item
                    .WhenAnyPropertyChanged(nameof(Fake.Value))
                    .Select(x => x.Value)
                    .Do(x => Log.Debug($"via NPC: {x}"));
            });

            var initialValue =
                Observable
                    .Defer(() => Observable.Return(item.Value))
                    .Do(x => Log.Debug($"via Defer: {x}"))
                    .Take(1);
            
            anchor = initialValue.Concat(eventSubscription)
                .Subscribe(x =>
                {
                    Log.Debug($"Received {x}");
                    received.Enqueue(x);
                });
            Log.Debug($"Subscribed");
        });
        
        var setter = Task.Run(() =>
        {
            Log.Debug($"Waiting");
            startEvent.Wait();
            Log.Debug($"Assigning");
            item.Value = 2;
            Log.Debug($"Assigned");
        });
        
        //When
        startEvent.Set();
        Task.WaitAll(subscriber, setter);

        //Then
        Log.Debug(() => "Asserting");
        item.Value.ShouldBe(2);
        Assert.That(received.LastOrDefault() == 2 == expectToSucceeed); // this shows the problem, it must be 2 in all cases !
    }
    
    [Test]
    [Repeat(30000)]
    public async Task ShouldSupportThreadSafetyOnAutoRefreshWhenSubscribedFirst()
    {
        //Given
        var itemsSource = new SourceListEx<Fake>();
        var item1 = new Fake(){ Value = 1 }.AddTo(itemsSource);
        itemsSource
            .Connect()
            .AutoRefreshOnObservable(item => item.WhenPropertyChanged(y => y.Value, notifyOnInitialValue: true))
            .Filter(item => item.Value == 2)
            .BindToCollection(out var items)
            .Subscribe();
        var startEvent = new ManualResetEventSlim(false);
        
        var setter = Task.Run(() =>
        {
            Log.Debug($"Waiting");
            startEvent.Wait();
            Log.Debug($"Assigning");
            item1.Value = 2;
            Log.Debug($"Assigned");
        });
        
        //When
        startEvent.Set();
        Task.WaitAll(setter);

        //Then
        item1.Value.ShouldBe(2);
        items.Count.ShouldBe(1);
    }

    private sealed class Fake : DisposableReactiveObject
    {
        public int Value { get; set; }
    }
}