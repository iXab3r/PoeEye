﻿using Microsoft.Practices.Unity;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.Models.Parsers;
using PoeEye.TradeMonitor.ViewModels;
using PoeShared.Scaffolding;

namespace PoeEye.TradeMonitor.Prism
{
    internal sealed class PoeTradeMonitorModuleRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<INegotiationViewModel, NegotiationViewModel>();

            Container
                .RegisterSingleton<IPoeStashService, PoeStashService>()
                .RegisterSingleton<IPoeMacroCommandsProvider, PoeMacroCommandsService>();

            Container
                 .RegisterType<ITradeMonitorService>(
                     new InjectionFactory(unity => unity.Resolve<TradeMonitorService>(
                             new DependencyOverride<IPoeMessageParser[]>(
                                     new IPoeMessageParser[]
                                     {
                                        unity.Resolve<PoeMessageStrictParserPoeTrade>(),
                                        unity.Resolve<PoeMessageWeakParserPoeTrade>(),
                                        unity.Resolve<PoeMessageCurrencyParserPoeTrade>(),
                                     }
                                 )
                         ))
                 );
        }
    }
}