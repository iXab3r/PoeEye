using System.Collections.Generic;
using NUnit.Framework;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.Services.Parsers;
using PoeShared.Common;
using PoeShared.Converters;
using PoeWhisperMonitor.Chat;
using Shouldly;

namespace PoeEye.Tests.PoeTradeMonitor.Services.Parsers
{
    [TestFixture]
    internal class PoeMessageCurrencyParserPoeTradeFixture
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
                    "Hi, I'd like to buy your 3 exalted for my 169.5 chaos in Legacy.",
                    true,
                    new TradeModel()
                    {
                        CharacterName = "Name",
                        Price = new PoePrice("chaos", 169.5f),
                        PositionName = "3 exalted",
                        League = "Legacy",
                    }
                );
        }

        private PoeMessageCurrencyParserPoeTrade CreateInstance()
        {
            return new PoeMessageCurrencyParserPoeTrade(new StringToPoePriceConverter());   
        }
    }
}