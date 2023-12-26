using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.Services;
using Shouldly;

namespace PoeShared.Tests.Services;

[TestFixture]
public class SharedResourceRepositoryFixture : FixtureBase
{
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
    public void ShouldCreateNewWhenRequested()
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.GetOrAdd(0, x => new Resource(x));

        //Then
        result.Key.ShouldBe(0);
        instance.Count.ShouldBe(1);
    }

    [Test]
    public void ShouldKeepItemEvenWhenDisposed()
    {
        //Given
        var instance = CreateInstance();
        var resource = instance.GetOrAdd(0, x => new Resource(x));

        //When
        resource.Dispose();

        //Then
        resource.IsDisposed.ShouldBe(true);
        resource.RefCount.ShouldBe(0);
        instance.Count.ShouldBe(1);
    }

    [Test]
    public void ShouldReturnExisting()
    {
        //Given
        var instance = CreateInstance();
        var resource = instance.GetOrAdd(0, x => new Resource(x));

        //When
        var secondResource = instance.GetOrAdd(0, _ => throw new InvalidOperationException());

        //Then
        secondResource.ShouldBeSameAs(resource);
    }

    [Test]
    public void ShouldRecreateIfDisposed()
    {
        //Given
        var instance = CreateInstance();
        var resource = instance.GetOrAdd(0, x => new Resource(x));
        resource.Dispose();

        //When
        var secondResource = instance.GetOrAdd(0, x => new Resource(x));


        //Then
        secondResource.Key.ShouldBe(0);
        instance.Count.ShouldBe(1);
        resource.ShouldNotBeSameAs(secondResource);
        resource.IsDisposed.ShouldBe(true);
        secondResource.IsDisposed.ShouldNotBe(true);
    }

    [Test]
    [TestCase(1, 1)]
    [TestCase(1, 100)]
    [TestCase(2, 1)]
    [TestCase(2, 10)]
    [TestCase(2, 100)]
    [TestCase(100, 1)]
    [TestCase(100, 10)]
    public void ShouldAllowSimultaneousRents(int runners, int runs)
    {
        //Given
        var instance = CreateInstance();
        var tasks = new List<Task>();
        var allRunnersReady = new ManualResetEvent(false);
        for (var runnerIdx = 0; runnerIdx < runners; runnerIdx++)
        {
            var runnerId = $"Runner#{runnerIdx}";
            Log.Debug($"Starting runner #{runnerId}");
            var task = Task.Run(() =>
            {
                var logger = Log.WithSuffix(runnerId);
                logger.Debug("Started, awaiting for signal");
                allRunnersReady.WaitOne();
                logger.Debug("Signal received, starting runs");
                for (var runIdx = 0; runIdx < runs; runIdx++)
                {
                    logger.Debug($"Starting run #{runIdx}");
                    using (var value = instance.GetOrAdd(0, x => new Resource(x)))
                    {
                        logger.Debug($"Rented {value}");
                    }
                    logger.Debug($"Completed run #{runIdx}");
                }
            });
            tasks.Add(task);
        }
            
        //When
        Log.Debug("Sending signal to all runners");
        allRunnersReady.Set();
        Log.Debug("Signal sent");

        //Then
        Task.WaitAll(tasks.ToArray());
    }

    private SharedResourceRepository<int, Resource> CreateInstance()
    {
        return new SharedResourceRepository<int, Resource>();
    }

    private sealed class Resource : SharedResourceBase
    {
        public int Key { get; }

        public Resource(int key)
        {
            Key = key;
        }

        protected override void FormatToString(ToStringBuilder builder)
        {
            base.FormatToString(builder);
            builder.Append("Resource");
            builder.AppendParameter(nameof(Key), Key);
            builder.AppendParameter(nameof(ResourceId), ResourceId);
            builder.AppendParameter(nameof(RefCount), RefCount);
        }
    }
}