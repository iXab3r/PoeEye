namespace PoeEye.Simulator.Prism
{
    using Microsoft.Practices.Unity;

    using PoeShared;
    using PoeShared.PoeTrade;

    using PoeTrade;

    public sealed class MockRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IClock, Clock>()
                .RegisterType<IPoeApi, PoeTradeApi>();
        }
    }
}