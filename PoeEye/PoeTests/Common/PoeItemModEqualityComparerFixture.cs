namespace PoeEye.Tests.Common
{
    using System.Collections.Generic;

    using NUnit.Framework;

    using Moq;

    using PoeShared.Common;

    using Shouldly;

    [TestFixture]
    public class PoeItemModEqualityComparerFixture
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        [TestCaseSource(nameof(ShouldReturnTrueTestCaseSource))]
        public void ShouldReturnTrue(IPoeItemMod item1, IPoeItemMod item2)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Equals(item1, item2);

            //Then
            result.ShouldBe(true);
        }

        [Test]
        [TestCaseSource(nameof(ShouldReturnFalseTestCaseSource))]
        public void ShouldReturnFalse(IPoeItemMod item1, IPoeItemMod item2)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Equals(item1, item2);

            //Then
            result.ShouldBe(false);
        }

        private IEnumerable<TestCaseData> ShouldReturnTrueTestCaseSource()
        {
            yield return new TestCaseData(
                    CreateItemMod(string.Empty, string.Empty),
                    CreateItemMod(string.Empty, string.Empty));
            yield return new TestCaseData(
                    CreateItemMod(string.Empty, "1"),
                    CreateItemMod(string.Empty, "1"));
        }

        private IEnumerable<TestCaseData> ShouldReturnFalseTestCaseSource()
        {
            yield return new TestCaseData(
                    CreateItemMod(string.Empty, "1"),
                    CreateItemMod(string.Empty, "2"));
            yield return new TestCaseData(
                    CreateItemMod("1", string.Empty),
                    CreateItemMod("1", "1"));
        }

        private IPoeItemMod CreateItemMod(string codeName, string name)
        {
            return Mock.Of<IPoeItemMod>(
                x => x.Name == name &&
                     x.CodeName == codeName);
        }

        private PoeItemModEqualityComparer CreateInstance()
        {
            return new PoeItemModEqualityComparer();
        }
    }
}