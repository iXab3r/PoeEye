using JetBrains.Annotations;
using PoeEye.TradeMonitor.Models;
using PoeShared.Scaffolding;

namespace PoeEye.TradeMonitor.ViewModels
{
    internal interface INegotiationViewModel : IDisposableReactiveObject
    {
        TradeModel Negotiation { [NotNull] get; }

        bool IsExpanded { get; set; }

        void UpdateModel([NotNull] TradeModel update);

        void SetCloseController([NotNull] INegotiationCloseController closeController);
    }
}