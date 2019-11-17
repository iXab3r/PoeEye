using PoeShared.Native;
using Unity;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Resolution;

namespace PoeShared.Wpf.Scaffolding
{
    public static class UnityContainerExtensions
    {
        public static IUnityContainer RegisterOverlayController(
            this IUnityContainer instance,
            string dependencyName,
            string windowTrackerDependencyName)
        {
            instance
                .RegisterType<IOverlayWindowController>(
                    dependencyName,
                    new ContainerControlledLifetimeManager(),
                    new InjectionFactory(unity => unity.Resolve<OverlayWindowController>(
                        new DependencyOverride<IWindowTracker>(
                            unity.Resolve<IWindowTracker>(windowTrackerDependencyName)))));

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
}