using System;
using System.Collections;
using System.Reflection;
using AutoFixture;
using PoeShared.Blazor;
using PoeShared.Blazor.Services;
using PoeShared.Services;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Blazor.Services;

[TestFixture]
public class BlazorViewRepositoryFixture : FixtureBase
{
    private Mock<IAssemblyTracker> assemblyTracker;
    private ReactiveList<Assembly> loadedAssemblies;
    
    protected override void SetUp()
    {
        assemblyTracker = Container.RegisterMock<IAssemblyTracker>();
        loadedAssemblies = new ReactiveList<Assembly>();
        assemblyTracker.SetupGet(x => x.Assemblies).Returns(loadedAssemblies);
    }

    [Test]
    [TestCase(typeof(object), null)]
    [TestCase(typeof(string), typeof(TestBlazorReactiveComponentForString))]
    [TestCase(typeof(ICollection), typeof(TestBlazorReactiveComponentForCollection))]
    [TestCase(typeof(ArrayList), typeof(TestBlazorReactiveComponentForCollection))]
    public void ShouldResolve(Type contentType, Type expected)
    {
        //Given
        var instance = CreateInstance();
        loadedAssemblies.Add(Assembly.GetExecutingAssembly());

        //When
        var viewType = instance.ResolveViewType(contentType);

        //Then
        viewType.ShouldBe(expected);
    }
    
    [Test]
    [TestCase(typeof(object), null)]
    [TestCase(typeof(string), typeof(BlazorTestViewForString))]
    [TestCase(typeof(ICollection), typeof(BlazorTestViewForCollection))]
    public void ShouldResolveManualRegistration(Type contentType, Type expected)
    {
        //Given
        var instance = CreateInstance();
        loadedAssemblies.Add(Assembly.GetExecutingAssembly());
        instance.RegisterViewType(typeof(BlazorTestViewForString));
        instance.RegisterViewType(typeof(BlazorTestViewForCollection));

        //When
        var viewType = instance.ResolveViewType(contentType);

        //Then
        viewType.ShouldBe(expected);
    }

    public BlazorViewRepository CreateInstance()
    {
        return Container.Create<BlazorViewRepository>();
    }

    public class TestBlazorReactiveComponentForString : BlazorReactiveComponent<string>
    {
    }
    
    public class TestBlazorReactiveComponentForCollection : BlazorReactiveComponent<ICollection>
    {
    }
    
    public class BlazorTestViewForString : BlazorTestView<string>
    {
    }
    
    [BlazorView(IsForManualRegistrationOnly = true)]
    public class BlazorTestViewForCollection : BlazorTestView<ICollection>
    {
    }
    
    [BlazorView(IsForManualRegistrationOnly = true)]
    public class BlazorTestView<T> : BlazorReactiveComponent<T> where T : class
    {
        
    }
}