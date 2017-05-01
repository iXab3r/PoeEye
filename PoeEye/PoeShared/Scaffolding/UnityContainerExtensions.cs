using System;
using PoeShared.Native;

namespace PoeShared.Scaffolding
{
    using Guards;

    using Microsoft.Practices.Unity;

    public static class UnityContainerExtensions
    {
        public static IUnityContainer RegisterSingleton<TFrom, TTo>(this IUnityContainer instance, params InjectionMember[] members)
            where TTo : TFrom
        {
            return instance.RegisterSingleton<TFrom, TTo>(null, members);
        }

        public static IUnityContainer RegisterSingleton<TFrom>(this IUnityContainer instance, params InjectionMember[] members)
        {
            return instance.RegisterSingleton(typeof(TFrom), members);
        }

        public static IUnityContainer RegisterSingleton(this IUnityContainer instance, Type from, Type to, params InjectionMember[] members)
        {
            return instance.RegisterType(from, to, new ContainerControlledLifetimeManager(), members);
        }

        public static IUnityContainer RegisterSingleton(this IUnityContainer instance, Type from, params InjectionMember[] members)
        {
            return instance.RegisterType(from, new ContainerControlledLifetimeManager(), members);
        }

        public static IUnityContainer RegisterSingleton<TFrom, TTo>(this IUnityContainer instance, string name, params InjectionMember[] members)
            where TTo : TFrom
        {
            Guard.ArgumentNotNull(instance, nameof(instance));
            Guard.ArgumentNotNull(members, nameof(members));
            
            return instance.RegisterType<TFrom, TTo>(name, new ContainerControlledLifetimeManager(), members);
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
