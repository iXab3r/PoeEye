using PoeShared.Native;
using Unity;
using Unity.Lifetime;
using Unity.Resolution;

namespace PoeShared.Wpf.Scaffolding;

public static class UnityContainerExtensions
{
    public static IUnityContainer RegisterOverlayController(
        this IUnityContainer instance,
        string dependencyName,
        string windowTrackerDependencyName)
    {
        instance
            .RegisterFactory<IOverlayWindowController>(
                dependencyName,
                unity => unity.Resolve<TrackedOverlayWindowController>(
                    new DependencyOverride<IWindowTracker>(
                        unity.Resolve<IWindowTracker>(windowTrackerDependencyName))), 
                new ContainerControlledLifetimeManager());

        return instance;
    }
        
    public static IUnityContainer RegisterOverlayController(
        this IUnityContainer instance,
        string windowTrackerDependencyName)
    {
        instance
            .RegisterFactory<IOverlayWindowController>(
                unity => unity.Resolve<TrackedOverlayWindowController>(
                    new DependencyOverride<IWindowTracker>(
                        unity.Resolve<IWindowTracker>(windowTrackerDependencyName))), 
                new ContainerControlledLifetimeManager());

        return instance;
    }
        
    public static IUnityContainer RegisterOverlayController(
        this IUnityContainer instance)
    {
        instance
            .RegisterType<IOverlayWindowController, OverlayWindowController>();

        return instance;
    }
}