﻿using System.Linq;
using CsQuery;
using Moq;
using NUnit.Framework;
using PoeEye.PoeTrade;
using PoeEye.Tests.PoeEye.PoeTrade.TestData;
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
        public void ShouldParseCurrenciesList()
        {
            //Given
            var rawHtml = TestDataProvider.ModernResult;
            var instance = CreateInstance();

            //When
            var result = instance.ParseStaticData(rawHtml);

            //Then
            result.CurrenciesList.Length.ShouldBe(16);
        }

        [Test]
        public void ShouldParseItems()
        {
            //Given
            var rawHtml = TestDataProvider.ModernResult;
            var instance = CreateInstance();

            //When
            var result = instance.ParseQueryResponse(rawHtml);

            //Then
            result.ItemsList.Count().ShouldBe(99);
        }

        [Test]
        public void ShouldParseModsList()
        {
            //Given
            var rawHtml = TestDataProvider.ModernResult;
            var instance = CreateInstance();

            //When
            var result = instance.ParseStaticData(rawHtml);

            //Then
            result.ModsList.Length.ShouldBe(619);
        }


        [Test]
        public void ShouldParseLiveResultNewItem()
        {
            //Given
            var rawHtml = TestDataProvider.ModernLiveResultNewItem;
            var instance = CreateInstance();

            //When
            var result = instance.ParseQueryResponse(rawHtml);

            //Then
            var item = result.ItemsList.Single();
            item.ItemState.ShouldBe(PoeTradeState.New);
            item.Hash.ShouldBe("dc01dfc3465383e3082a0ffc5f4f7268");
        }

        [Test]
        public void ShouldParseModWithTier()
        {
            //Given
            var text =
                @"<li class='sortable' style='' data-name='#+# to Intelligence' data-value='37.0'>+<b>37</b> to Intelligence <span class='item-affix item-affix-S'> <span class='affix-info-short'>S4</span> <span class='affix-info-full'>Tier 4 suffix: of the Sage, min=[33] max=[37]</span> </span> </li>";
            
            var instance = CreateInstance();

            //When
            var mod = instance.ExtractItemMod(new CQ(text), PoeModType.Explicit);

            //Then
            mod.Name.ShouldBe("+37 to Intelligence");
            mod.TierInfo.ShouldBe("S4 Tier 4 suffix: of the Sage, min=[33] max=[37]");
        }

        [Test]
        public void ShouldParseLiveResultItemGone()
        {
            //Given
            var rawHtml = TestDataProvider.ModernLiveResultItemGone;
            var instance = CreateInstance();

            //When
            var result = instance.ParseQueryResponse(rawHtml);

            //Then
            var item = result.ItemsList.Single();
            item.ItemState.ShouldBe(PoeTradeState.Removed);
            item.Hash.ShouldBe("dc01dfc3465383e3082a0ffc5f4f7268");
        }
    }
}