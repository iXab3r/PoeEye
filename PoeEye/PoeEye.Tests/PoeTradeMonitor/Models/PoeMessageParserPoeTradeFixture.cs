using NUnit.Framework;
using PoeEye.TradeMonitor.Models;
using PoeShared.Converters;
using PoeWhisperMonitor.Chat;
using Shouldly;

namespace PoeEye.Tests.PoeTradeMonitor.Models
{
    [TestFixture]
    public class GearTypeAnalyzerFixture
    {
        [Test]
        [TestCase(
            "Hi, I would like to buy your Enduring Onslaught Leaguestone of Slaying listed for 3 alchemy in Legacy (stash tab \"Трейд\"; position: left 12, top 2)", 
            true,
            "Enduring Onslaught Leaguestone of Slaying",
            "alchemy",
            3)]
        [TestCase(
            "Hi, I would like to buy your Rigwald's Savagery Royal Axe listed for 2 exalted in Legacy (stash tab \"sale\"; position: left 5, top 4)",
            true,
            "Rigwald's Savagery Royal Axe",
            "exalted",
            2)]
        public void ShouldParse(
            string messageText,
            bool expected,
            string expectedItemName,
            string expectedCurrency,
            float expectedPrice)
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
            isSuccess.ShouldBe(expected);
            result.CharacterName.ShouldBe("Name");
            result.ItemName.ShouldBe(expectedItemName);
            result.Price.CurrencyType.ShouldBe(expectedCurrency);
            result.Price.Value.ShouldBe(expectedPrice);
        }

        private PoeMessageParserPoeTrade CreateInstance()
        {
            return new PoeMessageParserPoeTrade(new PriceToCurrencyConverter());   
        }
    }
}