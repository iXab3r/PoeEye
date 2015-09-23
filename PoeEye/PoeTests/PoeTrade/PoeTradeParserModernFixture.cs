namespace PoeEye.Tests.PoeTrade
{
    using System.Linq;

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

        [Test]
        public void ShouldParseModernItems()
        {
            //Given
            var rawHtml = TestDataProvider.ModernResult;
            var instance = CreateInstance();

            //When
            var items = instance.ParseQueryResult(rawHtml);

            //Then
            items.Count().ShouldBe(99);
        }

        private PoeTradeParserModern CreateInstance()
        {
            return new PoeTradeParserModern();
        }
    }
}