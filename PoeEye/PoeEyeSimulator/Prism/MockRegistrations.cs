﻿namespace PoeEye.Simulator.Prism
{
    using Microsoft.Practices.Unity;

    using PoeShared.PoeTrade;

    using PoeTrade;

    public sealed class MockRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IPoeApi, PoeTradeApi>();
        }
    }
}