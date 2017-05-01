using PoeShared.Scaffolding;

namespace PoeEye.TradeMonitor.ViewModels
{
    internal interface IGridCellViewController : IDisposableReactiveObject
    {
        bool IsFresh { get; set; }

        string ToolTipText { get; set; }
    }
}