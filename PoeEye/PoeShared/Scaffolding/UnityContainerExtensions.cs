using System;
using System.Linq;
using log4net;
using Unity;
using Unity.Extension;
using Unity.Lifetime;

namespace PoeShared.Scaffolding
{
    public static class UnityContainerExtensions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UnityContainerExtensions));

        public static IUnityContainer RegisterSingleton<TTo>(this IUnityContainer instance, params Type[] types)
        {
            instance.RegisterSingleton(typeof(TTo));
            
            foreach (var type in types)
            {
                instance.RegisterType(type, typeof(TTo), new ContainerControlledLifetimeManager());
            }
            
            return instance;
        }

        public static IUnityContainer RegisterSingleton<TTo>(this IUnityContainer instance, Func<IUnityContainer, object> func)
        {
            return instance.RegisterFactory<TTo>(func, new ContainerControlledLifetimeManager());
        }
        
        public static IUnityContainer RegisterSingleton<TTo>(this IUnityContainer instance, string name, Func<IUnityContainer, object> func)
        {
            return instance.RegisterFactory<TTo>(name, func, new ContainerControlledLifetimeManager());
        }
        
        public static IUnityContainer AddNewExtensionIfNotExists<TExtension>(this IUnityContainer container)
            where TExtension : UnityContainerExtension
        {
            if (container.Configure<TExtension>() != null)
            {
                Log.Warn($"Extension of type {typeof(TExtension)} is already added - ignoring request");
                return container;
            }
            
            Log.Debug($"Adding new extension of type {typeof(TExtension)} to container, registered types: {container.Registrations.Count()}");
            return container.AddNewExtension<TExtension>();
        }
    }
}