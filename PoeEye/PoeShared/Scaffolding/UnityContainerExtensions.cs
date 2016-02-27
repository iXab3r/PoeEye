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

        public static IUnityContainer RegisterSingleton<TFrom, TTo>(this IUnityContainer instance, string name, params InjectionMember[] members)
            where TTo : TFrom
        {
            Guard.ArgumentNotNull(() => instance);
            Guard.ArgumentNotNull(() => members);
            
            return instance.RegisterType<TFrom, TTo>(name, new ContainerControlledLifetimeManager(), members);
        }
    }
}