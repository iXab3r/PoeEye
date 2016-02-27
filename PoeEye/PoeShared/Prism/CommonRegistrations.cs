namespace PoeShared.Prism
{
    using System.Collections.Generic;

    using Common;

    using Microsoft.Practices.Unity;

    using PoeDatabase;

    using PoeTrade;

    using Scaffolding;

    using TypeConverter;

    public sealed class CommonRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IPoeDatabaseReader, PoeDatabaseReader>();

            Container
                .RegisterSingleton<IPoeItemParser, PoeItemParser>()
                .RegisterSingleton<IRandomNumberGenerator, RandomNumberGenerator>();

            Container
                .RegisterSingleton<IClock, Clock>()
                .RegisterType(typeof (IEqualityComparer<IPoeItem>), typeof (PoeItemEqualityComparer))
                .RegisterType(typeof (IConverter<IPoeItem, IPoeQueryInfo>), typeof (PoeItemToPoeQueryConverter))
                .RegisterType(typeof (IPoeModsProcessor), typeof (PoeModsProcessor))
                .RegisterType(typeof (IFactory<,>), typeof (Factory<,>))
                .RegisterType(typeof (IFactory<,,>), typeof (Factory<,,>))
                .RegisterType(typeof (IFactory<>), typeof (Factory<>));
        }
    }
}