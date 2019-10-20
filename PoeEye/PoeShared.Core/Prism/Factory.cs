
using Unity;

namespace PoeShared.Prism
{
    internal sealed class Factory<T> : IFactory<T>
    {
        private readonly IUnityContainer container;

        public Factory(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public T Create()
        {
            return container.Resolve<T>();
        }
    }
}