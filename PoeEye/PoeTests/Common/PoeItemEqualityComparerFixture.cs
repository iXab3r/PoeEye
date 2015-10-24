namespace PoeEye.Tests.Common
{
    using System.Collections.Generic;
    using System.Linq;

    using Moq;

    using NUnit.Framework;

    using PoeShared.Common;

    using Shouldly;

    [TestFixture]
    public class PoeItemEqualityComparerFixture
    {
        [Test]
        [TestCaseSource(nameof(ShouldReturnTrueTestCaseSource))]
        public void ShouldReturnTrue(IPoeItem item1, IPoeItem item2)
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
        public void ShouldReturnFalse(IPoeItem item1, IPoeItem item2)
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
                Mock.Of<IPoeItem>(x => x.UserForumUri == "1"),
                Mock.Of<IPoeItem>(x => x.UserForumUri == "1"));

            yield return new TestCaseData(
                Mock.Of<IPoeItem>(x => x.Price == "1"),
                Mock.Of<IPoeItem>(x => x.Price == "1"));

            yield return new TestCaseData(
                Mock.Of<IPoeItem>(x => x.ItemName == "1"),
                Mock.Of<IPoeItem>(x => x.ItemName == "1"));

            yield return new TestCaseData(
                Mock.Of<IPoeItem>(x => x.League == "1"),
                Mock.Of<IPoeItem>(x => x.League == "1"));

            yield return new TestCaseData(
                Mock.Of<IPoeItem>(x => x.UserForumName == "1"),
                Mock.Of<IPoeItem>(x => x.UserForumName == "1"));

            yield return new TestCaseData(
                Mock.Of<IPoeItem>(x => x.TradeForumUri == "1"),
                Mock.Of<IPoeItem>(x => x.TradeForumUri == "1"));

            yield return new TestCaseData(
                Mock.Of<IPoeItem>(x => x.ItemIconUri == "1"),
                Mock.Of<IPoeItem>(x => x.ItemIconUri == "1"));

            yield return new TestCaseData(
                Mock.Of<IPoeItem>(x => x.UserIsOnline == true),
                Mock.Of<IPoeItem>(x => x.UserIsOnline == true));

            yield return new TestCaseData(
               Mock.Of<IPoeItem>(x => x.IsCorrupted == true),
               Mock.Of<IPoeItem>(x => x.IsCorrupted == true));

            yield return new TestCaseData(
               CreateItemWithMods("1"),
               CreateItemWithMods("1"));

            yield return new TestCaseData(
               CreateItemWithMods("1", "2"),
               CreateItemWithMods("1", "2"));
        }

        private IEnumerable<TestCaseData> ShouldReturnFalseTestCaseSource()
        {
            yield return new TestCaseData(
               Mock.Of<IPoeItem>(x => x.UserForumUri == "1"),
               Mock.Of<IPoeItem>(x => x.UserForumUri == "2"));

            yield return new TestCaseData(
                Mock.Of<IPoeItem>(x => x.Price == "1"),
                Mock.Of<IPoeItem>(x => x.Price == "2"));

            yield return new TestCaseData(
                Mock.Of<IPoeItem>(x => x.ItemName == "1"),
                Mock.Of<IPoeItem>(x => x.ItemName == "2"));

            yield return new TestCaseData(
                Mock.Of<IPoeItem>(x => x.League == "1"),
                Mock.Of<IPoeItem>(x => x.League == "2"));

            yield return new TestCaseData(
                Mock.Of<IPoeItem>(x => x.UserForumName == "1"),
                Mock.Of<IPoeItem>(x => x.UserForumName == "2"));

            yield return new TestCaseData(
               Mock.Of<IPoeItem>(x => x.ItemIconUri == "1"),
               Mock.Of<IPoeItem>(x => x.ItemIconUri == "2"));

            yield return new TestCaseData(
                Mock.Of<IPoeItem>(x => x.TradeForumUri == "1"),
                Mock.Of<IPoeItem>(x => x.TradeForumUri == "2"));

            yield return new TestCaseData(
                Mock.Of<IPoeItem>(x => x.UserIsOnline == true),
                Mock.Of<IPoeItem>(x => x.UserIsOnline == false));

            yield return new TestCaseData(
               Mock.Of<IPoeItem>(x => x.IsCorrupted == true),
               Mock.Of<IPoeItem>(x => x.IsCorrupted == false));

            yield return new TestCaseData(
                CreateItemWithMods("1"),
                CreateItemWithMods("2"));

            yield return new TestCaseData(
               CreateItemWithMods("1"),
               CreateItemWithMods("1", "2"));

            yield return new TestCaseData(
               CreateItemWithMods("1", "2"),
               CreateItemWithMods("1", "22"));
        }

        private IPoeItem CreateItemWithMods(params string[] mods)
        {
            return Mock.Of<IPoeItem>(x => x.Mods == mods.Select(CreateItemMod).ToArray());
        }

        private IPoeItemMod CreateItemMod(string name)
        {
            return Mock.Of<IPoeItemMod>(x => x.Name == name);
        }

        private PoeItemEqualityComparer CreateInstance()
        {
            return new PoeItemEqualityComparer();
        }
    }
}