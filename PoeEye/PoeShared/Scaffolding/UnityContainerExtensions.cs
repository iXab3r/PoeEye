namespace PoeShared.Scaffolding
{
    using Microsoft.Practices.Unity;

    public static class UnityContainerExtensions
    {
        public static IUnityContainer RegisterSingleton<TFrom, TTo>(this IUnityContainer instance, params InjectionMember[] members)
            where TTo : TFrom
        {
            return instance.RegisterType<TFrom, TTo>(new ContainerControlledLifetimeManager(), members);
        }
    }
}