using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace PoePickit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Extensions;

    using Parser;

    public enum ParseRegEx
    {
        RegExItemRarityLine,
        RegexNoteMessageLine,
        RegexSocket6,
        RegexSocket5,
        RegexSocket4,
        RegexSocket3,
        RegexSocket2,
        Regex1HWeaponClassLine,
        Regex2HWeaponClassLine,
        RegexRingClassLine,
        RegexBeltClassLine,
        RegexShieldClassLine,
        RegexBootsClassLine,
        RegexHelmClassLine,
        RegexGlovesClassLine,
        RegexComboAffixBracketLine,
        RegexLightAffixBracketLine,
        RegexAffixBracketLine,
        RegexAffixFileLine,
        RegexAffixFileArg
    }

    public class ItemParse 
    {
        public readonly IDictionary<AffixBracketType, AffixBracketsSource> KnownAffixBrackets;

        public readonly IDictionary<BaseItemTypes, BaseItemsSource> KnownBaseItems;

        public readonly IDictionary<AffixTypes, AffixesSource> KnownAffixes;

        public readonly IDictionary<FilterTypes, FilterSource> KnownFilters;

        public readonly IDictionary<ParseRegEx, Regex> KnownRegexes;

    
       

        

        public ItemParse()
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
                    "SPD",
                    "ComboStaffSpellMana",
                    "StaffSpellDamage",
                    "StunRecovery"
                }.ToDictionary(x => (AffixBracketType)Enum.Parse(typeof(AffixBracketType), x), x => new AffixBracketsSource(x));
            */
       

            KnownRegexes = SetRegExps();
            KnownAffixBrackets = Enum.GetValues(typeof(AffixBracketType)).Cast<AffixBracketType>().ToDictionary(x => x, x => new AffixBracketsSource(x.ToString(), KnownRegexes));
            KnownBaseItems = Enum.GetValues(typeof(BaseItemTypes)).Cast<BaseItemTypes>().ToDictionary(x => x, x => new BaseItemsSource(x.ToString()));
            KnownAffixes = Enum.GetValues(typeof(AffixTypes)).Cast<AffixTypes>().ToDictionary(x => x, x => new AffixesSource(x.ToString(), KnownRegexes));

            KnownFilters = Enum.GetValues(typeof (FilterTypes)).Cast<FilterTypes>().ToDictionary(x => x, x => new FilterSource(x.ToString()));

            


            foreach (var affixBracketsSource in KnownAffixBrackets)
            {
                ConsoleExtensions.WriteLine($"[PoePricer..ctor] Loaded affix brackets for '{affixBracketsSource.Key}', values count: {affixBracketsSource.Value.Brackets.Length}", ConsoleColor.DarkYellow);
            }

            foreach (var baseItem in KnownBaseItems)
            {
                ConsoleExtensions.WriteLine($"[PoePricer..ctor] Loaded base items for '{baseItem.Key}', values count: {baseItem.Value.Items.Length}", ConsoleColor.DarkYellow);
            }
            foreach (var knownAffixType in KnownAffixes)
            {
                ConsoleExtensions.WriteLine($"[PoePricer..ctor] Loaded affixes for '{knownAffixType.Key}', values count: {knownAffixType.Value.AffixesLines.Length}", ConsoleColor.DarkYellow);
            }
        }

        private static IDictionary<ParseRegEx, Regex> SetRegExps()
        {
            var knownRegexps = new Dictionary<ParseRegEx, Regex>
            {
                {ParseRegEx.RegExItemRarityLine, new Regex(@"^Rarity: (?'rarity'[A-Za-z ']+)\r", RegexOptions.Compiled)},
                {ParseRegEx.RegexNoteMessageLine, new Regex(@"^Note: (?'noteMessage'.*)$", RegexOptions.Compiled)},
                {
                    ParseRegEx.RegexSocket6,
                    new Regex(@"^Sockets: [RGBW]-[RGBW]-[RGBW]-[RGBW]-[RGBW]-[RGBW]$", RegexOptions.Compiled)
                },
                {
                    ParseRegEx.RegexSocket5,
                    new Regex(@"^Sockets: .*[RGBW]-[RGBW]-[RGBW]-[RGBW]-[RGBW].*$", RegexOptions.Compiled)
                },
                {
                    ParseRegEx.RegexSocket4,
                    new Regex(@"^Sockets: .*[RGBW]-[RGBW]-[RGBW]-[RGBW].*$", RegexOptions.Compiled)
                },
                {ParseRegEx.RegexSocket3, new Regex(@"^Sockets: .*[RGBW]-[RGBW]-[RGBW].*$", RegexOptions.Compiled)},
                {ParseRegEx.RegexSocket2, new Regex(@"^Sockets: .*[RGBW]-[RGBW].*$", RegexOptions.Compiled)},
                {
                    ParseRegEx.Regex1HWeaponClassLine,
                    new Regex(@"^(?'weaponClass'(One Handed Axe|One Handed Mace|One Handed Sword|Wand|Dagger|Claw))$",
                        RegexOptions.Compiled)
                },
                {
                    ParseRegEx.Regex2HWeaponClassLine,
                    new Regex(@"^(?'weaponClass'Two Handed Axe|Two Handed Mace|Bow|Two Handed Sword|Staff)$",
                        RegexOptions.Compiled)
                },
                {ParseRegEx.RegexRingClassLine, new Regex(@"Ring$", RegexOptions.Compiled)},
                {ParseRegEx.RegexBeltClassLine, new Regex(@"(Belt|Sash)$", RegexOptions.Compiled)},
                {ParseRegEx.RegexShieldClassLine, new Regex(@"(Shield|Bundle|Buckler)$", RegexOptions.Compiled)},
                {ParseRegEx.RegexBootsClassLine, new Regex(@"(Boots|Greaves|Shoes|Slippers)$", RegexOptions.Compiled)},
                {
                    ParseRegEx.RegexHelmClassLine,
                    new Regex(
                        @" (Hat|Helm|Bascinet|Burgonet|Cap|Tricorne|Hood|Pelt|Circlet|Cage|Sallet|Coif|Crown|Mask)$",
                        RegexOptions.Compiled)
                },
                {ParseRegEx.RegexGlovesClassLine, new Regex(@"(Gauntlets|Gloves|Mitts)$", RegexOptions.Compiled)},
                {
                    ParseRegEx.RegexComboAffixBracketLine,
                    new Regex(
                        @"^(?'itemLevel'\d+)\t+(?'valueLo'\d+)\-(?'valueHi'\d+)\t+(?'secondValueLo'\d+)\-(?'secondValueHi'\d+)[ ]*$",
                        RegexOptions.Compiled)
                },
                {
                    ParseRegEx.RegexLightAffixBracketLine,
                    new Regex(
                        @"^(?'itemLevel'\d+)\t+(?'valueLo'\d+)\-(?'valueHi'\d+)[%]*\t+(?'secondValueLo'\d+)[ ]*$",
                        RegexOptions.Compiled)
                },
                {
                    ParseRegEx.RegexAffixBracketLine,
                    new Regex(@"^(?'itemLevel'\d+)\t+(?'valueLo'\d+)\-(?'valueHi'\d+)[ ]*$", RegexOptions.Compiled)
                },
                {
                    ParseRegEx.RegexAffixFileLine,
                    new Regex(
                        @"^(?'affixRegexpPart'[\^A-Za-z\?\(\)\-\–\'\\\+\$ %\.1]+)\t*(?'affixArgsPart'([A-Za-z]+[23]{0,2}\t*)*) *(;|$)",
                        RegexOptions.Compiled)
                },
                {
                    ParseRegEx.RegexAffixFileArg,
                    new Regex(@"^(?'argName'[A-Za-z]+)(?'argMod'[23]{0,2})$", RegexOptions.Compiled)
                }
            };

            return knownRegexps;
        }

        public PoeToolTip CreateTooltip(string itemData)
        {
            if (itemData == null)
            {
                Console.WriteLine("EmptyText");
                
                return null;
            }
            var item = new Item();
            var result = item.ParseItemDataText(itemData, KnownAffixBrackets, KnownBaseItems, KnownAffixes, KnownRegexes);
            if ((result == Item.ParseResult.WrongDataText) || (result == Item.ParseResult.NotRare))
            {
                return null;
            }

            if (result == Item.ParseResult.Unidentified)
            {
                return PoeToolTip.Empty;
            }

            FilterTypes type;
            if (Enum.TryParse(item.ClassType.Replace(" ", ""), out type))
            {
                FilterSource filter;
                if (KnownFilters.TryGetValue(type, out filter))
                {
                    item.TtTypes = new List<ToolTipTypes>() { ToolTipTypes.Common };
                    filter.Scoring(item);
                }
                else
                {
                    Console.WriteLine($"[Item.Scoring] Wrong classtype: {item.ClassType}");
                    return null;
                }
            }

            return new PoeToolTip(item);
        }

    }
}