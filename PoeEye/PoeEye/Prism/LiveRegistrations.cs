namespace PoeEye.Prism
{
    using Communications;

    using Microsoft.Practices.Unity;

    using PoeShared.Http;
    using PoeShared.PoeTrade;

    using PoeTrade;

    public sealed class LiveRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IHttpClient, GenericHttpClient>();

            Container
                .RegisterType<IPoeApi, PoeTradeApi>()
                .RegisterType<IPoeTradeParser, PoeTradeParserModern>();
        }
    }
}