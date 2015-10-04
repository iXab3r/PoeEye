namespace PoeShared.Prism
{
    using System.Collections.Generic;

    using Common;

    using Factory;

    using Microsoft.Practices.Unity;

    using PoeDatabase;

    public sealed class CommonRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IPoeDatabaseReader, PoeDatabaseReader>(new ContainerControlledLifetimeManager());

            Container
                .RegisterType(typeof (IEqualityComparer<IPoeItem>), typeof (PoeItemEqualityComparer))
                .RegisterType(typeof (IFactory<,>), typeof(Factory<,>))
                .RegisterType(typeof (IFactory<>), typeof (Factory<>));
        }
    }
}