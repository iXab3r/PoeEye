using Microsoft.Extensions.DependencyInjection;
using Unity;
using Unity.Lifetime;

namespace PoeShared.Scaffolding;

public static class UnityServiceCollectionAdapterExtensions
{
    /// <summary>
    ///     Returns an IServiceCollection facade that immediately registers into the given Unity container.
    /// </summary>
    public static IServiceCollection AsServiceCollection(this IUnityContainer container)
    {
        return new UnityServiceCollectionAdapter(container);
    }

    private sealed class UnityServiceCollectionAdapter : IServiceCollection
    {
        private readonly IUnityContainer container;
        private readonly List<ServiceDescriptor> descriptors = new();

        public UnityServiceCollectionAdapter(IUnityContainer container)
        {
            this.container = container;
        }

        public ServiceDescriptor this[int index]
        {
            get => descriptors[index];
            set
            {
                RegisterDescriptor(value, true);
                descriptors[index] = value;
                // re-register updated descriptor
            }
        }

        public int Count => descriptors.Count;
        public bool IsReadOnly => false;

        public void Add(ServiceDescriptor item)
        {
            RegisterDescriptor(item, false);
            descriptors.Add(item);
        }

        public void Clear()
        {
            // Unity doesn't have a simple "clear all" registrations API.
            // We clear only local bookkeeping; existing Unity registrations remain.
            descriptors.Clear();
        }

        public bool Contains(ServiceDescriptor item)
        {
            return descriptors.Contains(item);
        }

        public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
        {
            descriptors.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ServiceDescriptor> GetEnumerator()
        {
            return descriptors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(ServiceDescriptor item)
        {
            return descriptors.IndexOf(item);
        }

        public void Insert(int index, ServiceDescriptor item)
        {
            RegisterDescriptor(item, false);
            descriptors.Insert(index, item);
        }

        public bool Remove(ServiceDescriptor item)
        {
            var removed = descriptors.Remove(item);
            // Note: We do not attempt to deregister from Unity here (Unity APIs do not support clean unregistration by descriptor).
            return removed;
        }

        public void RemoveAt(int index)
        {
            var item = descriptors[index];
            descriptors.RemoveAt(index);
            // As above, we don't deregister from Unity.
        }

        // ----- Core mapping -----

        private void RegisterDescriptor(ServiceDescriptor d, bool replaceExisting)
        {
            var lifetime = ToUnityLifetime(d.Lifetime);

            // For multiple registrations of the same service type (IEnumerable<T> semantics),
            // we keep the first as the default (unnamed) and subsequent ones as named entries.
            // ResolveAll<T>/IEnumerable<T> in Unity will see all of them.
            var uniqueName = NeedsName(d.ServiceType) 
                ? Guid.NewGuid().ToString("N") 
                : null;

            if (d.ImplementationInstance != null)
            {
                // Singleton instance
                container.RegisterInstance(d.ServiceType, uniqueName, d.ImplementationInstance,
                    new ContainerControlledLifetimeManager());
                return;
            }

            if (d.ImplementationFactory != null)
            {
                // Factory: wrap Func<IServiceProvider, object> with Unity InjectionFactory
                var factory = d.ImplementationFactory;
                container.RegisterFactory(
                    d.ServiceType,
                    uniqueName,
                    c => factory(new UnityServiceProviderAdapter(c)),
                    (IFactoryLifetimeManager) lifetime);
                return;
            }

            if (d.ImplementationType != null)
            {
                // Type->Type
                container.RegisterType(
                    d.ServiceType,
                    d.ImplementationType,
                    uniqueName,
                    lifetime);
                return;
            }

            // Fallback: serviceType == implementationType (rare)
            container.RegisterType(d.ServiceType, d.ServiceType, uniqueName, lifetime);
        }

        private static ITypeLifetimeManager ToUnityLifetime(ServiceLifetime lifetime)
        {
            return lifetime switch
            {
                ServiceLifetime.Singleton => new ContainerControlledLifetimeManager(),
                ServiceLifetime.Scoped => new HierarchicalLifetimeManager(),
                var _ => new TransientLifetimeManager()
            };
        }

        private bool NeedsName(Type serviceType)
        {
            // If we already registered this service type once, subsequent registrations get a unique name
            // to preserve "multiple registrations" semantics for IEnumerable<T>.
            // This is a simple heuristic; you can expand it if you need finer control.
            for (var i = 0; i < descriptors.Count; i++)
            {
                if (descriptors[i].ServiceType == serviceType)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    ///     Minimal IServiceProvider adapter over Unity to support ImplementationFactory delegates.
    /// </summary>
    private sealed class UnityServiceProviderAdapter : IServiceProvider
    {
        private readonly IUnityContainer container;

        public UnityServiceProviderAdapter(IUnityContainer container)
        {
            this.container = container;
        }

        public object? GetService(Type serviceType)
        {
            // TryResolve: return null when not registered (Unity throws by default).
            try
            {
                return container.Resolve(serviceType);
            }
            catch
            {
                return null;
            }
        }
    }
}