using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using AutoFixture;
using PoeShared.Services;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Services;

[TestFixture]
internal class BufferedItemsProcessorFixtureTests : FixtureBase
{
    private Queue<string> eventsQueue;
    
    protected override void SetUp()
    {
        base.SetUp();
        eventsQueue = new Queue<string>();
        Container.Register<IScheduler>(() => Scheduler.Immediate);
    }

    [Test]
    public void ShouldCreate()
    {
        //Given

        //When
        Action action = () => CreateInstance();

        //Then
        action.ShouldNotThrow();
    }

    [Test]
    public void ShouldAdd()
    {
        //Given
        var instance = CreateInstance();

        //When
        instance.Add(BufferedItemState.Added, new FakeItem(this, "1"));

        //Then
        instance.Flush(true);
        eventsQueue.CollectionSequenceShouldBe("Added 1");
    }
    
    [Test]
    public void ShouldChange()
    {
        //Given
        var instance = CreateInstance();

        //When
        instance.Add(BufferedItemState.Changed, new FakeItem(this, "1"));

        //Then
        instance.Flush(true);
        eventsQueue.CollectionSequenceShouldBe("Changed 1");
    }
    
    [Test]
    public void ShouldHandleRapidSuccessiveAdditions()
    {
        // Given
        var instance = CreateInstance();
        instance.BufferPeriod = TimeSpan.FromMilliseconds(100);

        // When
        for (int i = 0; i < 10; i++)
        {
            instance.Add(BufferedItemState.Added, new FakeItem(this, $"Item {i}"));
        }
        instance.Flush(true);

        // Then
        eventsQueue.Count.ShouldBe(10); // or any other appropriate assertion
    }

    [Test]
    public void ShouldProcessMixedStates()
    {
        // Given
        var instance = CreateInstance();

        // When
        instance.Add(BufferedItemState.Added, new FakeItem(this, "1"));
        instance.Add(BufferedItemState.Changed, new FakeItem(this, "1"));
        instance.Add(BufferedItemState.Removed, new FakeItem(this, "1"));
        instance.Flush(true);

        // Then
        eventsQueue.CollectionSequenceShouldBe("Added 1", "Removed 1");
    }

    [Test]
    public void ShouldCompactConsecutiveChangeStates()
    {
        // Given
        var instance = CreateInstance();
        instance.BufferPeriod = TimeSpan.MaxValue;

        // When
        instance.Add(BufferedItemState.Changed, new FakeItem(this, "1"));
        instance.Add(BufferedItemState.Changed, new FakeItem(this, "1"));
        instance.Add(BufferedItemState.Changed, new FakeItem(this, "1"));
        instance.Flush(true);

        // Then
        eventsQueue.CollectionSequenceShouldBe("Changed 1");
    }


    private BufferedItemsProcessor CreateInstance()
    {
        return Container.Create<BufferedItemsProcessor>();
    }

    private sealed class FakeItem : IBufferedItem
    {
        private readonly BufferedItemsProcessorFixtureTests owner;

        public FakeItem(BufferedItemsProcessorFixtureTests owner, string key)
        {
            this.owner = owner;
            Id = key;
        }

        public string Id { get; }
        
        public void HandleState(BufferedItemState state)
        {
            owner.Log.Info($"Handled {state}");
            owner.eventsQueue.Enqueue($"{state} {Id}");
        }
    }
}