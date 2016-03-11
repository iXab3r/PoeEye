namespace PoePricer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Extensions;

    using Parser;

    public sealed class PoePricer : IPoePricer
    {
        private readonly IDictionary<AffixBracketType, AffixBracketsSource> knownAffixBrackets;

        public PoePricer()
        {
            /*
                Could be implemented as
            
                 knownAffixBrackets = new[]
                {
                    "AccuracyLightRadius",
                    "AccuracyRating",
                    "Armour",
                    "ComboArmourStun",
                    "ComboSpellMana",
                    "LocalPhys",
                    "MaxMana",
                    "SpellDamage",
                    "StaffComboSpellMana",
                    "StaffSpellDamage",
                    "StunRecovery"
                }.ToDictionary(x => (AffixBracketType)Enum.Parse(typeof(AffixBracketType), x), x => new AffixBracketsSource(x));
            */

            knownAffixBrackets = Enum
                .GetValues(typeof(AffixBracketType))
                .Cast<AffixBracketType>()
                .ToDictionary(x => x, x => new AffixBracketsSource(x.ToString()));

            foreach (var affixBracketsSource in knownAffixBrackets)
            {
                ConsoleExtensions.WriteLine($"[PoePricer..ctor] Loaded affixes for '{affixBracketsSource.Key}', values count: {affixBracketsSource.Value}", ConsoleColor.DarkYellow);
            }
        }

        public string CreateTooltip(string itemData)
        {
            var armourAffixes = knownAffixBrackets[AffixBracketType.Armour];
            armourAffixes.DumpToConsole();

            return "tooltip example";
        }
    }
}