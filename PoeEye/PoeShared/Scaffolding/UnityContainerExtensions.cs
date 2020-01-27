using System;
using Unity;
using Unity.Lifetime;

namespace PoeShared.Scaffolding
{
    public static class UnityContainerExtensions
    {
        public static IUnityContainer RegisterSingleton<TTo>(this IUnityContainer instance, params Type[] types)
        {
            instance.RegisterSingleton(typeof(TTo));
            
            foreach (var type in types)
            {
                instance.RegisterFactory(type, container => container.Resolve(typeof(TTo)), new ContainerControlledLifetimeManager());
            }

            return instance;
        }
    }
}