using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using NUnit.Framework;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class SynchronizedObservableCollectionFixture : FixtureBase
{

    [Test]
    [TestCase(1)]
    [TestCase(100)]
    [TestCase(1000)]
    public void ShouldAllowAdd(int threads)
    {
        //Given
        var instance = CreateInstance();
        var kickOffEvent = new ManualResetEvent(false);
        var indexes = Enumerable.Range(0, threads).ToArray();
        var tasks = indexes
            .Select(x => Task.Run(() =>
            {
                kickOffEvent.WaitOne();
                instance.Add(x);
            }))
            .ToArray();

        //When
        kickOffEvent.Set();

        //Then
        Task.WaitAll(tasks);
        instance.ShouldBe(indexes, ignoreOrder: true);
    }
    
    [Test]
    [TestCase(1)]
    [TestCase(100)]
    [TestCase(1000)]
    public void ShouldAllowRemoval(int threads)
    {
        //Given
        var instance = CreateInstance();
        var kickOffEvent = new ManualResetEvent(false);
        var indexes = Enumerable.Range(0, threads).ToArray();
        instance.Add(indexes);
        var tasks = indexes
            .Select(x => Task.Run(() =>
            {
                kickOffEvent.WaitOne();
                instance.Remove(x);
            }))
            .ToArray();

        //When
        kickOffEvent.Set();

        //Then
        Task.WaitAll(tasks);
        instance.Count.ShouldBe(0);
    }

    [Test]
    [TestCase(1)]
    [TestCase(100)]
    [TestCase(1000)]
    public void ShouldBind(int threads)
    {
        //Given
        var cache = new SourceCache<int, int>(x => x);
        using var cacheAnchors = cache.Connect().BindToCollection(out var instance).Subscribe();

        var kickOffEvent = new ManualResetEvent(false);
        var indexes = Enumerable.Range(0, threads).ToArray();
        var tasks = indexes
            .Select(x => Task.Run(() =>
            {
                kickOffEvent.WaitOne();
                Log.Debug(() => $"Adding {x}");
                cache.AddOrUpdate(x);
                Log.Debug(() => $"Added {x}");
            }))
            .ToArray();

        //When
        kickOffEvent.Set();

        //Then
        Task.WaitAll(tasks);
        cache.Items.ToList().ShouldBe(indexes, ignoreOrder: true);
        instance.ShouldBe(indexes, ignoreOrder: true);
    }
    
    [Test]
    [TestCase(1)]
    [TestCase(100)]
    [TestCase(1000)]
    public void ShouldRemove(int threads)
    {
        //Given
        var cache = new SourceCache<int, int>(x => x);
        using var cacheAnchors = cache.Connect().BindToCollection(out var instance).Subscribe();

        var kickOffEvent = new ManualResetEvent(false);
        var indexes = Enumerable.Range(0, threads).ToArray();
        cache.AddOrUpdate(indexes);
        var tasks = indexes
            .Select(x => Task.Run(() =>
            {
                kickOffEvent.WaitOne();
                Log.Debug(() => $"Removing {x}");
                cache.RemoveKey(x);                
                Log.Debug(() => $"Removed {x}");
            }))
            .ToArray();

        //When
        kickOffEvent.Set();

        //Then
        Task.WaitAll(tasks);
        cache.Count.ShouldBe(0);
        instance.Count.ShouldBe(0);
    }
    
    public ReadOnlyObservableCollectionEx<int> CreateInstance()
    {
        return new ReadOnlyObservableCollectionEx<int>();
    }
}