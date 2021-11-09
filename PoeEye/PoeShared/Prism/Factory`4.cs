
using Unity;
using Unity.Resolution;

namespace PoeShared.Prism
{
    internal sealed class Factory<T1, T2, T3, T4> : IFactory<T1, T2, T3, T4>, INamedFactory<T1, T2, T3, T4>
    {
        private readonly IUnityContainer container;

        public Factory(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public T1 Create(T2 param1, T3 param2, T4 param3)
        {
            return container.Resolve<T1>(
                new DependencyOverride<T2>(param1),
                new DependencyOverride<T3>(param2),
                new DependencyOverride<T4>(param3));
        }

        public T1 Create(string name, T2 param1, T3 param2, T4 param3)
        {
            return container.Resolve<T1>(
                name,
                new DependencyOverride<T2>(param1),
                new DependencyOverride<T3>(param2),
                new DependencyOverride<T4>(param3));
        }
    }
}