namespace PoeShared.Prism
{
    using System.Collections.Generic;

    using Common;

    using Microsoft.Practices.Unity;

    using PoeDatabase;

    using PoeTrade;

    using TypeConverter;

    public sealed class CommonRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IPoeDatabaseReader, PoeDatabaseReader>(new ContainerControlledLifetimeManager());

            Container
                .RegisterType<IPoeItemParser, PoeItemParser>(new ContainerControlledLifetimeManager())
                .RegisterType<IRandomNumberGenerator, RandomNumberGenerator>(new ContainerControlledLifetimeManager());

            Container
                .RegisterType<IClock, Clock>(new ContainerControlledLifetimeManager())
                .RegisterType(typeof (IEqualityComparer<IPoeItem>), typeof (PoeItemEqualityComparer))
                .RegisterType(typeof (IConverter<IPoeItem, IPoeQueryInfo>), typeof (PoeItemToPoeQueryConverter))
                .RegisterType(typeof (IPoeModsProcessor), typeof (PoeModsProcessor))
                .RegisterType(typeof (IFactory<,>), typeof (Factory<,>))
                .RegisterType(typeof (IFactory<,,>), typeof (Factory<,,>))
                .RegisterType(typeof (IFactory<>), typeof (Factory<>));
        }
    }
}