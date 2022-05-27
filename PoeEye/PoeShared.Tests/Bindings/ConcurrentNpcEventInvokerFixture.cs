using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using NUnit.Framework;
using PoeShared.Tests.Helpers;
using ReactiveUI;

namespace PoeShared.Tests.Bindings;

[TestFixture]
[Ignore("Very poorly reproduces on build-server due to low number of cores")]
public class ConcurrentNpcEventInvokerFixture : FixtureBase
{
    [Test]
    [Retry(100000)]
    [TestCase(true)]
    [TestCase(false)]
    public async Task ShouldHaveRaceConditionInNpcViaEvents(bool expectToSucceeed)
    {
        //Given
        var item = new FakeViaDefaultNpc() {Value = 1};

        // When
        var received = RecordInvocationsObsolete(item);
        
        // Then
        Log.Debug(() => $"Asserting");
        item.Value.ShouldBe(2);
        Assert.That(received == 2 == expectToSucceeed); // this shows the problem, it must be 2 in all cases !
    }
    
    [Test]
    [Repeat(10000)]
    [TestCase(typeof(FakeViaReactiveObject))]
    [TestCase(typeof(FakeViaThreadSafeNpc))]
    public async Task ShouldNotHaveRaceConditionInNpcViaEvents(Type fakeType)
    {
        //Given
        var item = (IFake)Activator.CreateInstance(fakeType);
        item.Value = 1;

        // When
        var received = RecordInvocationsObsolete(item);
        
        // Then
        Log.Debug(() => $"Asserting");
        item.Value.ShouldBe(2);
        received.ShouldBe(2);
    }
    
    [Test]
    [TestCase(typeof(FakeViaDefaultNpc))]
    [TestCase(typeof(FakeViaReactiveObject))]
    [TestCase(typeof(FakeViaThreadSafeNpc))]
    [Repeat(10)]
    public void ShouldProcessSingularAssignmentWithSubscriber(Type fakeType)
    {
        //Given
        var item = (IFake)Activator.CreateInstance(fakeType);
        var items = new ConcurrentQueue<int>();

        item.WhenAnyValue(x => x.Value).Subscribe(items.Enqueue);
        
        //When
        var itemsToAdd = Enumerable.Range(0, 100000).ToArray();
        itemsToAdd.ForEach(x => item.Value = x);

        //Then
        items.CollectionSequenceShouldBe(itemsToAdd);
    }

    private int RecordInvocationsObsolete(IFake item)
    {
        var received = new ConcurrentQueue<int>();
        var startEvent = new ManualResetEventSlim(false);
        IDisposable anchor;
        var subscriber = Task.Run(() =>
        {
            Log.Debug($"Waiting");
            startEvent.Wait();
            Log.Debug($"Subscribing");

            var eventSubscription = Observable.Create<int>(observer =>
            {
                var replaySubject = new ReplaySubject<int>();
                var gate = new object();
                lock (gate)
                {
                    var valueBefore = item.Value;
                    Log.Debug($"via NPC-initial: {valueBefore}");
                    replaySubject.OnNext(valueBefore);
                    item.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName != nameof(item.Value))
                        {
                            return;
                        }

                        lock (gate)
                        {
                            var actual = item.Value;
                            Log.Debug($"via NPC: {actual}");
                            replaySubject.OnNext(actual);
                        }
                    };
                    var valueAfter = item.Value;
                    Log.Debug($"via NPC-after: {valueAfter}");
                    if (valueAfter != valueBefore)
                    {
                        replaySubject.OnNext(valueAfter);
                    }
                }
                
                return replaySubject.Subscribe(observer);
            });
            
            anchor = eventSubscription
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
        
        startEvent.Set();
        Task.WaitAll(subscriber, setter);
        return received.LastOrDefault();
    }
    
    
    [Test]
    [Repeat(10000)]
    public async Task WhenAnyShouldBeThreadSafe()
    {
        var anyReactiveObject = new FakeViaReactiveObject() { Value = 1 };
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
        var itemsSource = new SourceListEx<FakeViaReactiveObject>();
        var item = new FakeViaReactiveObject(){ Value = 1 }.AddTo(itemsSource);

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
        var itemsSource = new SourceListEx<FakeViaReactiveObject>();
        var item = new FakeViaReactiveObject(){ Value = 1 }.AddTo(itemsSource);

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
                    .WhenAnyPropertyChanged(nameof(FakeViaReactiveObject.Value))
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
        var itemsSource = new SourceListEx<FakeViaReactiveObject>();
        var item1 = new FakeViaReactiveObject(){ Value = 1 }.AddTo(itemsSource);
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

    private interface IFake : INotifyPropertyChanged
    {
        int Value { get; set; }
    }

    private sealed class FakeViaReactiveObject : DisposableReactiveObject, IFake
    {
        public int Value { get; set; }
    }
    
    private sealed class FakeViaDefaultNpc : IFake
    {
        public int Value { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    private sealed class FakeViaThreadSafeNpc : IFake
    {
        private readonly ConcurrentNpcEventInvoker propertyChanged;

        public FakeViaThreadSafeNpc()
        {
            propertyChanged = new ConcurrentNpcEventInvoker(this);
        }

        public int Value { get; set; }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => this.propertyChanged.Add(value);
            remove => this.propertyChanged.Remove(value);
        }

        private void RaisePropertyChanged(string propertyName)
        {
            propertyChanged.Raise(propertyName);
        }
    }
}