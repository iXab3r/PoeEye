using System.Text.RegularExpressions;
using Guards;
using JetBrains.Annotations;

namespace PoeEye.TradeMonitor.Models
{
    internal abstract class MacroCommand
    {
        private readonly Regex matcher;

        protected MacroCommand([NotNull] string commandText)
        {
            Guard.ArgumentNotNull(commandText, nameof(commandText));

            CommandText = commandText;
            Label = commandText;

            matcher = new Regex($"/{commandText}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public string Label { [CanBeNull] get; set; }

        public string Description { [CanBeNull] get; set; }

        public string CommandText { [NotNull] get; protected set; }

        public object Icon { [CanBeNull] get; protected set; }

        public abstract void Execute([NotNull] IMacroCommandContext context);

        public Match TryToMatch([NotNull] string text, int startIdx)
        {
            Guard.ArgumentNotNull(text, nameof(text));

            return matcher.Match(text, startIdx);
        }

        public Match TryToMatch([NotNull] string text)
        {
            Guard.ArgumentNotNull(text, nameof(text));

            return TryToMatch(text, 0);
        }

        public string CleanupText([NotNull] string text)
        {
            Guard.ArgumentNotNull(text, nameof(text));

            var match = TryToMatch(text);
            if (!match.Success)
            {
                return text;
            }

            return text.Remove(match.Index, match.Length);
        }
    }
}
