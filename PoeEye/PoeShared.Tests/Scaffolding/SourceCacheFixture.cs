using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

public class SourceCacheFixture : FixtureBase
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
            .Transform(x => $"={x}")
            .AsObservableCache();

        var tasks = Enumerable.Range(0, count)
            .Select(x => Task.Run(() =>
            {
                startEvent.WaitHandle.WaitOne();
                instance.AddOrUpdate(x);
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
    public void ShouldTransformAsyncSimultaneously(int count)
    {
        //Given
        var instance = CreateInstance();

        var startEvent = new ManualResetEventSlim(false);
        
        var transformed = instance
            .Connect()
            .TransformAsync(async x =>
            {
                await Task.Delay(1);
                return $"={x}";
            })
            .AsObservableCache();

        var tasks = Enumerable.Range(0, count)
            .Select(x => Task.Run(() =>
            {
                startEvent.WaitHandle.WaitOne();
                instance.AddOrUpdate(x);
            })).ToArray();

        //When
        startEvent.Set();
        Task.WaitAll(tasks);
        while (transformed.Count < count)
        {
            Thread.Yield();
        }

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
    public void ShouldTransformAsyncAddRemoveSimultaneously(int count)
    {
        //Given
        var instance = CreateInstance();

        var startEvent = new ManualResetEventSlim(false);

        var counter = 0;
        var transformed = instance
            .Connect()
            .TransformAsync(async x =>
            {
                var idx = Interlocked.Increment(ref counter);
                Log.Info($"Item Entry: {x} #{idx}");
                await Task.Delay(new Random().Next(0, 2));
                Log.Info($"Item Exit: {x} #{idx}");
                return $"={x}";
            })
            .AsObservableCache();

        
        var tasks = Enumerable.Range(0, count)
            .ForEach(x => instance.AddOrUpdate(x))
            .Select(x => Task.Run(() =>
            {
                startEvent.WaitHandle.WaitOne();
                instance.Remove(x);
            })).ToArray();

        //When
        startEvent.Set();
        Task.WaitAll(tasks);
        while (transformed.Count > 0)
        {
            Thread.Yield();
        }

        //Then
        Log.Info($"Result sequence:\n\t{transformed.Items.DumpToTable()}");
        transformed.Items.CollectionShouldBe();
    }
    
    
    [Test]
    [Timeout(10000)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(10)]
    [TestCase(16)]
    [TestCase(32)]
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
            .AsObservableCache();

        var tasks = Enumerable.Range(0, count)
            .Select(x => Task.Run(() =>
            {
                startEvent.WaitHandle.WaitOne();
                instance.AddOrUpdate(x);
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

        if (sw.ElapsedMilliseconds >= maxTimeMs)
        {
            throw new TimeoutException($"Failed to get into expected state in {maxTimeMs}ms");
        }
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
            .AsObservableCache();

        instance.AddOrUpdate(Enumerable.Range(0, count));
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

        if (sw.ElapsedMilliseconds >= maxTimeMs)
        {
            throw new TimeoutException($"Failed to get into expected state in {maxTimeMs}ms");
        }
        transformed.Items.CollectionShouldBe();
    }
    
    private SourceCache<int, string> CreateInstance()
    {
        return new SourceCache<int, string>(x => x.ToString());
    }
}