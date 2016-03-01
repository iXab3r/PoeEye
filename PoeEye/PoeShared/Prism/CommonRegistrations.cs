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
                .RegisterSingleton<IClock, Clock>()
                .RegisterSingleton<IPoeDatabaseReader, PoeDatabaseReader>()
                .RegisterSingleton<IPoeItemParser, PoeItemParser>()
                .RegisterSingleton<IEqualityComparer<IPoeItem>, PoeItemEqualityComparer>()
                .RegisterSingleton<IConverter<IPoeItem, IPoeQueryInfo>, PoeItemToPoeQueryConverter>()
                .RegisterSingleton<IRandomNumberGenerator, RandomNumberGenerator>();

            Container
                .RegisterType(typeof (IPoeModsProcessor), typeof (PoeModsProcessor))
                .RegisterType(typeof (IFactory<,,>), typeof (Factory<,,>))
                .RegisterType(typeof (IFactory<,>), typeof (Factory<,>))
                .RegisterType(typeof (IFactory<>), typeof (Factory<>));
        }
    }
}