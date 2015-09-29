namespace PoeEye.Tests.Common
{
    using System;
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
        public void ShouldReturnTrue(Tuple<IPoeItemMod, IPoeItemMod> pair)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Equals(pair.Item1, pair.Item2);

            //Then
            result.ShouldBe(true);
        }

        [Test]
        [TestCaseSource(nameof(ShouldReturnFalseTestCaseSource))]
        public void ShouldReturnFalse(Tuple<IPoeItemMod, IPoeItemMod> pair)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Equals(pair.Item1, pair.Item2);

            //Then
            result.ShouldBe(false);
        }

        private IEnumerable<Tuple<IPoeItemMod, IPoeItemMod>> ShouldReturnTrueTestCaseSource()
        {
            yield return new Tuple<IPoeItemMod, IPoeItemMod>(
                    CreateItemMod(string.Empty, string.Empty),
                     CreateItemMod(string.Empty, string.Empty));
            yield return new Tuple<IPoeItemMod, IPoeItemMod>(
                    CreateItemMod(string.Empty, "1"),
                     CreateItemMod(string.Empty, "1"));
        }

        private IEnumerable<Tuple<IPoeItemMod, IPoeItemMod>> ShouldReturnFalseTestCaseSource()
        {
            yield return new Tuple<IPoeItemMod, IPoeItemMod>(
                    CreateItemMod(string.Empty, "1"),
                     CreateItemMod(string.Empty, "2"));
            yield return new Tuple<IPoeItemMod, IPoeItemMod>(
                    CreateItemMod("1", string.Empty),
                     CreateItemMod("2", string.Empty));
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