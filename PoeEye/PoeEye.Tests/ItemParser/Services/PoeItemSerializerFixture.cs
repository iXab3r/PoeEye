using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.ObjectBuilder2;
using Moq;
using NUnit.Framework;
using PoeEye.ItemParser.Services;
using PoeShared.Common;
using PoeShared.StashApi.ProcurementLegacy;
using Shouldly;

namespace PoeEye.Tests.ItemParser.Services
{
    [TestFixture]
    public class PoeItemSerializerFixture
    {
        [Test]
        [TestCaseSource(nameof(KnownPathOfBuildingItems))]
        public void ShouldSerializeItems(string expectedData, IPoeItem item)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Serialize(item);

            //Then
            result.ShouldBe(TrimLines(expectedData));
        }

        private IEnumerable<TestCaseData> KnownPathOfBuildingItems()
        {
            yield return new TestCaseData(
                @"Rarity: Rare
                  Gloom Nails
                  Wyrmscale Gauntlets
                  --------
                  Adds 1-5 Lightning Damage to Attacks
                  +64 to maximum Life
                  +39% to Fire Resistance
                  +42% to Cold Resistance
                  +25% to Chaos Resistance
                  63% increased Armour and Evasion",
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Rare &&
                         x.TypeInfo == new ItemTypeInfo()
                         {
                             ItemName = "Gloom Nails",
                             ItemType = "Wyrmscale Gauntlets"
                         } &&
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
                @"Rarity: Unique
                  Lightning Coil
                  Desert Brigandine
                  --------
                  Adds 1-30 Lightning Damage to Attacks
                  107% increased Armour and Evasion
                  +63 to maximum Life
                  -60% to Lightning Resistance
                  30% of Physical Damage taken as Lightning Damage",
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Unique &&
                         x.TypeInfo == new ItemTypeInfo()
                         {
                             ItemName = "Lightning Coil",
                             ItemType = "Desert Brigandine"
                         } &&
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
                @"Rarity: Normal
                Sacrifice at Dawn",
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Normal &&
                         x.ItemName == "Sacrifice at Dawn"));

            yield return new TestCaseData(
                @"Rarity: Normal
                Emperor's Luck",
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Normal &&
                         x.ItemName == "Emperor's Luck"));

            yield return new TestCaseData(
                @"Rarity: Rare
                Corruption Clasp
                Agate Amulet
                --------
                +19 to Strength and Intelligence
                --------
                17% increased Fire Damage
                10% increased Global Critical Strike Chance
                -6 to Mana Cost of Skills",
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Rare &&
                         x.TypeInfo == new ItemTypeInfo()
                         {
                             ItemName = "Corruption Clasp",
                             ItemType = "Agate Amulet"
                         } &&
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
                27% increased Charge Recovery
                Immunity to Freeze and Chill during flask effect
                Removes Freeze and Chill on use",
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
                @"Rarity: Rare
                  Two-Stone Ring
                  --------
                  +14% to Fire and Cold Resistances",
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
                250% increased Armour
                +83 to maximum Life
                5% reduced Movement Speed
                20% increased Stun Recovery
                -25 Physical Damage taken from Projectile Attacks
                +5% Chance to Block",
                Mock.Of<IPoeItem>(
                    x => x.Rarity == PoeItemRarity.Unique &&
                         x.TypeInfo == new ItemTypeInfo()
                         {
                             ItemName = "Lioneye's Remorse",
                             ItemType = "Pinnacle Tower Shield"
                         } &&
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
            
            yield return new TestCaseData(
                @"Two-Stone Ring",
                Mock.Of<IPoeItem>(
                    x => x.TypeInfo == new ItemTypeInfo(){ ItemName = "Two-Stone Ring"}));
            yield return new TestCaseData(
                @"Two-Stone Ring",
                Mock.Of<IPoeItem>(
                    x => x.TypeInfo == new ItemTypeInfo(){ ItemType = "Two-Stone Ring"}));
            yield return new TestCaseData(
                @"Death
                  Two-Stone Ring",
                Mock.Of<IPoeItem>(
                    x => x.TypeInfo == new ItemTypeInfo(){ ItemName = "Death", ItemType = "Two-Stone Ring"}));
            yield return new TestCaseData(
                @"Death
                  Two-Stone Ring",
                Mock.Of<IPoeItem>(
                    x => x.ItemName == "Custom item name" && x.TypeInfo == new ItemTypeInfo { ItemName = "Death", ItemType = "Two-Stone Ring"}));
            yield return new TestCaseData(
                @"Custom item name",
                Mock.Of<IPoeItem>(
                    x => x.ItemName == "Custom item name" && x.TypeInfo == new ItemTypeInfo { ItemType = "Two-Stone Ring"}));
            yield return new TestCaseData(
                @"Custom item name",
                Mock.Of<IPoeItem>(
                    x => x.ItemName == "Custom item name" && x.TypeInfo == new ItemTypeInfo { ItemName = "Death"}));
            yield return new TestCaseData(
                @"Two-Stone Ring",
                Mock.Of<IPoeItem>(
                    x => x.ItemName == "Two-Stone Ring"));
            
            yield return new TestCaseData(
                @"Two-Stone Ring",
                Mock.Of<IPoeItem>(
                    x => x.ItemName == "Two-Stone Ring" &&  x.Mods == new[]
                    {
                        new PoeItemMod
                        {
                            Name = @"enchanted something",
                            CodeName = @"enchanted something",
                            ModType = PoeModType.Implicit
                        },
                    }));
        }


        private string TrimLines(string source)
        {
            return source
                .Split(new[] {"\r", "\n"}, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .JoinStrings(Environment.NewLine);
        }
        
        private PoeItemSerializer CreateInstance()
        {
            return new PoeItemSerializer();
        }
    }
}