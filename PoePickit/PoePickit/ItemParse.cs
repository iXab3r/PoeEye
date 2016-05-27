using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PoePricer.Parser;

namespace PoePricer
{
    public class ItemParse
    {
        public readonly IDictionary<AffixBracketType, AffixBracketsSource> KnownAffixBrackets;

        public readonly IDictionary<AffixTypes, AffixesSource> KnownAffixes;

        public readonly IDictionary<BaseItemTypes, BaseItemsSource> KnownBaseItems;

        public readonly IDictionary<ItemClassType, FilterSource> KnownFilters;

        public readonly IDictionary<ParseRegEx, Regex> KnownRegexes;

        public UniqueAffixSource Uniques;


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
            KnownAffixBrackets = Enum.GetValues(typeof(AffixBracketType))
                .Cast<AffixBracketType>()
                .ToDictionary(x => x, x => new AffixBracketsSource(x.ToString(), KnownRegexes));
            KnownBaseItems = Enum.GetValues(typeof(BaseItemTypes))
                .Cast<BaseItemTypes>()
                .ToDictionary(x => x, x => new BaseItemsSource(x.ToString()));
            KnownAffixes = Enum.GetValues(typeof(AffixTypes))
                .Cast<AffixTypes>()
                .ToDictionary(x => x, x => new AffixesSource(x.ToString(), KnownRegexes));
            KnownFilters = Enum.GetValues(typeof(ItemClassType))
                .Cast<ItemClassType>()
                .ToDictionary(x => x, x => new FilterSource(x));

            Uniques = new UniqueAffixSource();

            FilterTier = FilterTiers.low;
        }

        public FilterTiers FilterTier { get; set; }


        private static IDictionary<ParseRegEx, Regex> SetRegExps()
        {
            var knownRegexps = new Dictionary<ParseRegEx, Regex>
            {
                {ParseRegEx.RegExItemRarityLine, new Regex(@"^Rarity: (?'rarity'[A-Za-z ']+)\r", RegexOptions.Compiled)},
                {ParseRegEx.RegexNoteMessageLine, new Regex(@"^Note: (?'noteMessage'.*)$", RegexOptions.Compiled)},
                {
                    ParseRegEx.RegexSocket6,
                    new Regex(@"^Sockets: [RGBW]-[RGBW]-[RGBW]-[RGBW]-[RGBW]-[RGBW] $", RegexOptions.Compiled)
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
                        @" (Hat|Helm|Helmet|Bascinet|Burgonet|Cap|Tricorne|Hood|Pelt|Circlet|Cage|Sallet|Coif|Crown|Mask)$",
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
                },
                {
                    ParseRegEx.RegexUniqueValue, new Regex(@"(\+|^| )(?'value'[0-9\.,]+)(%| )", RegexOptions.Compiled)
                },
                {
                    ParseRegEx.RegexUniqueValueDouble,
                    new Regex(@"(\+|^| )(?'valueLo'[0-9\.,]+)-(?'valueHi'[0-9\.,]+)(%| )", RegexOptions.Compiled)
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
            var result = item.ParseItemDataText(itemData, KnownAffixBrackets, KnownBaseItems, KnownAffixes, KnownRegexes,
                Uniques);
            if ((result == Item.ParseResult.WrongDataText) || (result == Item.ParseResult.NotRareOrUnique))
            {
                return null;
            }

            if (result == Item.ParseResult.Unidentified)
            {
                return PoeToolTip.Empty;
            }


            if (item.ClassType == ItemClassType.Unknown)
                return null;

            FilterSource filter;
            if (KnownFilters.TryGetValue(item.ClassType, out filter))
            {
                item.TtTypes = new List<ToolTipTypes> {ToolTipTypes.Common};
                if (item.ItemRarity == Item.ItemRarityType.Rare)
                    filter.Scoring(item);
            }
            else
            {
                Console.WriteLine($"[Item.Scoring] Wrong classtype: {item.ClassType}");
                return null;
            }

            return new PoeToolTip(item);
        }
    }
}