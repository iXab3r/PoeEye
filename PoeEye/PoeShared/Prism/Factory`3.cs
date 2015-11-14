namespace PoeShared.Prism
{
    using Factory;

    using Guards;

    using Microsoft.Practices.Unity;

    public sealed class Factory<T1, T2, T3> : IFactory<T1, T2, T3>
    {
        private readonly IUnityContainer container;

        public Factory(IUnityContainer container)
        {
            Guard.ArgumentNotNull(() => container);

            this.container = container;
        }

        public T1 Create(T2 parameter1, T3 parameter2)
        {
            return container.Resolve<T1>(new DependencyOverride<T2>(parameter1), new DependencyOverride<T3>(parameter2));
        }
    }
}