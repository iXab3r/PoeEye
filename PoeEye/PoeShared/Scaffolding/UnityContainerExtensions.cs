using System;
using PoeShared.Native;
using Unity;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Registration;
using Unity.Resolution;

namespace PoeShared.Scaffolding
{
    using Guards;

    using Unity; using Unity.Resolution; using Unity.Attributes;

    public static class UnityContainerExtensions
    {
        public static IUnityContainer RegisterSingleton<TTo>(this IUnityContainer instance, params Type[] types)
        {
            var factory = new InjectionFactory(container => container.Resolve<TTo>());
            instance.RegisterSingleton(typeof(TTo));

            foreach (var type in types)
            {
                instance.RegisterSingleton(type, factory);
            }
            return instance;
        }
        
        public static IUnityContainer RegisterWindowTracker(this IUnityContainer instance, string dependencyName, Func<string> windowNameFunc)
        {
            return instance
                 .RegisterType<IWindowTracker>(
                     dependencyName,
                     new ContainerControlledLifetimeManager(),
                     new InjectionFactory(unity => unity.Resolve<WindowTracker>(new DependencyOverride<Func<string>>(windowNameFunc))));
        }

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
                            new DependencyOverride<IWindowTracker>(unity.Resolve<IWindowTracker>(windowTrackerDependencyName)))));

            return instance;
        }
    }
}
