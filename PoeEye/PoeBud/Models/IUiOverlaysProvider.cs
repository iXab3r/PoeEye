using JetBrains.Annotations;
using ReactiveUI;

namespace PoeBud.Models
{
    public interface IUiOverlaysProvider
    {
        IReactiveList<UiOverlayInfo> OverlaysList { [NotNull] get; }
    }
}