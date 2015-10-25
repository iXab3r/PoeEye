namespace PoeShared.PoeTrade
{
    using System.Linq;
    using System.Text.RegularExpressions;

    using Common;

    using Guards;

    using JetBrains.Annotations;

    using Query;

    internal sealed class PoeModsProcessor : IPoeModsProcessor
    {
        private readonly PoeModParser[] modsRegexes;

        public PoeModsProcessor([NotNull] IPoeQueryInfoProvider queryInfoProvider)
        {
            Guard.ArgumentNotNull(() => queryInfoProvider);

            modsRegexes = PrepareModsInfo(queryInfoProvider);
        }

        public PoeModParser[] GetKnownParsers()
        {
            return modsRegexes;
        }

        private static PoeModParser[] PrepareModsInfo(IPoeQueryInfoProvider provider)
        {
            var mods = provider.ModsList;
            return mods.Select(PrepareModInfo).ToArray();
        }

        private static PoeModParser PrepareModInfo(IPoeItemMod mod)
        {
            const string digitPlaceholder = "DIGITPLACEHOLDER";
            var escapedRegexText = Regex.Escape(mod.CodeName.Replace("#", digitPlaceholder));
            var regexText = "^" + escapedRegexText.Replace(digitPlaceholder, @"(\d*?)") + "$";
            var regex = new Regex(regexText, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return new PoeModParser(mod, regex);
        }
    }
}