using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using PoeEye.PoeTrade;
using PoeShared;
using Shouldly;

namespace PoeEye.Tests.PoeEye.PoeTrade
{
    [TestFixture]
    public class PoeTradeDateTimeExtractorFixture
    {
        [SetUp]
        public void SetUp()
        {
            clock = new Mock<IClock>();
            clock.SetupGet(x => x.Now).Returns(new DateTime(1000, 12, 12, 12, 12, 12));
        }

        private Mock<IClock> clock;

        private IEnumerable<TestCaseData> ShouldConvertCases()
        {
            yield return new TestCaseData("yesterday", new DateTime(1000, 12, 11, 12, 12, 12));
            yield return new TestCaseData("1 day ago", new DateTime(1000, 12, 11, 12, 12, 12));
            yield return new TestCaseData("2 days ago", new DateTime(1000, 12, 10, 12, 12, 12));
            yield return new TestCaseData("1 hour ago", new DateTime(1000, 12, 12, 11, 12, 12));
            yield return new TestCaseData("2 hours ago", new DateTime(1000, 12, 12, 10, 12, 12));
            yield return new TestCaseData("1 minute ago", new DateTime(1000, 12, 12, 12, 11, 12));
            yield return new TestCaseData("2 minutes ago", new DateTime(1000, 12, 12, 12, 10, 12));
            yield return new TestCaseData("1 second ago", new DateTime(1000, 12, 12, 12, 12, 11));
            yield return new TestCaseData("2 seconds ago", new DateTime(1000, 12, 12, 12, 12, 10));
        }

        private PoeTradeDateTimeExtractor CreateInstance()
        {
            return new PoeTradeDateTimeExtractor(clock.Object);
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
    }
}