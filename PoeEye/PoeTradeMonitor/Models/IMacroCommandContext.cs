using JetBrains.Annotations;

namespace PoeEye.TradeMonitor.Models
{
    internal interface IMacroCommandContext
    {
        INegotiationCloseController CloseController { [CanBeNull] get; }

        TradeModel Negotiation { get; }
    }
}