namespace PoeEye.Tests.Communications
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    using NUnit.Framework;

    using Moq;

    using PoeEye.Communications;

    using Shouldly;

    [TestFixture]
    public class NameValueCollectionToStringConverterFixture
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        [TestCaseSource(nameof(ShouldConvertTestCases))]
        public void ShouldConvert(NameValueCollection source, string expected)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Convert(source);

            //Then
            result.ShouldBe(expected);    
        }

        private IEnumerable<TestCaseData> ShouldConvertTestCases()
        {
            yield return new TestCaseData(
                new NameValueCollection()
                {
                    { "a", "1" },
                },
                "a=1");
            yield return new TestCaseData(
                new NameValueCollection()
                {
                    { "a", "1" },
                    { "b", "2" },
                },
                "a=1&b=2");
            yield return new TestCaseData(
               new NameValueCollection()
               {
                    { "a", "1" },
                    { "a", "2" },
                    { "a", "3" },
               },
               "a=1&a=2&a=3");
            yield return new TestCaseData(
                new NameValueCollection()
                {
                    { "a", "1" },
                    { "b", "2" },
                    { "b", "3" },
                    { "c", "4" },
                }, 
                "a=1&b=2&c=4&b=3");
            yield return new TestCaseData(
              new NameValueCollection()
              {
                    { "a", "1" },
                    { "b", "2" },
                    { "c", "3" },
                    { "d", "4" },
                    { "a", "11" },
                    { "b", "22" },
                    { "c", "33" },
                    { "d", "44" },
                    { "a", "111" },
                    { "b", "222" },
                    { "c", "333" },
                    { "d", "444" },
              },
              "a=1&b=2&c=3&d=4&a=11&b=22&c=33&d=44&a=111&b=222&c=333&d=444");
        }

        private NameValueCollectionToStringConverter CreateInstance()
        {
            return new NameValueCollectionToStringConverter();
        }
    }
}