using CsQuery;
using Moq;
using NUnit.Framework;
using PoeEye.PoeTrade;
using PoeShared.Common;
using PoeShared.StashApi.ProcurementLegacy;
using Shouldly;

namespace PoeEye.Tests.PoeEye.PoeTrade
{
    [TestFixture]
    public class PoeTradeParserModernFixture
    {
        [SetUp]
        public void SetUp()
        {
        }

        private PoeTradeParserModern CreateInstance()
        {
            return new PoeTradeParserModern(Mock.Of<IPoeTradeDateTimeExtractor>(), Mock.Of<IItemTypeAnalyzer>());
        }

        [Test]
        public void ShouldParseModWithTier()
        {
            //Given
            var text =
                @"<li class='sortable' style='' data-name='#+# to Intelligence' data-value='37.0'>+<b>37</b> to Intelligence <span class='item-affix item-affix-S'> <span class='affix-info-short'>S4</span> <span class='affix-info-full'>Tier 4 suffix: of the Sage, min=[33] max=[37]</span> </span> </li>";
            
            var instance = CreateInstance();

            //When
            var mod = instance.ExtractItemMod(new CQ(text).FirstElement(), PoeModType.Explicit);

            //Then
            mod.Name.ShouldBe("+37 to Intelligence");
            mod.TierInfo.ShouldBe("S4 Tier 4 suffix: of the Sage, min=[33] max=[37]");
        }
    }
}