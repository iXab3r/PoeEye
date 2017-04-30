using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PoeShared.Scaffolding;

namespace PoeShared.Common
{
    public static class KnownCurrencyNameList
    {
        public static readonly IDictionary<string, string> CurrencyByAlias;

        public static readonly string Unknown = "Unknown";
        public static readonly string ArmourersScrap = "armour";
        public static readonly string ApprenticeSextant = "apprentice-sextant";
        public static readonly string JourneymanSextant = "journeyman-sextant";
        public static readonly string MasterSextant = "master-sextant";
        public static readonly string BlacksmithsWhetstone = "blacksmith";

        public static readonly string ChaosOrb = "chaos";
        public static readonly string OrbOfAlteration = "alteration";
        public static readonly string BlessedOrb = "blessed";
        public static readonly string CartographersChisel = "chisel";
        public static readonly string GlassblowersBauble = "bauble";
        public static readonly string ChromaticOrb = "chromatic";
        public static readonly string DivineOrb = "divine";
        public static readonly string ExaltedOrb = "exalted";
        public static readonly string GemcuttersPrism = "gcp";
        public static readonly string JewellersOrb = "jewellers";
        public static readonly string OrbOfAlchemy = "alchemy";
        public static readonly string OrbOfFusing = "fusing";
        public static readonly string OrbOfChance = "chance";
        public static readonly string OrbOfRegret = "regret";
        public static readonly string OrbOfScouring = "scouring";
        public static readonly string RegalOrb = "regal";
        public static readonly string VaalOrb = "vaal";
        public static readonly string MirrorOfKalandra = "mirror";
        public static readonly string EternalOrb = "eternal";

        public static readonly string FragmentOfTheChimera = "chimera";
        public static readonly string FragmentOfTheHydra = "hydra";
        public static readonly string FragmentOfTheMinotaur = "minotaur";
        public static readonly string FragmentOfThePhoenix = "phoenix";
        public static readonly string FragmentSet = "shaper set";

        public static readonly string SilverCoin = "silver";
        public static readonly string SplinterOfChayula = "splinter of chayula";
        public static readonly string SplinterOfEsh = "splinter of esh";
        public static readonly string SplinterOfTul = "splinter of tul";
        public static readonly string SplinterOfUulNetol = "splinter of uul-netol";
        public static readonly string SplinterOfXoph = "splinter of xoph";

        public static readonly string MortalGrief = "grief";
        public static readonly string MortalHope = "hope";
        public static readonly string MortalIgnorance = "ignorance";
        public static readonly string MortalRage = "rage";
        public static readonly string MortalSet = "mortal-set";
        public static readonly string OfferingToTheGoddess = "offering";

        public static readonly string SacrificeSet = "sacrifice-set";
        public static readonly string SacrificeAtDawn = "dawn";
        public static readonly string SacrificeAtDusk = "dusk";
        public static readonly string SacrificeAtMidnight = "midnight";
        public static readonly string SacrificeAtNoon = "noon";

        public static readonly string InyasKey = "inya's";
        public static readonly string EbersKey = "eber's";
        public static readonly string VolkuursKey = "volkuur's";
        public static readonly string YrielsKey = "yriel's";
        public static readonly string PaleCourtSet = "pale-court-set";

