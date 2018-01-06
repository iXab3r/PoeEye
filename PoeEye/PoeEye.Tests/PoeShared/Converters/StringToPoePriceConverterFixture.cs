using System;
using System.Collections.Generic;
using PoeShared.Common;
using PoeShared.Converters;

namespace PoeEye.Tests.PoeShared.Converters
{
    using System.Linq;

    using Moq;

    using NUnit.Framework;

    using Shouldly;

    [TestFixture]
    public class StringToPoePriceConverterFixture
    {
        [Test]
        public void ShouldCreate()
        {
            //Then
            CreateInstance();
        }

        [Test]
        [TestCaseSource(nameof(ShouldConvertCases))]
        public void ShouldConvert(string rawPrice, PoePrice expected)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Convert(rawPrice);

            //Then
            result.CurrencyType.ShouldBe(expected.CurrencyType);
            result.Value.ShouldBe(expected.Value);
            result.Price.ShouldBe(expected.Price);
            result.HasValue.ShouldBe(expected.HasValue);
            result.IsEmpty.ShouldBe(expected.IsEmpty);
        }

        public IEnumerable<TestCaseData> ShouldConvertCases()
        {
            yield return new TestCaseData("Unknown", PoePrice.Empty);
            yield return new TestCaseData("Random", PoePrice.Empty);
            yield return new TestCaseData("blessed", new PoePrice(KnownCurrencyNameList.BlessedOrb, 0));
            yield return new TestCaseData("0 alt", new PoePrice(KnownCurrencyNameList.OrbOfAlteration, 0));
            yield return new TestCaseData("alt", new PoePrice(KnownCurrencyNameList.OrbOfAlteration, 0));
            yield return new TestCaseData("1 alt", new PoePrice(KnownCurrencyNameList.OrbOfAlteration, 1));
            yield return new TestCaseData("1 chaos orb", new PoePrice(KnownCurrencyNameList.ChaosOrb, 1));
            yield return new TestCaseData("9 Jeweller's Orb", new PoePrice(KnownCurrencyNameList.JewellersOrb, 9));
            yield return new TestCaseData("price 1 alt", new PoePrice(KnownCurrencyNameList.OrbOfAlteration, 1));
            yield return new TestCaseData("b/o 1 alt", new PoePrice(KnownCurrencyNameList.OrbOfAlteration, 1));
            yield return new TestCaseData("1.5 alt", new PoePrice(KnownCurrencyNameList.OrbOfAlteration, 1.5f));
            yield return new TestCaseData("5 blessing of chayula", new PoePrice(KnownCurrencyNameList.BlessingOfChayula, 5));
            yield return new TestCaseData("1 xoph's breachstone", new PoePrice(KnownCurrencyNameList.XophsBreachstone, 1));
            yield return new TestCaseData("1 remnant of corruption", new PoePrice(KnownCurrencyNameList.RemnantOfCorruption, 1));

            var rng = new Random(1);
            foreach (var currency in KnownCurrencyNameList.EnumerateKnownCurrencies())
            {
                var value = rng.Next(1, 10);
                yield return new TestCaseData($"{value} {currency}", new PoePrice(currency, value));
            }
        }

        private StringToPoePriceConverter CreateInstance()
        {
            return new StringToPoePriceConverter();
        }
    }
}