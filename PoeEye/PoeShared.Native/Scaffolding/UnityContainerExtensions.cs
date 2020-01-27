using System;
using PoeShared.Native;
using Unity;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Resolution;

namespace PoeShared.Native.Scaffolding
{
    public static class UnityContainerExtensions
    {
        public static IUnityContainer RegisterWindowTracker(this IUnityContainer instance, string dependencyName,
            Func<string> windowNameFunc)
        {
            return instance.RegisterFactory<IWindowTracker>(
                dependencyName,
                unity =>
                {
                    var result = unity.Resolve<WindowTracker>(
                        new DependencyOverride<IStringMatcher>(
                            new RegexStringMatcher().WithLazyWhitelistItem(windowNameFunc)));
                    result.Name = $"{dependencyName} (Lazy'{windowNameFunc()}')";
                    return result;
                }, new ContainerControlledLifetimeManager());
        }

        public static IUnityContainer RegisterWindowTracker(this IUnityContainer instance, string dependencyName,
            string windowNamePattern)
        {
            return instance.RegisterFactory<IWindowTracker>(
                    dependencyName,
                    unity =>
                    {
                        var result = unity.Resolve<WindowTracker>(
                            new DependencyOverride<IStringMatcher>(
                                new RegexStringMatcher().AddToWhitelist(windowNamePattern)));
                        result.Name = $"{dependencyName} ('{windowNamePattern}')";
                        return result;
                    },
                    new ContainerControlledLifetimeManager());
        }

        public static IUnityContainer RegisterWindowTracker(this IUnityContainer instance, string dependencyName,
            IStringMatcher matcher)
        {
            return instance.RegisterFactory<IWindowTracker>(
                    dependencyName,
                    unity =>
                    {
                        var result = unity.Resolve<WindowTracker>(
                            new DependencyOverride<IStringMatcher>(matcher));
                        result.Name = $"{dependencyName}";
                        return result;
                    },new ContainerControlledLifetimeManager());
        }
    }
}