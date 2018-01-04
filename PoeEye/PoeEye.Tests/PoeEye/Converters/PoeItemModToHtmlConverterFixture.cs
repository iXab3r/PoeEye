using System;
using System.Collections.Generic;
using System.Windows.Media;
using log4net.Appender;
using PoeEye.Converters;
using PoeShared.Common;
using PoeShared.Scaffolding;

namespace PoeEye.Tests.PoeEye.Converters
{
    using System.Linq;

    using Moq;

    using NUnit.Framework;

    using Shouldly;

    [TestFixture]
    public class PoeItemModToHtmlConverterFixture
    {
        private static readonly PoeItemModToHtmlConverter DataSrc;

        static PoeItemModToHtmlConverterFixture()
        {
            DataSrc = new PoeItemModToHtmlConverter();
            var color = Color.FromRgb(100, 100, 100);
            foreach (var propertyInfo in DataSrc.GetType().GetProperties().Where(x => x.PropertyType == typeof(Color)))
            {
                propertyInfo.SetValue(DataSrc, color);

                color = Color.FromRgb((byte)(color.R + 1), (byte)(color.G + 1), (byte)(color.B + 1));
            }
        }
        
        [Test]
        public void ShouldCreate()
        {
            //Then
            CreateInstance();
        }

        [Test]
        [TestCaseSource(nameof(ShouldConvertCases))]
        public void ShouldConvert(IPoeItemMod mod, string expected)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Convert(mod);

            //Then
            result.ShouldBe(expected);
        }

        public IEnumerable<TestCaseData> ShouldConvertCases()
        {
            yield return new TestCaseData(
                ToMod("+53 to maximum Mana"),
                Wrap("+53 to maximum Mana", DataSrc.ManaRelatedTextColor, DataSrc.DefaultTextColor));
            yield return new TestCaseData(
                ToMod("40% increased Righteous Fire Damage"),
                Wrap("40% increased Righteous Fire Damage", DataSrc.DefaultTextColor));
            yield return new TestCaseData(
                ToMod("+53 to maximum Life"),
                Wrap("+53 to maximum Life", DataSrc.LifeRelatedTextColor, DataSrc.DefaultTextColor));
            yield return new TestCaseData(
                ToMod("+60% to Fire Resistance"),
                Wrap("+60% to Fire Resistance", DataSrc.FireRelatedTextColor, DataSrc.DefaultTextColor));
            yield return new TestCaseData(
                ToMod("Adds 61 Fire Damage"),
                Wrap("Adds 61 Fire Damage", DataSrc.FireRelatedTextColor, DataSrc.DefaultTextColor));
            yield return new TestCaseData(
                ToMod("+60% to Cold Resistance"),
                Wrap("+60% to Cold Resistance", DataSrc.ColdRelatedTextColor, DataSrc.DefaultTextColor));
            yield return new TestCaseData(
                ToMod("+60% to Lightning Resistance"),
                Wrap("+60% to Lightning Resistance", DataSrc.LightningRelatedTextColor, DataSrc.DefaultTextColor));
            yield return new TestCaseData(
                ToMod("+60% to Chaos Resistance"),
                Wrap("+60% to Chaos Resistance", DataSrc.ChaosRelatedTextColor, DataSrc.DefaultTextColor));
            yield return new TestCaseData(
                ToMod("Adds 1 Physical Damage to Attacks"),
                Wrap("Adds 1 Physical Damage to Attacks", DataSrc.PhysicalRelatedTextColor, DataSrc.DefaultTextColor)); 
            yield return new TestCaseData(
                ToMod("Adds 1 to 2 Fire Damage"),
                Wrap("Adds 1 to 2 (~1.5) Fire Damage", DataSrc.FireRelatedTextColor, DataSrc.DefaultTextColor));
            yield return new TestCaseData(
                ToMod("total: +13% to Fire Resistance"),
                Wrap(
                    Wrap(AddGroup("total", DataSrc.DefaultTextColor, DataSrc.TotalGroupColor) + "+13% to Fire Resistance", DataSrc.FireRelatedTextColor), 
                    DataSrc.DefaultTextColor
                    ));
        }

        private IPoeItemMod ToMod(string name)
        {
            return Mock.Of<IPoeItemMod>(x => x.Name == name && x.CodeName == name);
        }
        
        private string AddGroup(string input, Color color, Color bgColor)
        {
            return PoeItemModToHtmlConverter.AddGroup(input, color, bgColor);
        }

        private string Wrap(string input, params Color[] colors)
        {
            return colors.Aggregate(input, PoeItemModToHtmlConverter.WrapInSpan);
        }

        private PoeItemModToHtmlConverter CreateInstance()
        {
            var result = new PoeItemModToHtmlConverter();
            DataSrc.TransferPropertiesTo(result);
            return result;
        }
    }
}