        public static readonly string BlessingOfChayula = "blessing-of-chayula";
        public static readonly string BlessingOfEsh = "blessing-of-esh";
        public static readonly string BlessingOfTul = "blessing-of-tul";
        public static readonly string BlessingOfUulNetol = "blessing-of-uul-netol";
        public static readonly string BlessingOfXoph = "blessing-of-xoph";
        public static readonly string XophsBreachstone = "xophs-breachstone";
        public static readonly string TulsBreachstone = "tuls-breachstone";
        public static readonly string EshsBreachstone = "eshs-breachstone";
        public static readonly string UulNetolsBreachstone = "uul-netol-breachstone";
        public static readonly string ChayulasBreachstone = "chayulas-breachstone";
        public static readonly string EssenceOfDelirium = "essence-of-delirium";
        public static readonly string EssenceOfHorror = "essence-of-horror";
        public static readonly string EssenceOfHysteria = "essence-of-hysteria";
        public static readonly string EssenceOfInsanity = "essence-of-insanity";
        public static readonly string ScreamingEssenceOfAnger = "screaming-essence-of-anger";
        public static readonly string ShriekingEssenceOfAnger = "shrieking-essence-of-anger";
        public static readonly string DeafeningEssenceOfAnger = "deafening-essence-of-anger";
        public static readonly string ScreamingEssenceOfAnguish = "screaming-essence-of-anguish";
        public static readonly string ShriekingEssenceOfAnguish = "shrieking-essence-of-anguish";
        public static readonly string DeafeningEssenceOfAnguish = "deafening-essence-of-anguish";
        public static readonly string ScreamingEssenceOfContempt = "screaming-essence-of-contempt";
        public static readonly string ShriekingEssenceOfContempt = "shrieking-essence-of-contempt";
        public static readonly string DeafeningEssenceOfContempt = "deafening-essence-of-contempt";
        public static readonly string ScreamingEssenceOfDoubt = "screaming-essence-of-doubt";
        public static readonly string ShriekingEssenceOfDoubt = "shrieking-essence-of-doubt";
        public static readonly string DeafeningEssenceOfDoubt = "deafening-essence-of-doubt";
        public static readonly string ScreamingEssenceOfDread = "screaming-essence-of-dread";
        public static readonly string ShriekingEssenceOfDread = "shrieking-essence-of-dread";
        public static readonly string DeafeningEssenceOfDread = "deafening-essence-of-dread";
        public static readonly string ScreamingEssenceOfEnvy = "screaming-essence-of-envy";
        public static readonly string ShriekingEssenceOfEnvy = "shrieking-essence-of-envy";
        public static readonly string DeafeningEssenceOfEnvy = "deafening-essence-of-envy";
        public static readonly string ScreamingEssenceOfFear = "screaming-essence-of-fear";
        public static readonly string ShriekingEssenceOfFear = "shrieking-essence-of-fear";
        public static readonly string DeafeningEssenceOfFear = "deafening-essence-of-fear";
        public static readonly string ScreamingEssenceOfGreed = "screaming-essence-of-greed";
        public static readonly string ShriekingEssenceOfGreed = "shrieking-essence-of-greed";
        public static readonly string DeafeningEssenceOfGreed = "deafening-essence-of-greed";
        public static readonly string ScreamingEssenceOfHatred = "screaming-essence-of-hatred";
        public static readonly string ShriekingEssenceOfHatred = "shrieking-essence-of-hatred";
        public static readonly string DeafeningEssenceOfHatred = "deafening-essence-of-hatred";
        public static readonly string ScreamingEssenceOfLoathing = "screaming-essence-of-loathing";
        public static readonly string ShriekingEssenceOfLoathing = "shrieking-essence-of-loathing";
        public static readonly string DeafeningEssenceOfLoathing = "deafening-essence-of-loathing";
        public static readonly string ScreamingEssenceOfMisery = "screaming-essence-of-misery";
        public static readonly string ShriekingEssenceOfMisery = "shrieking-essence-of-misery";
        public static readonly string DeafeningEssenceOfMisery = "deafening-essence-of-misery";
        public static readonly string ScreamingEssenceOfRage = "screaming-essence-of-rage";
        public static readonly string ShriekingEssenceOfRage = "shrieking-essence-of-rage";
        public static readonly string DeafeningEssenceOfRage = "deafening-essence-of-rage";
        public static readonly string ScreamingEssenceOfScorn = "screaming-essence-of-scorn";
        public static readonly string ShriekingEssenceOfScorn = "shrieking-essence-of-scorn";
        public static readonly string DeafeningEssenceOfScorn = "deafening-essence-of-scorn";
        public static readonly string ScreamingEssenceOfSorrow = "screaming-essence-of-sorrow";
        public static readonly string ShriekingEssenceOfSorrow = "shrieking-essence-of-sorrow";
        public static readonly string DeafeningEssenceOfSorrow = "deafening-essence-of-sorrow";
        public static readonly string ScreamingEssenceOfSpite = "screaming-essence-of-spite";
        public static readonly string ShriekingEssenceOfSpite = "shrieking-essence-of-spite";
        public static readonly string DeafeningEssenceOfSpite = "deafening-essence-of-spite";
        public static readonly string ScreamingEssenceOfSuffering = "screaming-essence-of-suffering";
        public static readonly string ShriekingEssenceOfSuffering = "shrieking-essence-of-suffering";
        public static readonly string DeafeningEssenceOfSuffering = "deafening-essence-of-suffering";
        public static readonly string ScreamingEssenceOfTorment = "screaming-essence-of-torment";
        public static readonly string ShriekingEssenceOfTorment = "shrieking-essence-of-torment";
        public static readonly string DeafeningEssenceOfTorment = "deafening-essence-of-torment";
        public static readonly string ScreamingEssenceOfWoe = "screaming-essence-of-woe";
        public static readonly string ShriekingEssenceOfWoe = "shrieking-essence-of-woe";
        public static readonly string DeafeningEssenceOfWoe = "deafening-essence-of-woe";
        public static readonly string ScreamingEssenceOfWrath = "screaming-essence-of-wrath";
        public static readonly string ShriekingEssenceOfWrath = "shrieking-essence-of-wrath";
        public static readonly string DeafeningEssenceOfWrath = "deafening-essence-of-wrath";
        public static readonly string ScreamingEssenceOfZeal = "screaming-essence-of-zeal";
        public static readonly string ShriekingEssenceOfZeal = "shrieking-essence-of-zeal";
        public static readonly string DeafeningEssenceOfZeal = "deafening-essence-of-zeal";
        public static readonly string RemnantOfCorruption = "remnant-of-corruption";

