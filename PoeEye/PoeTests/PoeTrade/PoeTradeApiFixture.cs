namespace PoeEye.Tests.PoeTrade
{
    using NUnit.Framework;
    using Moq;
    using Shouldly;

    [TestFixture]
    public class PoeTradeApiFixture
    {
        [SetUp]
        public void SetUp()
        {
        }

        private PoeTradeApi CreateInstance()
        {
            return new PoeTradeApi();
        }
    }
}