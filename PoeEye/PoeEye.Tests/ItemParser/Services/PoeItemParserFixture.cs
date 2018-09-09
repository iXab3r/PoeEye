using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using PoeEye.ItemParser.Services;
using PoeShared.Common;
using PoeShared.PoeTrade.Query;
using Shouldly;

namespace PoeEye.Tests.ItemParser.Services
{
    [TestFixture]
    public class PoeItemParserFixture
    {
        private readonly Mock<IPoeStaticData> queryInfoProvider = new Mock<IPoeStaticData>();

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
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Rare &&
                         x.ItemName == "Gloom Nails" &&
                         x.Quality == "1%" &&
                         x.Requirements == "Level: 70 Str: 155 Dex: 38 Int: 111" &&
                         x.Links == new PoeLinksInfo("B-R-B-R") &&
                         x.Mods == new[]
                         {
                             new PoeItemMod
                             {
                                 Name = @"Adds 1-5 Lightning Damage to Attacks",
                                 CodeName = @"Adds #-# Lightning Damage to Attacks",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"+64 to maximum Life",
                                 CodeName = @"+# to maximum Life",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"+39% to Fire Resistance",
                                 CodeName = @"+#% to Fire Resistance",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"+42% to Cold Resistance",
                                 CodeName = @"+#% to Cold Resistance",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"+25% to Chaos Resistance",
                                 CodeName = @"+#% to Chaos Resistance",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"63% increased Armour and Evasion",
                                 CodeName = @"#% increased Armour and Evasion",
                                 ModType = PoeModType.Explicit
                             }
                         }));

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
                Mock.Of<IPoeItem>(
                    x => x.ItemName == "Vengeance" &&
                         x.Requirements == "Level: 24 Str: 58" &&
                         x.Quality == "14% (augmented)" &&
                         x.Mods == new[]
                         {
                             new PoeItemMod
                             {
                                 Name = @"Deals 75% of Base Attack Damage",
                                 CodeName = @"Deals #% of Base Attack Damage",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"37% chance to Counterattack with this Skill when Hit",
                                 CodeName = @"#% chance to Counterattack with this Skill when Hit",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"You cannot use this Attack directly",
                                 CodeName = @"You cannot use this Attack directly",
                                 ModType = PoeModType.Explicit
                             }
                         }));

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
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Unique &&
                         x.ItemName == "Lightning Coil" &&
                         x.Quality == "20% (augmented)" &&
                         x.Requirements == "Level: 70 Str: 108 Dex: 111 Int: 155" &&
                         x.Links == new PoeLinksInfo("B-B-B-R-G G") &&
                         x.Mods == new[]
                         {
                             new PoeItemMod
                             {
                                 Name = @"Adds 1-30 Lightning Damage to Attacks",
                                 CodeName = @"Adds #-# Lightning Damage to Attacks",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"107% increased Armour and Evasion",
                                 CodeName = @"#% increased Armour and Evasion",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"+63 to maximum Life",
                                 CodeName = @"+# to maximum Life",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"-60% to Lightning Resistance",
                                 CodeName = @"-#% to Lightning Resistance",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"30% of Physical Damage taken as Lightning Damage",
                                 CodeName = @"#% of Physical Damage taken as Lightning Damage",
                                 ModType = PoeModType.Explicit
                             }
                         }));

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
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Rare &&
                         x.ItemName == "Torment Veil" &&
                         x.Requirements == "Level: 49 Int: 134" &&
                         x.Links == new PoeLinksInfo("R-B-B-R-B-G") &&
                         x.Modifications == PoeItemModificatins.Corrupted));

            yield return new TestCaseData(
                @"Rarity: Normal
                Sacrifice at Dawn
                --------
                Item Level: 69
                --------
                Only those who aspire can dare to hope.
                --------
                Can be used in the Eternal Laboratory or a personal Map Device.",
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Normal &&
                         x.ItemName == "Sacrifice at Dawn"));

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
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Normal &&
                         x.ItemName == "Emperor's Luck"));

            yield return new TestCaseData(
                @"Rarity: Rare
                Corruption Clasp
                Agate Amulet
                --------
                Requirements:
                Level: 28
                --------
                Item Level: 44
                --------
                +19 to Strength and Intelligence
                --------
                17% increased Fire Damage
                10% increased Global Critical Strike Chance
                -6 to Mana Cost of Skills",
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Rare &&
                         x.ItemName == "Corruption Clasp" &&
                         x.Requirements == "Level: 28" &&
                         x.ItemLevel == "44" &&
                         x.Mods == new[]
                         {
                             new PoeItemMod
                             {
                                 Name = @"+19 to Strength and Intelligence",
                                 CodeName = @"+# to Strength and Intelligence",
                                 ModType = PoeModType.Implicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"17% increased Fire Damage",
                                 CodeName = @"#% increased Fire Damage",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"10% increased Global Critical Strike Chance",
                                 CodeName = @"#% increased Global Critical Strike Chance",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"-6 to Mana Cost of Skills",
                                 CodeName = @"-# to Mana Cost of Skills",
                                 ModType = PoeModType.Explicit
                             }
                         }));

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
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Magic &&
                         x.Requirements == "Level: 18" &&
                         x.ItemName == "Perpetual Ruby Flask of Heat" &&
                         x.Quality == "20% (augmented)" &&
                         x.Mods == new[]
                         {
                             new PoeItemMod
                             {
                                 Name = @"27% increased Charge Recovery",
                                 CodeName = @"#% increased Charge Recovery",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"Immunity to Freeze and Chill during flask effect",
                                 CodeName = @"Immunity to Freeze and Chill during flask effect",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"Removes Freeze and Chill on use",
                                 CodeName = @"Removes Freeze and Chill on use",
                                 ModType = PoeModType.Explicit
                             }
                         }));

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
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Rare &&
                         x.ItemName == "Two-Stone Ring" &&
                         x.ItemLevel == "74" &&
                         x.Mods == new[]
                         {
                             new PoeItemMod
                             {
                                 Name = @"+14% to Fire and Cold Resistances",
                                 CodeName = @"+#% to Fire and Cold Resistances",
                                 ModType = PoeModType.Implicit
                             }
                         }));

            yield return new TestCaseData(
                @"Rarity: Unique
                Lioneye's Remorse
                Pinnacle Tower Shield
                --------
                Quality: +20% (augmented)
                Chance to Block: 30% (augmented)
                Armour: 1502 (augmented)
                --------
                Requirements:
                Level: 70
                Str: 159
                Int: 68
                --------
                Sockets: R-R-R 
                --------
                Item Level: 70
                --------
                250% increased Armour
                +83 to maximum Life
                5% reduced Movement Speed
                20% increased Stun Recovery
                -25 Physical Damage taken from Projectile Attacks
                +5% Chance to Block
                --------
                Marceus' unblemished shield is a testament
                to his arrogance... and his fate.
                ",
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Unique &&
                         x.ItemName == "Lioneye's Remorse" &&
                         x.ItemLevel == "70" &&
                         x.Requirements == "Level: 70 Str: 159 Int: 68" &&
                         x.Links == new PoeLinksInfo("R-R-R") &&
                         x.Mods == new[]
                         {
                             new PoeItemMod
                             {
                                 Name = @"250% increased Armour",
                                 CodeName = @"#% increased Armour",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"+83 to maximum Life",
                                 CodeName = @"+# to maximum Life",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"5% reduced Movement Speed",
                                 CodeName = @"#% reduced Movement Speed",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"20% increased Stun Recovery",
                                 CodeName = @"#% increased Stun Recovery",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"-25 Physical Damage taken from Projectile Attacks",
                                 CodeName = @"-# Physical Damage taken from Projectile Attacks",
                                 ModType = PoeModType.Explicit
                             },
                             new PoeItemMod
                             {
                                 Name = @"+5% Chance to Block",
                                 CodeName = @"+#% Chance to Block",
                                 ModType = PoeModType.Explicit
                             }
                         }));
        }

        private PoeItemParser CreateInstance()
        {
            return new PoeItemParser(new PoeModsProcessor(queryInfoProvider.Object));
        }

        [Test]
        [TestCaseSource(nameof(KnownItems))]
        public void ShouldParseExplicitMods(string data, IPoeItem expectedItem)
        {
            //Given
            var modsList = expectedItem.Mods.Where(x => x.ModType == PoeModType.Explicit).ToArray();
            queryInfoProvider.SetupGet(x => x.ModsList).Returns(modsList);

            var instance = CreateInstance();

            //When
            var result = instance.Parse(data);

            //Then
            var exprectedImplicitMods = expectedItem.Mods.Where(x => x.ModType == PoeModType.Explicit).ToArray();
            var resultMods = result.Mods.Where(x => x.ModType == PoeModType.Explicit).ToArray();

            CollectionAssert.AreEqual(exprectedImplicitMods, resultMods, new PoeItemModEqualityComparer());
        }

        [Test]
        [TestCaseSource(nameof(KnownItems))]
        public void ShouldParseImplicitMods(string data, IPoeItem expectedItem)
        {
            //Given
            var modsList = expectedItem.Mods.Where(x => x.ModType == PoeModType.Implicit).ToArray();
            queryInfoProvider.SetupGet(x => x.ModsList).Returns(modsList);

            var instance = CreateInstance();

            //When
            var result = instance.Parse(data);

            //Then
            var exprectedImplicitMods = expectedItem.Mods.Where(x => x.ModType == PoeModType.Implicit).ToArray();
            var resultMods = result.Mods.Where(x => x.ModType == PoeModType.Implicit).ToArray();

            CollectionAssert.AreEqual(exprectedImplicitMods, resultMods, new PoeItemModEqualityComparer());
        }

        [Test]
        [TestCaseSource(nameof(KnownItems))]
        public void ShouldParseIsCorrupted(string data, IPoeItem expectedItem)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Parse(data);

            //Then
            result.Modifications.HasFlag(PoeItemModificatins.Corrupted).ShouldBe(expectedItem.Modifications.HasFlag(PoeItemModificatins.Corrupted));
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

        [Test]
        [TestCaseSource(nameof(KnownItems))]
        public void ShouldParseLinks(string data, IPoeItem expectedItem)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Parse(data);

            //Then
            result.Links.ShouldBe(expectedItem.Links);
        }

        [Test]
        [TestCaseSource(nameof(KnownItems))]
        public void ShouldParseRarity(string data, IPoeItem expectedItem)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Parse(data);

            //Then
            result.Rarity.ShouldBe(expectedItem.Rarity);
        }

        [Test]
        [TestCaseSource(nameof(KnownItems))]
        public void ShouldParseRequirements(string data, IPoeItem expectedItem)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Parse(data);

            //Then
            result.Requirements.ShouldBe(expectedItem.Requirements);
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
            result.ShouldBe(null);
        }
    }
}