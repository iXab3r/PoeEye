using System.Collections.Generic;
using NUnit.Framework;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.Models.Parsers;
using PoeShared.Common;
using PoeShared.Converters;
using PoeWhisperMonitor.Chat;
using Shouldly;

namespace PoeEye.Tests.PoeTradeMonitor.Models
{
    [TestFixture]
    internal class PoeMessageStrictParserPoeTradeFixture
    {
        [Test]
        [TestCaseSource(nameof(ShouldParseCases))]
        public void ShouldParse(
            string messageText,
            bool expectedSuccess,
            TradeModel expected)
        {
            //Given
            var instance = CreateInstance();

            var message = new PoeMessage()
            {
                Message = messageText,
                Name = "Name",
            };

            //When
            TradeModel result;
            var isSuccess = instance.TryParse(message, out result);

            //Then
            isSuccess.ShouldBe(expectedSuccess);
            if (expectedSuccess)
            {
                result.PositionName.ShouldBe(expected.PositionName);
                result.Price.ShouldBe(expected.Price);
                result.CharacterName.ShouldBe(expected.CharacterName);
                result.TabName.ShouldBe(expected.TabName);
                result.League.ShouldBe(expected.League);
                result.ItemPosition.ShouldBe(expected.ItemPosition);
            }
        }

        private IEnumerable<TestCaseData> ShouldParseCases()
        {
            yield return new TestCaseData(
                    "Hi, I would like to buy your Enduring Onslaught Leaguestone of Slaying listed for 3 alchemy in Legacy (stash tab \"Трейд\"; position: left 12, top 2)",
                    true,
                    new TradeModel()
                    {
                        CharacterName = "Name",
                        ItemPosition = new ItemPosition(12, 2),
                        Price = new PoePrice("alchemy", 3),
                        PositionName = "Enduring Onslaught Leaguestone of Slaying",
                        League = "Legacy",
                        TabName = "Трейд"
                    }
                );
            yield return new TestCaseData(
                    "Hi, I would like to buy your Enduring Onslaught Leaguestone of Slaying listed for 3 alchemy in Legacy (stash tab \"Трейд\"; position: left 12, top 2), offer 3c",
                    true,
                    new TradeModel()
                    {
                        CharacterName = "Name",
                        ItemPosition = new ItemPosition(12, 2),
                        Price = new PoePrice("alchemy", 3),
                        PositionName = "Enduring Onslaught Leaguestone of Slaying",
                        League = "Legacy",
                        TabName = "Трейд",
                        Offer = "offer 3c"
                    });
        }

        private PoeMessageStrictParserPoeTrade CreateInstance()
        {
            return new PoeMessageStrictParserPoeTrade(new PriceToCurrencyConverter());   
        }
    }
}