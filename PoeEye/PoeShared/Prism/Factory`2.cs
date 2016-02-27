namespace PoeShared.Prism
{
    using Guards;

    using Microsoft.Practices.Unity;

    internal sealed class Factory<T1, T2> : IFactory<T1, T2>
    {
        private readonly IUnityContainer container;

        public Factory(IUnityContainer container)
        {
            Guard.ArgumentNotNull(() => container);

            this.container = container;
        }

        public T1 Create(T2 param1)
        {
            return container.Resolve<T1>(new DependencyOverride<T2>(param1));
        }
    }
}