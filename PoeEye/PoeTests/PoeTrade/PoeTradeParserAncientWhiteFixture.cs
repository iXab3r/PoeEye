namespace PoeEye.Tests.PoeTrade
{
    using System.Linq;

    using NUnit.Framework;

    using PoeEye.PoeTrade;

    using Shouldly;

    using TestData;

    [TestFixture]
    public class PoeTradeParserAncientWhiteFixture
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void ShouldParseAncientWhiteTheme()
        {
            //Given
            var instance = CreateInstance();
            var rawResult = TestDataProvider.AncientWhiteResult;

            //When
            var items = instance.ParseQueryResult(rawResult);

            //Then
            items.Count().ShouldBe(300);
        }

        private PoeTradeParserAncientWhite CreateInstance()
        {
            return new PoeTradeParserAncientWhite();
        }
    }
}