        public static readonly string PerandusCoin = "coin";


        public static readonly IDictionary<string, string> KnownImages = new Dictionary<string, string>
        {
            {Unknown, "Unknown"},

            {PerandusCoin, "Perandus_Coin"},

            {InyasKey, "Inyas_Key"},
            {EbersKey, "Ebers_Key"},
            {VolkuursKey, "Volkuurs_Key"},
            {YrielsKey, "Yriels_Key"},

            {SacrificeSet, "Sacrifice_Set"},
            {SacrificeAtDawn, "Sacrifice_at_Dawn"},
            {SacrificeAtDusk, "Sacrifice_at_Dusk"},
            {SacrificeAtMidnight, "Sacrifice_at_Midnight"},
            {SacrificeAtNoon, "Sacrifice_at_Noon"},
            {PaleCourtSet, "Key_Set"},

            {MortalGrief, "Mortal_Grief"},
            {MortalHope, "Mortal_Hope"},
            {MortalIgnorance, "Mortal_Ignorance"},
            {MortalRage, "Mortal_Rage"},
            {MortalSet, "Mortal_Set"},
            {OfferingToTheGoddess, "Offering_to_the_Goddess"},

            {SilverCoin, "Silver_Coin"},
            {SplinterOfChayula, "Splinter_of_Chayula"},
            {SplinterOfEsh, "Splinter_of_Esh"},
            {SplinterOfTul, "Splinter_of_Tul"},
            {SplinterOfUulNetol, "Splinter_of_Uul_Netol"},
            {SplinterOfXoph, "Splinter_of_Xoph"},

            {FragmentOfTheChimera, "Fragment_of_the_Chimera"},
            {FragmentOfTheHydra, "Fragment_of_the_Hydra"},
            {FragmentOfTheMinotaur, "Fragment_of_the_Minotaur"},
            {FragmentOfThePhoenix, "Fragment_of_the_Phoenix"},
            {FragmentSet, "Fragment_Set"},

            {ArmourersScrap, "Armourers_Scrap"},
            {ApprenticeSextant, "Apprentice_Sextant"},
            {JourneymanSextant, "Journeyman_Sextant"},
            {MasterSextant, "Master_Sextant"},
            {BlessedOrb, "Blessed_Orb"},
            {BlacksmithsWhetstone, "Blacksmiths_Whetstone"},
            {CartographersChisel, "Cartographers_Chisel"},
            {GlassblowersBauble, "Glassblowers_Bauble"},
            {ChaosOrb, "Chaos_Orb"},
            {ChromaticOrb, "Chromatic_Orb"},
            {DivineOrb, "Divine_Orb"},
            {ExaltedOrb, "Exalted_Orb"},
            {GemcuttersPrism, "Gemcutters_Prism"},
            {JewellersOrb, "Jewellers_Orb"},
            {OrbOfAlchemy, "Orb_of_Alchemy"},
            {OrbOfAlteration, "Orb_of_Alteration"},
            {OrbOfChance, "Orb_of_Chance"},
            {OrbOfFusing, "Orb_of_Fusing"},
            {OrbOfRegret, "Orb_of_Regret"},
            {OrbOfScouring, "Orb_of_Scouring"},
            {RegalOrb, "Regal_Orb"},
            {VaalOrb, "Vaal_Orb"},
            {MirrorOfKalandra, "Mirror_of_Kalandra"},
            {EternalOrb, "Eternal_Orb"},

            {BlessingOfChayula, "Blessing_of_Chayula"},
            {BlessingOfEsh, "Blessing_of_Esh"},
            {BlessingOfTul, "Blessing_of_Tul"},
            {BlessingOfUulNetol, "Blessing_of_Uul-Netol"},
            {BlessingOfXoph, "Blessing_of_Xoph"},

            {XophsBreachstone, "Xophs_Breachstone"},
            {TulsBreachstone, "Tuls_Breachstone"},
            {EshsBreachstone, "Eshs_Breachstone"},
            {UulNetolsBreachstone, "Uul-Netols_Breachstone"},
            {ChayulasBreachstone, "Chayulas_Breachstone"},
            {EssenceOfDelirium, "Essence_of_Delirium_inventory_icon"},
            {EssenceOfHorror, "Essence_of_Horror_inventory_icon"},
            {EssenceOfHysteria, "Essence_of_Hysteria_inventory_icon"},
            {EssenceOfInsanity, "Essence_of_Insanity_inventory_icon"},
            {DeafeningEssenceOfAnger, "Deafening_Essence_of_Anger_inventory_icon"},
            {DeafeningEssenceOfAnguish, "Deafening_Essence_of_Anguish_inventory_icon"},
            {DeafeningEssenceOfContempt, "Deafening_Essence_of_Contempt_inventory_icon"},
            {DeafeningEssenceOfDoubt, "Deafening_Essence_of_Doubt_inventory_icon"},
            {DeafeningEssenceOfDread, "Deafening_Essence_of_Dread_inventory_icon"},
            {DeafeningEssenceOfEnvy, "Deafening_Essence_of_Envy_inventory_icon"},
            {DeafeningEssenceOfFear, "Deafening_Essence_of_Fear_inventory_icon"},
            {DeafeningEssenceOfGreed, "Deafening_Essence_of_Greed_inventory_icon"},
            {DeafeningEssenceOfHatred, "Deafening_Essence_of_Hatred_inventory_icon"},
            {DeafeningEssenceOfLoathing, "Deafening_Essence_of_Loathing_inventory_icon"},
            {DeafeningEssenceOfMisery, "Deafening_Essence_of_Misery_inventory_icon"},
            {DeafeningEssenceOfRage, "Deafening_Essence_of_Rage_inventory_icon"},
            {DeafeningEssenceOfScorn, "Deafening_Essence_of_Scorn_inventory_icon"},
            {DeafeningEssenceOfSorrow, "Deafening_Essence_of_Sorrow_inventory_icon"},
            {DeafeningEssenceOfSpite, "Deafening_Essence_of_Spite_inventory_icon"},
            {DeafeningEssenceOfSuffering, "Deafening_Essence_of_Suffering_inventory_icon"},
            {DeafeningEssenceOfTorment, "Deafening_Essence_of_Torment_inventory_icon"},
            {DeafeningEssenceOfWoe, "Deafening_Essence_of_Woe_inventory_icon"},
            {DeafeningEssenceOfWrath, "Deafening_Essence_of_Wrath_inventory_icon"},
            {DeafeningEssenceOfZeal, "Deafening_Essence_of_Zeal_inventory_icon"},
            {ScreamingEssenceOfAnger, "Screaming_Essence_of_Anger_inventory_icon"},
            {ScreamingEssenceOfAnguish, "Screaming_Essence_of_Anguish_inventory_icon"},
            {ScreamingEssenceOfContempt, "Screaming_Essence_of_Contempt_inventory_icon"},
            {ScreamingEssenceOfDoubt, "Screaming_Essence_of_Doubt_inventory_icon"},
            {ScreamingEssenceOfDread, "Screaming_Essence_of_Dread_inventory_icon"},
            {ScreamingEssenceOfEnvy, "Screaming_Essence_of_Envy_inventory_icon"},
            {ScreamingEssenceOfFear, "Screaming_Essence_of_Fear_inventory_icon"},
            {ScreamingEssenceOfGreed, "Screaming_Essence_of_Greed_inventory_icon"},
            {ScreamingEssenceOfHatred, "Screaming_Essence_of_Hatred_inventory_icon"},
            {ScreamingEssenceOfLoathing, "Screaming_Essence_of_Loathing_inventory_icon"},
            {ScreamingEssenceOfMisery, "Screaming_Essence_of_Misery_inventory_icon"},
            {ScreamingEssenceOfRage, "Screaming_Essence_of_Rage_inventory_icon"},
            {ScreamingEssenceOfScorn, "Screaming_Essence_of_Scorn_inventory_icon"},
            {ScreamingEssenceOfSorrow, "Screaming_Essence_of_Sorrow_inventory_icon"},
            {ScreamingEssenceOfSpite, "Screaming_Essence_of_Spite_inventory_icon"},
            {ScreamingEssenceOfSuffering, "Screaming_Essence_of_Suffering_inventory_icon"},
            {ScreamingEssenceOfTorment, "Screaming_Essence_of_Torment_inventory_icon"},
            {ScreamingEssenceOfWoe, "Screaming_Essence_of_Woe_inventory_icon"},
            {ScreamingEssenceOfWrath, "Screaming_Essence_of_Wrath_inventory_icon"},
            {ScreamingEssenceOfZeal, "Screaming_Essence_of_Zeal_inventory_icon"},
            {ShriekingEssenceOfAnger, "Shrieking_Essence_of_Anger_inventory_icon"},
            {ShriekingEssenceOfAnguish, "Shrieking_Essence_of_Anguish_inventory_icon"},
            {ShriekingEssenceOfContempt, "Shrieking_Essence_of_Contempt_inventory_icon"},
            {ShriekingEssenceOfDoubt, "Shrieking_Essence_of_Doubt_inventory_icon"},
            {ShriekingEssenceOfDread, "Shrieking_Essence_of_Dread_inventory_icon"},
            {ShriekingEssenceOfEnvy, "Shrieking_Essence_of_Envy_inventory_icon"},
            {ShriekingEssenceOfFear, "Shrieking_Essence_of_Fear_inventory_icon"},
            {ShriekingEssenceOfGreed, "Shrieking_Essence_of_Greed_inventory_icon"},
            {ShriekingEssenceOfHatred, "Shrieking_Essence_of_Hatred_inventory_icon"},
            {ShriekingEssenceOfLoathing, "Shrieking_Essence_of_Loathing_inventory_icon"},
            {ShriekingEssenceOfMisery, "Shrieking_Essence_of_Misery_inventory_icon"},
            {ShriekingEssenceOfRage, "Shrieking_Essence_of_Rage_inventory_icon"},
            {ShriekingEssenceOfScorn, "Shrieking_Essence_of_Scorn_inventory_icon"},
            {ShriekingEssenceOfSorrow, "Shrieking_Essence_of_Sorrow_inventory_icon"},
            {ShriekingEssenceOfSpite, "Shrieking_Essence_of_Spite_inventory_icon"},
            {ShriekingEssenceOfSuffering, "Shrieking_Essence_of_Suffering_inventory_icon"},
            {ShriekingEssenceOfTorment, "Shrieking_Essence_of_Torment_inventory_icon"},
            {ShriekingEssenceOfWoe, "Shrieking_Essence_of_Woe_inventory_icon"},
            {ShriekingEssenceOfWrath, "Shrieking_Essence_of_Wrath_inventory_icon"},
            {ShriekingEssenceOfZeal, "Shrieking_Essence_of_Zeal_inventory_icon"},
            {RemnantOfCorruption, "Remnant_of_Corruption_inventory_icon"}
        };

