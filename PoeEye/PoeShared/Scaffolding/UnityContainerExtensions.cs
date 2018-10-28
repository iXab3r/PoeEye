using System;
using PoeShared.Native;
using Unity;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Resolution;

namespace PoeShared.Scaffolding
{
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
                    new InjectionFactory(unity => unity.Resolve<WindowTracker>(new DependencyOverride<IStringMatcher>(new RegexStringMatcher().WithLazyWhitelistItem(windowNameFunc)))));
        }
        
        public static IUnityContainer RegisterWindowTracker(this IUnityContainer instance, string dependencyName, string windowNamePattern)
        {
            return instance
                .RegisterType<IWindowTracker>(
                    dependencyName,
                    new ContainerControlledLifetimeManager(),
                    new InjectionFactory(unity =>
                    {
                        var result = unity.Resolve<WindowTracker>(
                            new DependencyOverride<IStringMatcher>(new RegexStringMatcher().AddToWhitelist(windowNamePattern)));
                        result.Name = $"{dependencyName} ('{windowNamePattern}')";
                        return result;
                    }));
        }
        
        public static IUnityContainer RegisterWindowTracker(this IUnityContainer instance, string dependencyName, IStringMatcher matcher)
        {
            return instance
                .RegisterType<IWindowTracker>(
                    dependencyName,
                    new ContainerControlledLifetimeManager(),
                    new InjectionFactory(unity =>
                    {
                        var result = unity.Resolve<WindowTracker>(
                            new DependencyOverride<IStringMatcher>(matcher));
                        result.Name = $"{dependencyName}";
                        return result;
                    }));
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