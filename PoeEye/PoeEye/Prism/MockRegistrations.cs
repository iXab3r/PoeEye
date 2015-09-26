namespace PoeEye.Prism
{
    using Microsoft.Practices.Unity;

    using PoeShared;
    using PoeShared.PoeTrade;

    internal sealed class MockRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IPoeApi, PoeApi>();
        }
    }
}