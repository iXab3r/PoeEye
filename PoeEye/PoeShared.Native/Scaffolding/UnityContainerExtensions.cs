using System;
using PoeShared.Native;
using PoeShared.Prism;
using Unity;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Resolution;

namespace PoeShared.Native.Scaffolding
{
    public static class UnityContainerExtensions
    {
        public static IUnityContainer RegisterWindowTracker<T>(this IUnityContainer instance, string dependencyName) 
        where T : IWindowTrackerMatcher
        {
            return instance.RegisterFactory<IWindowTracker>(
                dependencyName,
                unity =>
                {
                    var factory = unity.Resolve<IFactory<WindowTracker, IWindowTrackerMatcher>>();
                    var windowTrackerMatcher = unity.Resolve<T>();
                    var result = factory.Create(windowTrackerMatcher);
                    result.Name = $"{dependencyName} (Lazy'{windowTrackerMatcher}')";
                    return result;
                }, new ContainerControlledLifetimeManager());
        }
    }
}