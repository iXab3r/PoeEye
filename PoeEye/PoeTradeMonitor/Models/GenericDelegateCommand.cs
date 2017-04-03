using System;
using Guards;
using JetBrains.Annotations;
using PoeShared;

namespace PoeEye.TradeMonitor.Models
{
    internal sealed class GenericDelegateCommand : MacroCommand
    {
        private readonly Action<IMacroCommandContext> action;

        public GenericDelegateCommand([NotNull] string commandText, Action<IMacroCommandContext> action) : base(commandText)
        {
            Guard.ArgumentNotNull(action, nameof(action));

            this.action = action;
        }

        public override void Execute(IMacroCommandContext context)
        {
            Guard.ArgumentNotNull(context, nameof(context));

            try
            {
                Log.Instance.Debug($"[TradeMonitor.GenericDelegateCommand] Executing commandName '{CommandText}'...");
                action(context);
            }
            catch (Exception e)
            {
                Log.HandleException(e);
            }
            finally
            {
                Log.Instance.Debug($"[TradeMonitor.GenericDelegateCommand] Executed commandName '{CommandText}'");
            }
        }
    }
}
