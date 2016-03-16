using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Collections.Generic;

namespace PoePricer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Extensions;

    using Parser;

    public class PoePricer : IPoePricer
    {
        private readonly IDictionary<AffixBracketType, AffixBrackets> knownAffixBrackets;

        private readonly IDictionary<BaseItemTypes, BaseItemsSource> knownBaseItems;
        

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
                    "ComboStaffSpellMana",
                    "StaffSpellDamage",
                    "StunRecovery"
                }.ToDictionary(x => (AffixBracketType)Enum.Parse(typeof(AffixBracketType), x), x => new AffixBracketsSource(x));
            */

            knownAffixBrackets = Enum.GetValues(typeof(AffixBracketType)).Cast<AffixBracketType>().ToDictionary(x => x, x => new AffixBrackets(x.ToString()));
            knownBaseItems = Enum.GetValues(typeof(BaseItemTypes)).Cast<BaseItemTypes>().ToDictionary(x => x, x => new BaseItemsSource(x.ToString()));

            foreach (var affixBracketsSource in knownAffixBrackets)
            {
                ConsoleExtensions.WriteLine($"[PoePricer..ctor] Loaded affixes for '{affixBracketsSource.Key}', values count: {affixBracketsSource.Value.Brackets.Length}", ConsoleColor.DarkYellow);
            }

            foreach (var baseItem in knownBaseItems)
            {
                ConsoleExtensions.WriteLine($"[PoePricer..ctor] Loaded affixes for '{baseItem.Key}', values count: {baseItem.Value.Items.Length}", ConsoleColor.DarkYellow);
            }
        }


        public string CreateTooltip(string itemData)
        {

            var item = new Item();
            
            item.ParseItemDataText(itemData, knownAffixBrackets, knownBaseItems);
            item.Name.DumpToConsole();
            return "tooltip example";
        }

        
    }
}