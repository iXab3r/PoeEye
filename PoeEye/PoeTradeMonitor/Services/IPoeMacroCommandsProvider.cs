using JetBrains.Annotations;
using PoeEye.TradeMonitor.Models;
using ReactiveUI;

namespace PoeEye.TradeMonitor.Services
{
    internal interface IPoeMacroCommandsProvider
    {
        IReactiveList<MacroCommand> MacroCommands { [NotNull] get; }
    }
}