using System;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeShared;

namespace PoeEye.TradeMonitor.Models
{
    internal sealed class GenericDelegateCommand : MacroCommand
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(GenericDelegateCommand));

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
                Log.Debug($"[TradeMonitor.GenericDelegateCommand] Executing commandName '{CommandText}'...");
                action(context);
            }
            catch (Exception e)
            {
                Log.HandleException(e);
            }
            finally
            {
                Log.Debug($"[TradeMonitor.GenericDelegateCommand] Executed commandName '{CommandText}'");
            }
        }
    }
}