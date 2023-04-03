using PoeShared.IO;

namespace PoeShared.Tests.IO;

[TestFixture]
internal class OSPathFixtureTests : FixtureBase
{
    [Test]
    [TestCase("", "")]
    [TestCase("a", "a")]
    [TestCase("a\\b", "a\\b")]
    [TestCase("a\\b\\", "a\\b")]
    [TestCase("a/b", "a\\b")]
    [TestCase("a/b/", "a\\b")]
    public void ShouldNormalizeWindowsPath(string path, string expected)
    {
        //Given
        //When
        var result = OSPath.ToWindowsPath(path);

        //Then
        result.ShouldBe(expected);
    }
    
    [Test]
    [TestCase("", "")]
    [TestCase("a", "a")]
    [TestCase("a/b", "a/b")]
    [TestCase("a/b/", "a/b")]
    [TestCase("a\\b", "a/b")]
    [TestCase("a\\b\\", "a/b")]
    public void ShouldNormalizeNonWindowsPath(string path, string expected)
    {
        //Given
        //When
        var result = OSPath.ToUnixPath(path);

        //Then
        result.ShouldBe(expected);
    }
}