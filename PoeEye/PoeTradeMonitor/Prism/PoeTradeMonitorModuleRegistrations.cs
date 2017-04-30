﻿using Microsoft.Practices.Unity;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.Services;
using PoeEye.TradeMonitor.Services.Notifications;
using PoeEye.TradeMonitor.Services.Parsers;
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
                .RegisterSingleton<PoeStashGridViewModel>()
                .RegisterSingleton<IPoeStashHighlightService, PoeStashGridViewModel>()
                .RegisterSingleton<IPoeStashService, PoeStashService>()
                .RegisterSingleton<IPoeNotifier, PoeNotifier>()
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