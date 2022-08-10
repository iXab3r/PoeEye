using NUnit.Framework;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class StringUtilsFixture : FixtureBase
{
    [Test]
    [TestCase("", false)]
    [TestCase("", true)]
    [TestCase("a", false)]
    [TestCase("a", true)]
    [TestCase("abcd", false)]
    [TestCase("abcd", true)]
    [TestCase("abcdefghijklmnopqrstuvwxyz01234567890", false)]
    [TestCase("abcdefghijklmnopqrstuvwxyz01234567890", true)]
    public void ShouldCompressToGzip(string input, bool requirePrefix)
    {
        //Given
        //When
        var compressed = StringUtils.CompressStringToGZip(input, includePrefix: requirePrefix);
        var result = StringUtils.DecompressStringFromGZip(compressed);

        //Then
        compressed.ShouldNotBe(input);
        result.ShouldBe(input);
    }
}