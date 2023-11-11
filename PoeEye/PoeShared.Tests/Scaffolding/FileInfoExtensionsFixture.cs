using System;
using System.IO;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
internal class FileInfoExtensionsFixtureTests : FixtureBase
{
    [Test]
    [TestCase("C:\\", "C")]
    [TestCase("C:/", "C")]
    [TestCase("C:\\test.txt", "C")]
    [TestCase("d:\\test.txt", "d")]
    [TestCase("D:/folder/subfolder/", "D")]
    public void ShouldGetDriveLetter(string path, string expected)
    {
        //Given
        var entry = new FileInfo(path);

        //When
        //Then
        entry.GetDriveLetter().ShouldBe(expected);
    }

    [Test]
    public void ShouldThrowArgumentNullExceptionForNullFileInfo()
    {
        // Given
        FileInfo entry = null;

        // When
        TestDelegate testDelegate = () => entry.GetDriveLetter();

        // Then
        Assert.Throws<ArgumentNullException>(testDelegate);
    }
}