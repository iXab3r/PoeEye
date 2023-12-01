using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Services;

namespace PoeShared.Tests.Services;

[TestFixture]
internal class SharedResourceTestsTests : FixtureBase
{
    private FactoryFuncFake factoryFunc;

    protected override void SetUp()
    {
        base.SetUp();
        factoryFunc = new FactoryFuncFake();
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
    public void ShouldCreateResource()
    {
        //Given
        var instance = CreateInstance();

        //When
        var resource = instance.RentOrCreate();

        //Then
        resource.ShouldNotBeNull();
        factoryFunc.InstancesCount.ShouldBe(1);
    }

    [Test]
    public void ShouldReturnSameResourceIfNotDisposed()
    {
        //Given
        var instance = CreateInstance();

        var resource1 = instance.RentOrCreate();

        //When
        var resource2 = instance.RentOrCreate();

        //Then
        factoryFunc.InstancesCount.ShouldBe(1);
        resource2.ShouldBeSameAs(resource1);
        resource2.IsDisposed.ShouldBeFalse();
    }

    [Test]
    public void ShouldNotDisposeResourceWhenSomethingIsRentingResource()
    {
        // Given
        var instance = CreateInstance();
        var resource = instance.RentOrCreate();

        // When
        instance.Dispose();

        // Then
        resource.IsDisposed.ShouldBeFalse();
    }

    [Test]
    public void ShouldNotDisposeResourceWhenInstanceIsDisposed()
    {
        // Given
        var instance = CreateInstance();
        var resource = instance.RentOrCreate();

        // When
        resource.Dispose();

        // Then
        resource.IsDisposed.ShouldBeFalse();
    }

    [Test]
    public void ShouldDispose()
    {
        // Given
        var instance = CreateInstance();
        var resource = instance.RentOrCreate();

        // When
        instance.Dispose();
        resource.Dispose();

        // Then
        resource.IsDisposed.ShouldBeTrue();
    }

    [Test]
    public void ShouldRecreateResourceAfterDisposal()
    {
        // Given
        var instance = CreateInstance();
        var resource = instance.RentOrCreate();
        resource.Dispose(); //release rent
        resource.Dispose(); //dispose resource itself

        // When
        var newResource = instance.RentOrCreate();

        // Then
        newResource.ShouldNotBeNull();
        newResource.ShouldNotBeSameAs(resource);
        factoryFunc.InstancesCount.ShouldBe(2);
    }

    [Test]
    public void ShouldThrowExceptionIfNewInstanceCannotBeRented()
    {
        // Given
        factoryFunc.CanBeRentedByDefault = false;
        var instance = CreateInstance();

        // When
        var action = new Action(() => instance.RentOrCreate());

        // Then
        action.ShouldThrow<InvalidOperationException>();
    }

    [Test]
    public void ShouldHandleConcurrentRentOrCreateCallsCorrectly()
    {
        // Given
        var instance = CreateInstance();
        factoryFunc.CanBeRentedByDefault = true;

        // When
        var startEvent = new ManualResetEvent(false);

        var tasks = Enumerable.Range(0, Environment.ProcessorCount)
            .Select(_ => Task.Run(() =>
            {
                startEvent.WaitOne();
                return instance.RentOrCreate();
            }))
            .ToArray();

        startEvent.Set();
        Task.WaitAll(tasks);

        // Then
        factoryFunc.InstancesCount.ShouldBe(1);
        tasks.Select(task => task.Result).ShouldAllBe(resource => resource == tasks.First().Result);
    }

    [Test]
    public void ShouldHandleConcurrentRentOrCreateCallsCorrectlyAfterDisposal()
    {
        // Given
        var instance = CreateInstance();
        factoryFunc.CanBeRentedByDefault = true;
        var resource = instance.RentOrCreate();
        resource.Dispose(); // create and dispose first rent
        resource.Dispose(); // dispose the resource

        // When
        var startEvent = new ManualResetEvent(false);

        var tasks = Enumerable.Range(0, Environment.ProcessorCount)
            .Select(_ => Task.Run(() =>
            {
                startEvent.WaitOne();
                return instance.RentOrCreate();
            }))
            .ToArray();

        startEvent.Set();
        Task.WaitAll(tasks);

        // Then
        factoryFunc.InstancesCount.ShouldBe(2);
        var expectedResource = tasks.First().Result;
        tasks.Select(task => task.Result).ShouldAllBe(taskResource => taskResource == expectedResource);
    }

    [Test]
    public void ShouldNotCreateNewResourceIfInstanceIsRentedAndNotDisposed()
    {
        // Given
        var instance = CreateInstance();
        var resource = instance.RentOrCreate();

        // When
        var sameResource = instance.RentOrCreate();

        // Then
        sameResource.ShouldBeSameAs(resource);
        factoryFunc.InstancesCount.ShouldBe(1);
    }

    private SharedResource<TestClass> CreateInstance()
    {
        return new SharedResource<TestClass>(factoryFunc.Create);
    }

    public sealed class FactoryFuncFake
    {
        private int instancesCount;

        public int InstancesCount => instancesCount;

        public bool CanBeRentedByDefault { get; set; } = true;

        public TestClass Create()
        {
            Interlocked.Increment(ref instancesCount);
            var result = new TestClass();
            result.CanBeRented = CanBeRentedByDefault;
            return result;
        }
    }

    public sealed class TestClass : SharedResourceBase
    {
        public bool CanBeRented { get; set; }

        protected override bool CanRent()
        {
            return CanBeRented;
        }
    }
}