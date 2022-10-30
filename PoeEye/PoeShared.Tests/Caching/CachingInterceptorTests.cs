using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using PoeShared.Caching;
using PoeShared.Tests.Helpers;
using PoeShared.Tests.Scaffolding;
using Unity;

namespace PoeShared.Tests.Caching;

[TestFixture]
public class CachingInterceptorTests : FixtureBase
{
    private Mock<IClock> clock;
    
    protected override void SetUp()
    {
        clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(new DateTime(2022, 10, 30, 12, 13, 14));
    }
    
    [Test]
    [TestCaseSource(nameof(Cases))]
    public void ShouldCalculateStats(Func<IDataAccessor, object> accessor, Action<object> resultVerifier, Action<Mock<IDataAccessor>, Times> mockVerifier)
    {
        //Given
        var instance = CreateInstance().Object;

        //When
        var result = accessor(instance);

        //Then
        resultVerifier(result);
        var mock = instance.GetMock();
        mockVerifier(mock, Times.Once());
    }
    
    [Test]
    [TestCaseSource(nameof(Cases))]
    public void ShouldPropagate(Func<IDataAccessor, object> accessor, Action<object> resultVerifier, Action<Mock<IDataAccessor>, Times> mockVerifier)
    {
        //Given
        var instanceSource = CreateInstance();
        var proxyGenerator = new ProxyGenerator();
        var interceptor = new CachingInterceptor(Log, clock.Object);
        var instance = proxyGenerator.CreateInterfaceProxyWithTarget(instanceSource.Object, interceptor);

        //When
        var result = accessor(instance);

        //Then
        resultVerifier(result);
        mockVerifier(instanceSource, Times.Once());
    }
    
    [Test]
    [TestCaseSource(nameof(Cases))]
    public void ShouldNotRequestTwice(Func<IDataAccessor, object> accessor, Action<object> resultVerifier, Action<Mock<IDataAccessor>, Times> mockVerifier)
    {
        //Given
        var instanceSource = CreateInstance();
        var proxyGenerator = new ProxyGenerator();
        var interceptor = new CachingInterceptor(Log, clock.Object) { TimeToLive = TimeSpan.FromSeconds(1) };
        var instance = proxyGenerator.CreateInterfaceProxyWithTarget(instanceSource.Object, interceptor);

        var result1 = accessor(instance);
        resultVerifier(result1);
        mockVerifier(instanceSource, Times.Once());
        instanceSource.Invocations.Clear();

        //When
        var result2 = accessor(instance);

        //Then
        resultVerifier(result2);
        mockVerifier(instanceSource, Times.Never());
    }
    
    [Test]
    [TestCaseSource(nameof(Cases))]
    public void ShouldUpdateCacheIfEnoughTimeHasElapsed(Func<IDataAccessor, object> accessor, Action<object> resultVerifier, Action<Mock<IDataAccessor>, Times> mockVerifier)
    {
        //Given
        var instanceSource = CreateInstance();
        var proxyGenerator = new ProxyGenerator();
        var interceptor = new CachingInterceptor(Log, clock.Object) { TimeToLive = TimeSpan.FromSeconds(1) };
        var instance = proxyGenerator.CreateInterfaceProxyWithTarget(instanceSource.Object, interceptor);

        var result1 = accessor(instance);
        resultVerifier(result1);
        mockVerifier(instanceSource, Times.Once());
        instanceSource.Invocations.Clear();

        //When
        var newTime = clock.Object.UtcNow + interceptor.TimeToLive + TimeSpan.FromSeconds(1);
        clock.Setup(x => x.UtcNow).Returns(newTime);
        var result2 = accessor(instance);

        //Then
        resultVerifier(result2);
        mockVerifier(instanceSource, Times.Once());
    }

    [Test]
    public void ShouldPropagateException()
    {
        //Given
        var instanceSource = CreateInstance();
        var proxyGenerator = new ProxyGenerator();
        var interceptor = new CachingInterceptor(Log, clock.Object) { TimeToLive = TimeSpan.FromSeconds(1) };
        var instance = proxyGenerator.CreateInterfaceProxyWithTarget(instanceSource.Object, interceptor);
        instanceSource.Setup(x => x.GetInt()).Throws(new ApplicationException());

        //When
        Action action = () => instance.GetInt();

        //Then
        action.ShouldThrow<ApplicationException>();
    }

    public static IEnumerable<TestCaseData> Cases()
    {
        yield return new NamedTestCaseData(
            new Func<IDataAccessor, object>(x => x.Int), 
            new Action<object>(x => x.ShouldBe(1)), 
            new Action<Mock<IDataAccessor>, Times>((x, times) => x.VerifyGet(x => x.Int, times))) { TestName = "Int" };
        yield return new NamedTestCaseData(
            new Func<IDataAccessor, object>(x => x.GetInt()), 
            new Action<object>(x => x.ShouldBe(2)), 
            new Action<Mock<IDataAccessor>, Times>((x, times) => x.Verify(x => x.GetInt(), times))) { TestName = "GetInt" };
        yield return new NamedTestCaseData(
            new Func<IDataAccessor, object>(x => x.GetString()), 
            new Action<object>(x => x.ShouldBe("3")), 
            new Action<Mock<IDataAccessor>, Times>((x, times) => x.Verify(x => x.GetString(), times))) { TestName = "GetString" };
        yield return new NamedTestCaseData(
            new Func<IDataAccessor, object>(x => x.GetMirror(4)), 
            new Action<object>(x => x.ShouldBe(4)), 
            new Action<Mock<IDataAccessor>, Times>((x, times) => x.Verify(x => x.GetMirror(4), times))) { TestName = "GetMirror" };
    }

    public Mock<IDataAccessor> CreateInstance()
    {
        var mock = new Mock<IDataAccessor>();
        mock.Setup(x => x.GetInt()).Returns(2);
        mock.Setup(x => x.GetString()).Returns("3");
        mock.SetupGet(x => x.Int).Returns(1);
        mock.Setup(x => x.GetMirror(It.IsAny<int>())).Returns<int>((value) => value);
        return mock;
    }
    
    public interface IDataAccessor
    {
        public int Int { get; }

        public int GetInt();

        public string GetString();

        public int GetMirror(int value);
    }
}