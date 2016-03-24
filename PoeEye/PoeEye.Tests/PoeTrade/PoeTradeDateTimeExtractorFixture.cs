namespace PoeEye.Tests.PoeTrade
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using Moq;

    using NUnit.Framework;

    using PoeEye.PoeTrade;

    using PoeShared;

    using Shouldly;

    [TestFixture]
    public class PoeTradeDateTimeExtractorFixture
    {
        private Mock<IClock> clock;

        [SetUp]
        public void SetUp()
        {
            clock = new Mock<IClock>();
            clock.SetupGet(x => x.Now).Returns(new DateTime(1000, 12, 12, 12, 12, 12));
        }

        [Test]
        [TestCaseSource(nameof(ShouldConvertCases))]
        public void ShouldConvert(string timestamp, DateTime expectedResult)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.ExtractTimestamp(timestamp);

            //Then
            result.ShouldBe(expectedResult);
        }

        private IEnumerable<TestCaseData> ShouldConvertCases()
        {
            yield return new TestCaseData("yesterday", new DateTime(year: 1000, month: 12, day: 11, hour: 12, minute: 12, second: 12));
            yield return new TestCaseData("1 day ago", new DateTime(year: 1000, month: 12, day: 11, hour: 12, minute: 12, second: 12));
            yield return new TestCaseData("2 days ago", new DateTime(year: 1000, month: 12, day: 10, hour: 12, minute: 12, second: 12));
            yield return new TestCaseData("1 hour ago", new DateTime(year: 1000, month: 12, day: 12, hour: 11, minute: 12, second: 12));
            yield return new TestCaseData("2 hours ago", new DateTime(year: 1000, month: 12, day: 12, hour: 10, minute: 12, second: 12));
            yield return new TestCaseData("1 minute ago", new DateTime(year: 1000, month: 12, day: 12, hour: 12, minute: 11, second: 12));
            yield return new TestCaseData("2 minutes ago", new DateTime(year: 1000, month: 12, day: 12, hour: 12, minute: 10, second: 12));
            yield return new TestCaseData("1 second ago", new DateTime(year: 1000, month: 12, day: 12, hour: 12, minute: 12, second: 11));
            yield return new TestCaseData("2 seconds ago", new DateTime(year: 1000, month: 12, day: 12, hour: 12, minute: 12, second: 10));
        }

        private PoeTradeDateTimeExtractor CreateInstance()
        {
            return new PoeTradeDateTimeExtractor(clock.Object);
        }
    }
}