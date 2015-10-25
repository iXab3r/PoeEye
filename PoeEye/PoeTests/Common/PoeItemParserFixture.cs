namespace PoeEye.Tests.Common
{
    using System.Collections.Generic;

    using Moq;

    using NUnit.Framework;

    using PoeShared.Common;
    using PoeShared.PoeTrade.Query;

    using Shouldly;

    [TestFixture]
    public class PoeItemParserFixture
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        [TestCase("")]
        [TestCase("RNGSTR")]
        public void ShouldReturnNullNameOnUnexpectedInput(string data)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Parse(data);

            //Then
            result.ItemName.ShouldBe(null);
        }

        [Test]
        [TestCaseSource(nameof(KnownItems))]
        public void ShouldParseItemName(string data, IPoeItem expectedItem)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Parse(data);

            //Then
            result.ItemName.ShouldBe(expectedItem.ItemName);
        }

        private IEnumerable<TestCaseData> KnownItems()
        {
            yield return new TestCaseData(
                @"Rarity: Rare
                  Gloom Nails
                  Wyrmscale Gauntlets
                  --------
                  Quality: +1%
                  Armour: 141 (augmented)
                  Evasion Rating: 141 (augmented)
                  --------
                  Requirements:
                  Level: 70
                  Str: 155
                  Dex: 38
                  Int: 111
                  --------
                  Sockets: B-R-B-R 
                  --------
                  Item Level: 80
                  --------
                  Adds 1-5 Lightning Damage to Attacks
                  +64 to maximum Life
                  +39% to Fire Resistance
                  +42% to Cold Resistance
                  +25% to Chaos Resistance
                  63% increased Armour and Evasion
                  --------
                  Has Infernal Gloves",
                Mock.Of<IPoeItem>(x => x.Rarity == PoeItemRarity.Rare && 
                                       x.ItemName == "Gloom Nails" &&
                                       x.Quality == "1%" &&
                                       x.Links == Mock.Of<IPoeLinksInfo>(y => y.RawSockets == "B-R-B-R")));

            yield return new TestCaseData(
                 @"Rarity: Gem
                  Vengeance
                  --------
                  Trigger, Attack, AoE, Melee
                  Level: 1
                  Mana Cost: 0
                  Cooldown Time: 1.20 sec
                  Quality: +14% (augmented)
                  Experience: 1/118 383
                  --------
                  Requirements:
                  Level: 24
                  Str: 58
                  --------
                  Deals 75% of Base Attack Damage
                  37% chance to Counterattack with this Skill when Hit
                  You cannot use this Attack directly
                  --------
                  Place into an item socket of the right colour to gain this skill. Right click to remove from a socket.",
                Mock.Of<IPoeItem>(x => x.Rarity == PoeItemRarity.Rare && 
                                       x.ItemName == "Vengeance" &&
                                       x.Quality == "14% (augmented)"));

            yield return new TestCaseData(
                  @"Rarity: Unique
                  Lightning Coil
                  Desert Brigandine
                  --------
                  Quality: +20% (augmented)
                  Armour: 608 (augmented)
                  Evasion Rating: 608 (augmented)
                  --------
                  Requirements:
                  Level: 70
                  Str: 108
                  Dex: 111
                  Int: 155
                  --------
                  Sockets: B-B-B-R-G G 
                  --------
                  Item Level: 74
                  --------
                  Adds 1-30 Lightning Damage to Attacks
                  107% increased Armour and Evasion
                  +63 to maximum Life
                  -60% to Lightning Resistance
                  30% of Physical Damage taken as Lightning Damage
                  --------
                  There's nothing like imminent death
                  to galvanize one's purpose in life.
                  - Malachai the Soulless.
                  --------
                  Has Infernal Body Armour",
                Mock.Of<IPoeItem>(x => x.Rarity == PoeItemRarity.Unique && 
                                       x.ItemName == "Lightning Coil" &&
                                       x.Quality == "20% (augmented)" &&
                                       x.Links == Mock.Of<IPoeLinksInfo>(y => y.RawSockets == "B-B-B-R-G G")));

            yield return new TestCaseData(
               @"Rarity: Rare
                Torment Veil
                Spidersilk Robe
                --------
                Energy Shield: 127 (augmented)
                --------
                Requirements:
                Level: 49
                Int: 134
                --------
                Sockets: R-B-B-R-B-G 
                --------
                Item Level: 71
                --------
                +58 to maximum Life
                +46 to maximum Mana
                +24 to maximum Energy Shield
                +36% to Fire Resistance
                +9% to Cold Resistance
                +22% to Chaos Resistance
                --------
                Corrupted",
               Mock.Of<IPoeItem>(x => x.Rarity == PoeItemRarity.Rare && 
                                      x.ItemName == "Torment Veil" &&
                                      x.Links == Mock.Of<IPoeLinksInfo>(y => y.RawSockets == "R-B-B-R-B-G") &&
                                      x.IsCorrupted == true));

            yield return new TestCaseData(
                @"Rarity: Normal
                Sacrifice at Dawn
                --------
                Item Level: 69
                --------
                Only those who aspire can dare to hope.
                --------
                Can be used in the Eternal Laboratory or a personal Map Device.",
               Mock.Of<IPoeItem>(x => x.Rarity == PoeItemRarity.Normal && x.ItemName == "Sacrifice at Dawn"));

            yield return new TestCaseData(
                @"Rarity: Normal
                Emperor's Luck
                --------
                Stack Size: 2/5
                --------
                5x Currency
                --------
                The house always wins.
                --------

                Shift click to unstack.",
               Mock.Of<IPoeItem>(x => x.Rarity == PoeItemRarity.Normal && x.ItemName == "Emperor's Luck"));

            yield return new TestCaseData(
                  @"Rarity: Magic
                Perpetual Ruby Flask of Heat
                --------
                Quality: +20% (augmented)
                Lasts 4.20 (augmented) Seconds
                Consumes 30 of 60 Charges on use
                Currently has 0 Charges
                +10% to maximum Fire Resistance (augmented)
                +50% to Fire Resistance (augmented)
                --------
                Requirements:
                Level: 18
                --------
                Item Level: 70
                --------
                27% increased Charge Recovery
                Immunity to Freeze and Chill during flask effect
                Removes Freeze and Chill on use
                --------
                Right click to drink. Can only hold charges while in belt. Refills as you kill monsters.",
               Mock.Of<IPoeItem>(x => x.Rarity == PoeItemRarity.Magic && 
                                      x.ItemName == "Perpetual Ruby Flask of Heat" && 
                                      x.Quality == "20% (augmented)"));

            yield return new TestCaseData(
                  @"Rarity: Currency
                  Jeweller's Orb
                  --------
                  Stack Size: 5/20
                  --------
                  Reforges the number of sockets on an item
                  --------
                  Right click this item then left click a socketed item to apply it. The item's quality value is consumed to increase the chances of obtaining more sockets.
                  Shift click to unstack.",
               Mock.Of<IPoeItem>(x => x.ItemName == "Jeweller's Orb"));

            yield return new TestCaseData(
                @"Rarity: Rare
                  Two-Stone Ring
                  --------
                  Item Level: 74
                  --------
                  +14% to Fire and Cold Resistances
                  --------
                  Unidentified",
               Mock.Of<IPoeItem>(x => x.Rarity == PoeItemRarity.Rare && x.ItemName == "Two-Stone Ring"));
        }

        private PoeItemParser CreateInstance()
        {
            return new PoeItemParser(Mock.Of<IPoeQueryInfoProvider>());
        }
    }
}