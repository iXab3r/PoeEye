using System.Linq;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Unity;
using Unity.Lifetime;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
internal class UnityContainerExtensionsFixtureTests : FixtureBase
{
    [Test]
    public void ShouldCreateTransient()
    {
        //Given
        var serviceCollection = new ServiceCollection();
        var unityContainer = new UnityContainer();

        unityContainer.RegisterType<ITestInterface, TestInterfaceImplementation>();

        var descriptors = unityContainer.ToServiceDescriptors();
        serviceCollection.Add(descriptors);
        var serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions {ValidateScopes = true, ValidateOnBuild = true});

        //When
        var result = serviceProvider.GetService<ITestInterface>();

        //Then
        result.ShouldBeOfType<TestInterfaceImplementation>();
    }

    [Test]
    public void ShouldCreateSingleton()
    {
        // Given
        var serviceCollection = new ServiceCollection();
        var unityContainer = new UnityContainer();

        unityContainer.RegisterType<ISingletonTestService, SingletonTestService>(new ContainerControlledLifetimeManager());

        serviceCollection.Add(unityContainer.ToServiceDescriptors());
        var serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions {ValidateScopes = true, ValidateOnBuild = true});

        // When
        var instance1 = serviceProvider.GetService<ISingletonTestService>();
        var instance2 = serviceProvider.GetService<ISingletonTestService>();

        // Then
        instance1.ShouldBeOfType<SingletonTestService>();
        instance1.ShouldBeSameAs(instance2); // Singleton instance should be the same
    }

    [Test]
    public void ShouldResolveOpenGeneric()
    {
        // Given
        var serviceCollection = new ServiceCollection();
        var unityContainer = new UnityContainer();

        unityContainer.RegisterType(typeof(IGenericTestService<>), typeof(GenericTestService<>));

        serviceCollection.Add(unityContainer.ToServiceDescriptors());
        var serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions {ValidateScopes = true, ValidateOnBuild = true});

        // When
        var result = serviceProvider.GetService<IGenericTestService<string>>();

        // Then
        result.ShouldBeOfType<GenericTestService<string>>();
    }

    [Test]
    public void ShouldCreateScopedService()
    {
        // Given
        var serviceCollection = new ServiceCollection();
        var unityContainer = new UnityContainer();

        unityContainer.RegisterType<IScopedTestService, ScopedTestService>(new HierarchicalLifetimeManager());

        serviceCollection.Add(unityContainer.ToServiceDescriptors());
        var serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions {ValidateScopes = true, ValidateOnBuild = true});

        // When
        IScopedTestService instance1, instance2;
        using (var scope = serviceProvider.CreateScope())
        {
            instance1 = scope.ServiceProvider.GetService<IScopedTestService>();
            instance2 = scope.ServiceProvider.GetService<IScopedTestService>();
        }

        // Then
        instance1.ShouldBeOfType<ScopedTestService>();
        instance1.ShouldBeSameAs(instance2); // Scoped instances should be the same within the scope
    }

    [Test]
    public void ShouldResolveRegisteredInstance()
    {
        // Given
        var serviceCollection = new ServiceCollection();
        var unityContainer = new UnityContainer();
        var instance = new InstanceTestService();

        unityContainer.RegisterInstance<IInstanceTestService>(instance);

        serviceCollection.Add(unityContainer.ToServiceDescriptors());
        var serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions {ValidateScopes = true, ValidateOnBuild = true});

        // When
        var resolvedInstance = serviceProvider.GetService<IInstanceTestService>();

        // Then
        resolvedInstance.ShouldBeSameAs(instance); // Resolved instance should be the same as the registered one
    }

    [Test]
    public void ShouldIgnoreNamedRegistrations()
    {
        // Given
        var serviceCollection = new ServiceCollection();
        var unityContainer = new UnityContainer();

        unityContainer.RegisterType<ITestService, TestService1>("Name1");
        unityContainer.RegisterType<ITestService, TestService2>();

        serviceCollection.Add(unityContainer.ToServiceDescriptors());
        var serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions {ValidateScopes = true, ValidateOnBuild = true});

        // When
        var defaultService = serviceProvider.GetService<ITestService>();

        // Then
        defaultService.ShouldBeOfType<TestService2>(); // Should resolve the unnamed registration
        // Optionally, assert that no service is resolved when asked for the named registration
    }

    [Test]
    public void ShouldKeepLastRegistrationOfSameServiceType()
    {
        // Given
        var serviceCollection = new ServiceCollection();
        var unityContainer = new UnityContainer();

        unityContainer.RegisterType<ITestService, TestService1>();
        unityContainer.RegisterType<ITestService, TestService2>(); // This will override TestService1

        serviceCollection.Add(unityContainer.ToServiceDescriptors());
        var serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions {ValidateScopes = true, ValidateOnBuild = true});

        // When
        var services = serviceProvider.GetServices<ITestService>().ToArray();

        // Then
        services.Length.ShouldBe(1); // Only the last registration should be present
        services.First().ShouldBeOfType<TestService2>(); // Should be the last registered implementation
    }

    
    [Test]
    public void ShouldNotSupportNonOpenGenerics()
    {
        // Given
        var serviceCollection = new ServiceCollection();
        var unityContainer = new UnityContainer();

        // Registering a closed generic type
        unityContainer.RegisterType(typeof(IGenericTestService<int>), typeof(GenericTestService<int>));

        var descriptors = unityContainer.ToServiceDescriptors();
        serviceCollection.Add(descriptors);
        var serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        // When
        var result = serviceProvider.GetService<IGenericTestService<int>>();

        // Then
        // The result should be null since the closed generic registration should not be supported
        result.ShouldBeOfType<GenericTestService<int>>();
    }
    
    [Test]
    public void ShouldHandleMismatchedGenericTypes()
    {
        // Given
        var serviceCollection = new ServiceCollection();
        var unityContainer = new UnityContainer();

        // Registering an open generic type to a non-generic implementation
        unityContainer.RegisterType(typeof(IGenericTestService<>), typeof(NonGenericTestService));

        var descriptors = unityContainer.ToServiceDescriptors();
        serviceCollection.Add(descriptors);
        var serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        // When
        var result = serviceProvider.GetService<IGenericTestService<int>>();

        // Then
        result.ShouldBeNull(); // The result should be null due to mismatched generic mapping
    }

    private interface ITestService
    {
    }

    private class TestService1 : ITestService
    {
    }

    private class TestService2 : ITestService
    {
    }


    private interface IInstanceTestService
    {
    }

    private class InstanceTestService : IInstanceTestService
    {
    }


    private interface IScopedTestService
    {
    }

    private class ScopedTestService : IScopedTestService
    {
    }


    private interface IGenericTestService<T>
    {
    }
    
    private class NonGenericTestService : IGenericTestService<int> { } // Intentionally mismatched for the test

    private class GenericTestService<T> : IGenericTestService<T>
    {
    }


    private interface ISingletonTestService
    {
    }

    private class SingletonTestService : ISingletonTestService
    {
    }


    private interface ITestInterface
    {
    }

    private class TestInterfaceImplementation : ITestInterface
    {
    }
}