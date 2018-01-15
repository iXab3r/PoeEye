using NUnit.Framework;
using PoeShared.StashApi.DataTypes;
using PoeShared.StashApi.ProcurementLegacy;
using Shouldly;

namespace PoeEye.Tests.PoeBud.OfficialApi
{
    [TestFixture]
    public class GearTypeAnalyzerFixture
    {
        [Test]
        [TestCase("Mask", GearType.Helmet)]
        [TestCase("Saint's Hauberk", GearType.Chest)]
        [TestCase("Sai", GearType.Dagger)]
        public void ShouldResolveGearTypeByName(string itemName, GearType expected)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Resolve(itemName);

            //Then
            result.ShouldBe(expected);
        }
        
        [Test]
        [TestCase("Wraith Sword", GearType.Sword, "",  "Wraith Sword")]
        [TestCase("Sai", GearType.Dagger, "","Sai")]
        
        [TestCase("Superior Dragonscale Doublet", GearType.Chest, "", "Dragonscale Doublet")]
        [TestCase("Soul Sanctuary Ringmail Coat", GearType.Chest, "Soul Sanctuary", "Ringmail Coat")]
        [TestCase("Death Coil Diamond Ring", GearType.Ring, "Death Coil", "Diamond Ring")]
        [TestCase("Perpetual Ruby Flask of Heat", GearType.Flask, "Perpetual Ruby Flask of Heat", "Ruby Flask")]
        public void ShouldResolveItemTypeByName(string itemName, GearType expectedGearType, string expectedName, string expectedItemType)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.ResolveTypeInfo(itemName);

            //Then
            result.ItemType.ShouldBe(expectedItemType);
            result.GearType.ShouldBe(expectedGearType);
            result.ItemName.ShouldBe(expectedName);
        }

        private GearTypeAnalyzer CreateInstance()
        {
            return new GearTypeAnalyzer();   
        }
    }
}