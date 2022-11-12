using NUnit.Framework;
using AutoFixture;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ABI.Windows.ApplicationModel.Email;
using DynamicData;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class ReadOnlyObservableCollectionExTests : FixtureBase
{
    protected override void SetUp()
    {
    }

    [Test]
    public void ShouldCreate()
    {
        // Given
        // When 
        Action action = () => CreateInstance();

        // Then
        action.ShouldNotThrow();
    }

    [Test]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(10)]
    [TestCase(1000)]
    [TestCase(10000)]
    public void ShouldBind(int itemCount)
    {
        //Given
        var instance = CreateInstance();

        var startTasks = new ManualResetEvent(false);
        long itemsToAdd = itemCount;
        var producer = Task.Run(() =>
        {
            startTasks.WaitOne();
            Log.Debug($"Adding {itemCount} items");
            while (Interlocked.Decrement(ref itemsToAdd) > 0)
            {
                var idx = itemCount - itemsToAdd;
                Log.Debug($"Adding item #{idx + 1}");
                instance.Add(idx);
            }
            Log.Debug($"Added {itemCount} items");
        });

        var consumer = Task.Run(() =>
        {
            startTasks.WaitOne();
            Log.Debug($"Consuming items");
            var attemptIdx = 0;
            while (Interlocked.Read(ref itemsToAdd) > 0)
            {
                attemptIdx++;
                Log.Debug($"Attempt #{attemptIdx}");
                var itemsRead = 0;
                foreach (var i in instance)
                {
                    itemsRead++;
                }
                Log.Debug($"Read {itemsRead} item(s) on attempt #{attemptIdx}");
            }
            Log.Debug($"Consumed");
        });

        //When
        startTasks.Set();

        //Then
        Task.WaitAll(producer, consumer);
    }

    private ReadOnlyObservableCollectionEx<long> CreateInstance()
    {
        return new ReadOnlyObservableCollectionEx<long>();
    }
}