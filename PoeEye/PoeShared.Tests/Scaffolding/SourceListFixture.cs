using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

/// <summary>
/// There is a very-very nasty bug in DynamicData 8.0.2 SourceList:
/// when TransformAsync is used, there is a possibility that other conflicting change will overwrite/overlap with ongoing change
/// this leads to a whole plethora of different issues and problems and DOES NOT occur in SourceCache
/// </summary>
public class SourceListFixture : FixtureBase
{
    [Test]
    [Timeout(10000)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(10)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(64)]
    [TestCase(128)]
    public void ShouldTransformSimultaneously(int count)
    {
        //Given
        var instance = CreateInstance();

        var startEvent = new ManualResetEventSlim(false);
        
        var transformed = instance
            .Connect()
            .Do(x => Log.Info(x.DumpToString()))
            .Transform(x => $"={x}")
            .AsObservableList();

        var tasks = Enumerable.Range(0, count)
            .Select(x => Task.Run(() =>
            {
                startEvent.WaitHandle.WaitOne();
                instance.Add(x);
            })).ToArray();

        //When
        startEvent.Set();
        Task.WaitAll(tasks);

        //Then
        Log.Info($"Result sequence:\n\t{transformed.Items.DumpToTable()}");
        var expected = Enumerable.Range(0, count).Select(x => $"={x}").ToArray();
        transformed.Items.CollectionShouldBe(expected);
    }

    [Test]
    [Timeout(10000)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(10)]
    [TestCase(16)]
    [TestCase(32)]
    public void ShouldTransformAsyncAddRangeSimultaneously(int count)
    {
        //Given
        var instance = CreateInstance();

        var startEvent = new ManualResetEventSlim(false);
        
        var transformed = instance
            .Connect()
            .Do(x => Log.Info(x.DumpToString()))
            .TransformAsync(async x =>
            {
                await Task.Delay(new Random().Next(0, 2));
                return $"={x}";
            })
            .Filter(x => x != null)
            .AddKey(x => x)
            .AsObservableCache();

        var tasks = Enumerable.Range(0, count)
            .Select(x => Task.Run(() =>
            {
                startEvent.WaitHandle.WaitOne();
                instance.AddRange(new[]{ x, 1000 + x, 10000 + x });
            })).ToArray();

        //When
        startEvent.Set();
        Task.WaitAll(tasks);
        while (transformed.Count < count * 3)
        {
            Thread.Yield();
        }

        //Then
        Log.Info($"Result sequence:\n\t{transformed.Items.DumpToTable()}");
        var expected = Enumerable.Range(0, count).SelectMany(x => new[]{ $"={x}", $"={1000+x}", $"={10000+x}" }).ToArray();
        transformed.Items.CollectionShouldBe(expected);
    }

    [Test]
    [Timeout(10000)]
    [TestCase(16)]
    [Retry(1000)]
    public void ShouldTransformAsyncAddSimultaneously(int count)
    {
        //Given
        var instance = CreateInstance();

        var startEvent = new ManualResetEventSlim(false);
        
        var counter = 0;
        var transformed = instance
            .Connect()
            .Do(x => Log.Info(x.DumpToString()))
            .TransformAsync(async x =>
            {
                var idx = Interlocked.Increment(ref counter);
                Log.Info($"Item Entry: {x} #{idx}");
                await Task.Delay(new Random().Next(0, 2));
                Log.Info($"Item Exit: {x} #{idx}");
                return $"={x}";
            })
            .Do(x => Log.Info(x.DumpToString()))
            .AsObservableList();

        var tasks = Enumerable.Range(0, count)
            .Select(x => Task.Run(() =>
            {
                startEvent.WaitHandle.WaitOne();
                instance.Add(x);
            })).ToArray();

        //When
        startEvent.Set();
        Task.WaitAll(tasks);
        var sw = Stopwatch.StartNew();
        var maxTimeMs = 5000;
        while (transformed.Count < count && sw.ElapsedMilliseconds < maxTimeMs)
        {
            Thread.Yield();
        }

        //Then
        Log.Info($"Result sequence:\n\t{transformed.Items.DumpToTable()}");

        //FIXME Should throw due to bug in DynamicData 8.0.2 - TransformAsync does not work with SourceList
        Assert.Throws<TimeoutException>(() =>
        {
            if (sw.ElapsedMilliseconds >= maxTimeMs)
            {
                throw new TimeoutException($"Failed to get into expected state in {maxTimeMs}ms");
            }

            var expected = Enumerable.Range(0, count).Select(x => $"={x}").ToArray();
            transformed.Items.CollectionShouldBe(expected);
        });
    }

    [Test]
    [Timeout(10000)]
    [TestCase(16)]
    [Retry(1000)]
    [Ignore("Could not reach the needed stability with meaningful retry count. It thrown during development multiple times though")]
    public void ShouldTransformAsyncRemoveSimultaneously(int count)
    {
        //Given
        var instance = CreateInstance();

        var startEvent = new ManualResetEventSlim(false);

        var counter = 0;
        var transformed = instance
            .Connect()
            .Do(x => Log.Info(x.DumpToString()))
            .TransformAsync(async x =>
            {
                var idx = Interlocked.Increment(ref counter);
                Log.Info($"Item Entry: {x} #{idx}");
                await Task.Delay(new Random().Next(0, 2));
                Log.Info($"Item Exit: {x} #{idx}");
                return $"={x}";
            })
            .Do(x => Log.Info(x.DumpToString()))
            .AsObservableList();

        instance.AddRange(Enumerable.Range(0, count));
        while (transformed.Count < count)
        {
            Thread.Yield();
        }
        Log.Info($"Prepared initial sequence sequence:\n\t{transformed.Items.DumpToTable()}");

        var tasks = Enumerable.Range(0, count)
            .Select(x => Task.Run(() =>
            {
                startEvent.WaitHandle.WaitOne();
                instance.Remove(x);
            })).ToArray();

        //When
        startEvent.Set();
        Task.WaitAll(tasks);
        
        var sw = Stopwatch.StartNew();
        var maxTimeMs = 5000;
        while (transformed.Count > 0 && sw.ElapsedMilliseconds < maxTimeMs)
        {
            Thread.Yield();
        }

        //Then
        Log.Info($"Result sequence:\n\t{transformed.Items.DumpToTable()}");

        //FIXME Should throw due to bug in DynamicData 8.0.2 - TransformAsync does not work with SourceList
        Assert.Throws<TimeoutException>(() =>
        {
            if (sw.ElapsedMilliseconds >= maxTimeMs)
            {
                throw new TimeoutException($"Failed to get into expected state in {maxTimeMs}ms");
            }
            transformed.Items.CollectionShouldBe();
        });
    }
    
    private SourceList<int> CreateInstance()
    {
        return new SourceList<int>();
    }
}