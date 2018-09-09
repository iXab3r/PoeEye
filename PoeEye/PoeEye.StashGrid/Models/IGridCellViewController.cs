using System.Windows.Media;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeEye.StashGrid.Models
{
    public interface IGridCellViewController : IDisposableReactiveObject
    {
        bool IsFresh { get; set; }

        Color? BorderColor { get; set; }

        string ToolTipText { [CanBeNull] get; [CanBeNull] set; }
    }
}