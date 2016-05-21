namespace PoeEye.Tests.PoeTrade
{
    using System.Linq;

    using Moq;

    using NUnit.Framework;

    using PoeEye.PoeTrade;

    using Shouldly;

    using TestData;

    [TestFixture]
    public class PoeTradeParserModernFixture
    {
        [SetUp]
        public void SetUp()
        {
        }

        private PoeTradeParserModern CreateInstance()
        {
            return new PoeTradeParserModern(Mock.Of<IPoeTradeDateTimeExtractor>());
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
    }
}