using NUnit.Framework;
using PoeBud.OfficialApi.DataTypes;
using PoeBud.OfficialApi.ProcurementLegacy;
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

        private GearTypeAnalyzer CreateInstance()
        {
            return new GearTypeAnalyzer();   
        }
    }
}