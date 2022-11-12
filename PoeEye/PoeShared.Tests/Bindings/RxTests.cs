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
    [Repeat(100)]
    public void ShouldObserveInSerializedWayOnMaxProcessorCount()
    {
        ShouldObserveInSerializedWay(Environment.ProcessorCount, bgScheduler: true, synchronizePreScheduler: false, synchronizePostScheduler: false);
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

    private sealed class Fake : DisposableReactiveObject
    {
        public int Value { get; set; }
    }
}