using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;
using Unity.Lifetime;

namespace PoeShared.Native.Scaffolding;

public static class UnityContainerExtensions
{
    public static IUnityContainer RegisterWindowTracker<T>(this IUnityContainer instance, string dependencyName) 
        where T : IWindowTrackerMatcher
    {
        return instance.RegisterSingleton<IWindowTracker>(
            dependencyName,
            unity =>
            {
                var factory = unity.Resolve<IFactory<WindowTracker, IWindowTrackerMatcher>>();
                var windowTrackerMatcher = unity.Resolve<T>();
                var result = factory.Create(windowTrackerMatcher);
                result.Name = $"{dependencyName} (Lazy'{windowTrackerMatcher}')";
                return result;
            });
    }
}