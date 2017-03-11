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
    internal class PoeMessageWeakParserPoeTradeFixture
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
                    "Hi, I would like to buy your Enduring Onslaught Leaguestone of Slaying listed for 3 alchemy in Legacy",
                    true,
                    new TradeModel()
                    {
                        CharacterName = "Name",
                        Price = new PoePrice("alchemy", 3),
                        PositionName = "Enduring Onslaught Leaguestone of Slaying",
                        League = "Legacy",
                    }
                );
        }

        private PoeMessageWeakParserPoeTrade CreateInstance()
        {
            return new PoeMessageWeakParserPoeTrade(new PriceToCurrencyConverter());   
        }
    }
}