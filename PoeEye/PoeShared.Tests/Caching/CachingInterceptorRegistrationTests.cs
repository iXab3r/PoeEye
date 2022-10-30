using PoeShared.Caching;
using PoeShared.Prism;
using Unity;

namespace PoeShared.Tests.Caching;

[TestFixture]
public class CachingInterceptorRegistrationTests : FixtureBase
{
    private IClock clock;
    
    protected override void SetUp()
    {
        clock = Mock.Of<IClock>();
    }

    [Test]
    public void ShouldRegister()
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.Resolve<ICachingProxyFactory>();

        //Then
        result.ShouldBeOfType<CachingProxyFactory>();
    }

    [Test]
    public void ShouldResolveInstance()
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.Resolve<IClock>();
        
        //Then
        result.ShouldBeSameAs(clock);
    }

    [Test]
    public void ShouldResolveCachingProxy()
    {
        //Given
        var instance = CreateInstance();
        var factory = instance.Resolve<ICachingProxyFactory>();

        //When
        var proxy = factory.GetOrCreate<IClock>();

        //Then
        proxy.ShouldNotBeSameAs(clock);
    }

    [Test]
    public void ShouldResolveTypedCachingProxy()
    {
        //Given
        var instance = CreateInstance();
        var factory = instance.Resolve<ICachingProxyFactory<IClock>>();

        //When
        var proxy = factory.Create();

        //Then
        proxy.ShouldNotBeSameAs(clock);
    }

    private UnityContainer CreateInstance()
    {
        var container = new UnityContainer();
        container.RegisterSingleton<IClock>(x => clock);
        container.RegisterCachingProxyFactory();
        container.RegisterType(typeof(IFactory<>), typeof(Factory<>));
        return container;
    }
}