using JetBrains.Annotations;
using ReactiveUI;

namespace PoeEye.TradeMonitor.Models
{
    internal interface IPoeMacroCommandsProvider
    {
        IReactiveList<MacroCommand> MacroCommands { [NotNull] get; }
    }
}