        private static readonly IDictionary<string, string> DefaultAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"offering", OfferingToTheGoddess},

            {"splinter-chayula", SplinterOfChayula},
            {"splinter-esh", SplinterOfEsh},
            {"splinter-tul", SplinterOfTul},
            {"splinter-uul-netol", SplinterOfUulNetol},
            {"splinter-xoph", SplinterOfXoph},

            {"phenix", FragmentOfThePhoenix},
            {"pheon", FragmentOfThePhoenix},

            {"Orb of Alteration", OrbOfAlteration},
            {"Alt", OrbOfAlteration},

            {"Blessed Orb", BlessedOrb},

            {"Whetstone", BlacksmithsWhetstone},

            {"Cartographer's Chisel", CartographersChisel},
            {"Cartographers Chisel", CartographersChisel},

            {"Chaos Orb", ChaosOrb},
            {"Chaos", ChaosOrb},

            {"Chromatic Orb", ChromaticOrb},
            {"Chrome", ChromaticOrb},
            {"Chrom", ChromaticOrb},

            {"Divine Orb", DivineOrb},

            {"Exalted Orb", ExaltedOrb},
            {"Ex", ExaltedOrb},
            {"Exa", ExaltedOrb},

            {"Gemcutter's Prism", GemcuttersPrism},
            {"Gemcutters Prism", GemcuttersPrism},

