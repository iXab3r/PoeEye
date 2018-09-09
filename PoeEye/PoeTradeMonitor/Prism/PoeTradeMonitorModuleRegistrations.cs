using Unity; using Unity.Resolution; using Unity.Attributes;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.Services;
using PoeEye.TradeMonitor.Services.Notifications;
using PoeEye.TradeMonitor.Services.Parsers;
using PoeEye.TradeMonitor.ViewModels;
using PoeShared.Scaffolding;
using Unity.Extension;
using Unity.Injection;

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