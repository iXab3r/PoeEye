using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace PoePricer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Extensions;

    using Parser;

    public sealed class PoePricer : IPoePricer
    {
        private readonly IDictionary<AffixBracketType, AffixBrackets> knownAffixBrackets;

        private readonly IDictionary<BaseItemTypes, BaseItems> knownBaseItems;
        

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
            knownBaseItems = Enum.GetValues(typeof(BaseItemTypes)).Cast<BaseItemTypes>().ToDictionary(x => x, x => new BaseItems(x.ToString()));

            foreach (var affixBracketsSource in knownAffixBrackets)
            {
                ConsoleExtensions.WriteLine($"[PoePricer..ctor] Loaded affixes for '{affixBracketsSource.Key}', values count: {affixBracketsSource.Value.Brackets.Length}", ConsoleColor.DarkYellow);
            }

            foreach (var baseItem in knownBaseItems)
            {
                ConsoleExtensions.WriteLine($"[PoePricer..ctor] Loaded affixes for '{baseItem.Key}', values count: {baseItem.Value.Items.Length}", ConsoleColor.DarkYellow);
            }
        }


        public void TrashOutput()
        {

            var glovesBases = knownBaseItems[BaseItemTypes.Gloves];
            int ar, ev, es;
            glovesBases.SetArmourBaseProperties("Iron Gauntlets", out ar, out ev, out es);
            Console.WriteLine($"Gloves AR: {ar} EV: {ev} ES: {es}");
            //glovesBases.DumpToConsole();
            //knownBaseItems[BaseItemTypes.Weapon].DumpToConsole();


            var tArmourBracket = knownAffixBrackets[AffixBracketType.Armour];
            var tPhysAccBracket = knownAffixBrackets[AffixBracketType.ComboLocalPhysAcc];
            //tBracket.DumpToConsole();
            //var tValue = knownAffixBrackets[AffixBracketType.Armour].GetRangeFromiLevel(40, "Armour_Hi");
            //int Min, Max;
            //tArmourBracket.GetAffixValueRangeFromAffixValue("Armour", 50,"Accuracy", out Min, out Max);
            //tPhysAccBracket.GetAffixValueRangeFromAffixValue("Accuracy", 30, "Phys", out Min, out Max);
            //Console.WriteLine($"Min: {Min} Max: {Max}");
            

            // RANGE

            //tBracket.GetAffixRange("Armour", 140, out Min, out Max);
            //Min.DumpToConsole();
            //Max.DumpToConsole();


            //tBracket.DumpToConsole();



        }





        public string CreateTooltip(string itemData)
        {
            var armourAffixes = knownAffixBrackets[AffixBracketType.Armour];
            armourAffixes.DumpToConsole();
            //knownAffixBrackets.DumpToConsole();
            
            
           //Console.WriteLine(knownAffixBrackets);
            //var accuracyRatingAffixes = knownAffixBrackets[AffixBracketType.AccuracyRating];

            //accuracyRatingAffixes.DumpToConsole();
            //var comboLocalPhysAcc = knownAffixBrackets[AffixBracketType.ComboLocalPhysAcc];
            //comboLocalPhysAcc.DumpToConsole();


            return "tooltip example";
        }

        
    }
}