            {"Jewellers Orb", JewellersOrb},
            {"Jew", JewellersOrb},

            {"Orb of Alchemy", OrbOfAlchemy},
            {"Alch", OrbOfAlchemy},

            {"Orb of Fusing", OrbOfFusing},
            {"Fuse", OrbOfFusing},

            {"Orb of Chance", OrbOfChance},

            {"Orb of Regret", OrbOfRegret},

            {"Orb of Scouring", OrbOfScouring},
            {"Scour", OrbOfScouring},

            {"Regal Orb", RegalOrb},

            {"Vaal Orb", VaalOrb},
            {"Vaal", VaalOrb},

            {"Mirror of Kalandra", MirrorOfKalandra},
            {"Mirror", MirrorOfKalandra},

            {"Eternal Orb", EternalOrb},
            {"Eternal", EternalOrb},

            {"blessing-chayula", BlessingOfChayula}
        };

        static KnownCurrencyNameList()
        {
            var currencyByAlias = new Dictionary<string, string>(DefaultAliases, StringComparer.OrdinalIgnoreCase);
            var allCurrencies = EnumerateKnownCurrencies().ToArray();
            foreach (var currency in allCurrencies)
            {
                currencyByAlias[currency] = currency;
            }

            foreach (var currency in allCurrencies
                .Where(x => x.Contains("-")))
            {
                var formattedCurrency = currency.Replace("-", " ");
                currencyByAlias[formattedCurrency] = currency;
            }

            foreach (var currency in allCurrencies
                .Where(x => x.Contains("s-")))
            {
                var formattedCurrency = currency.Replace("s-", "'s ").Replace("-", " ");
                currencyByAlias[formattedCurrency] = currency;
            }

            DefaultAliases.Values.ForEach(x => currencyByAlias[x] = x);

            CurrencyByAlias = currencyByAlias;
        }

        public static IEnumerable<string> EnumerateKnownCurrencies()
        {
            var fields = typeof(KnownCurrencyNameList).GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (var fieldInfo in
                fields.Where(x => x.FieldType == typeof(string)))
            {
                var currency = fieldInfo.GetValue(null) as string;
                if (string.IsNullOrEmpty(currency))
                {
                    continue;
                }
                yield return currency;
            }
        }
    }
}