using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Services;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
internal class ResourcePoolFixtureTests : FixtureBase
{
    private Mock<IClock> clockMock;

    protected override void SetUp()
    {
        base.SetUp();

        clockMock = new Mock<IClock>();
        clockMock.SetupGet(x => x.Elapsed).Returns(() => Clock.Instance.Elapsed);
        clockMock.SetupGet(x => x.Now).Returns(() => Clock.Instance.Now);
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
    public void ShouldRentAndReturnResource()
    {
        // Given
        var pool = CreateInstance();

        // When
        var resource = pool.Rent("testKey", out var instance);

        // Then
        instance.Key.ShouldBe("testKey");
        instance.Value.ShouldBe("Value: testKey");

        // Cleanup
        resource.Dispose();

        // Rent again to ensure the returned resource is reused
        var newResource = pool.Rent("testKey", out var newInstance);
        newInstance.ShouldBeEquivalentTo(instance);

        // Cleanup
        newResource.Dispose();
    }

    [Test]
    public void ShouldCleanupOldResources()
    {
        // Given
        var elapsedTime = TimeSpan.Zero;
        clockMock.SetupGet(x => x.Elapsed).Returns(() => elapsedTime);

        var pool = CreateInstance();
        pool.TimeToLive = TimeSpan.FromSeconds(1);

        pool.Rent("testKey", out var instance1).Dispose();
        pool.Rent("testKey", out var instance2).Dispose();

        // Simulate passage of time
        elapsedTime = TimeSpan.FromSeconds(2.5);

        // When
        pool.Rent("testKey", out var newInstance);

        // Then
        newInstance.ShouldNotBe(instance1);
        newInstance.ShouldNotBe(instance2);
    }


    [Test]
    public void ShouldHandleConcurrentRenting()
    {
        // Given
        var pool = CreateInstance();
        var keys = Enumerable.Range(0, 100).Select(i => i.ToString()).ToArray();
        var instances = new ConcurrentBag<TestResource>();

        // When
        Parallel.ForEach(keys, key =>
        {
            var resource = pool.Rent(key, out var instance);
            instances.Add(instance);
            resource.Dispose();
        });

        // Then
        instances.ShouldAllBe(i => keys.Contains(i.Key));
    }

    [Test]
    public void ResourcesShouldBeReturnedToPoolAfterDispose()
    {
        // Given
        var pool = CreateInstance();

        var resource = pool.Rent("testKey", out var instance);
        resource.Dispose();

        // When
        var newResource = pool.Rent("testKey", out var newInstance);

        // Then
        newInstance.ShouldBe(instance);

        // Cleanup
        newResource.Dispose();
    }

    [Test]
    public void ShouldOnlyCleanupResourcesBeyondTTL()
    {
        // Given
        TimeSpan elapsedTime = TimeSpan.Zero;
        clockMock.SetupGet(x => x.Elapsed).Returns(() => elapsedTime);

        var pool = CreateInstance();
        pool.TimeToLive = TimeSpan.FromSeconds(5);

        var resource1 = pool.Rent("testKey", out var instance1);

        elapsedTime = TimeSpan.FromSeconds(4); // Not beyond TTL yet
        resource1.Dispose();

        var resource2 = pool.Rent("testKey", out var instance2);

        // Then
        instance2.ShouldBe(instance1); // The resource should be reused because it hasn't passed the TTL

        // Cleanup
        resource2.Dispose();
    }

    [Test]
    public void ResourcesWithDifferentKeysShouldBeIndependent()
    {
        // Given
        var pool = CreateInstance();

        var resource1 = pool.Rent("testKey1", out var instance1);
        var resource2 = pool.Rent("testKey2", out var instance2);

        // Then
        instance1.Key.ShouldBe("testKey1");
        instance2.Key.ShouldBe("testKey2");

        instance1.ShouldNotBe(instance2);

        // Cleanup
        resource1.Dispose();
        resource2.Dispose();
    }

    [Test]
    public void ShouldInvokeFactoryWhenPoolIsEmpty()
    {
        // Given
        var pool = CreateInstance();
        var key = "testKey";

        // When
        var resource = pool.Rent(key, out var instance);

        // Then
        instance.Key.ShouldBe(key);
        instance.Value.ShouldBe($"Value: {key}");
    }

    [Test]
    public void ShouldNotCleanupResourcesUnderTtl()
    {
        // Given
        TimeSpan elapsedTime = TimeSpan.Zero;
        clockMock.SetupGet(x => x.Elapsed).Returns(() => elapsedTime);

        var pool = CreateInstance();
        pool.TimeToLive = TimeSpan.FromSeconds(5);

        pool.Rent("testKey", out var instance1).Dispose();

        // Simulate short passage of time
        elapsedTime = TimeSpan.FromSeconds(2);

        // When
        pool.Rent("testKey", out var newInstance);

        // Then
        newInstance.ShouldBe(instance1);
    }

    [Test]
    public void ShouldDisposeRemovedResources()
    {
        // Given
        TimeSpan elapsedTime = TimeSpan.Zero;
        clockMock.SetupGet(x => x.Elapsed).Returns(() => elapsedTime);

        var pool = CreateInstance();
        pool.TimeToLive = TimeSpan.FromSeconds(1);

        var disposable = pool.Rent("testKey1", out var instance);
        disposable.Dispose();

        // Simulate passage of time beyond TTL
        elapsedTime = TimeSpan.FromSeconds(2);
        instance.Anchors.IsDisposed.ShouldBe(false);

        // When
        pool.Rent("testKey2", out _);

        // Then
        instance.Anchors.IsDisposed.ShouldBe(true);
    }

    private ResourcePool<string, TestResource> CreateInstance()
    {
        return new ResourcePool<string, TestResource>(clockMock.Object, key => new TestResource() {Key = key, Value = $"Value: {key}"});
    }

    public sealed class TestResource : DisposableReactiveObject
    {
        public TestResource()
        {
        }

        public string Key { get; init; }

        public string Value { get; set; }
    }
}