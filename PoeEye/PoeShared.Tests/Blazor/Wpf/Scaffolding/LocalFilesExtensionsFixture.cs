using System.IO;
using PoeShared.Blazor.Wpf.Scaffolding;

namespace PoeShared.Tests.Blazor.Wpf.Scaffolding;

[TestFixture]
internal class LocalFilesExtensionsFixtureTests : FixtureBase
{
    [Test]
    [TestCase("C:\\file.txt", "https://c/file.txt")]
    [TestCase("C:/file.txt", "https://c/file.txt")]
    [TestCase("C:/folder/file.txt", "https://c/folder/file.txt")]
    [TestCase("d:/folder/file.txt", "https://d/folder/file.txt")]
    [TestCase("d:/folder/../file.txt", "https://d/file.txt")]
    public void ShouldConvertToLocalFileUri(string inputPath, string expectedPath)
    {
        //Given
        var inputFile = new FileInfo(inputPath);

        //When
        var uri = inputFile.ToLocalFileUri();

        //Then
        uri.ToString().ShouldBe(expectedPath);
    }
}