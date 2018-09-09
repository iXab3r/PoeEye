using JetBrains.Annotations;
using PoeEye.TradeMonitor.Models;
using PoeShared.Scaffolding;

namespace PoeEye.TradeMonitor.ViewModels
{
    internal interface INegotiationViewModel : IDisposableReactiveObject
    {
        TradeModel Negotiation { [NotNull] get; }

        void UpdateModel([NotNull] TradeModel update);

        bool IsExpanded { get; set; }

        void SetCloseController([NotNull] INegotiationCloseController closeController);
    }
}