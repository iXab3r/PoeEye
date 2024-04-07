using System;
using System.IO;

namespace PoeShared.Tests.Scaffolding;

public class IconHelperFixture : FixtureBase
{
    protected DirectoryInfo storageFolder;
    protected DirectoryInfo assetsFolder;

    protected override void SetUp()
    {
        base.SetUp();
    }

    protected override void OneTimeSetUp()
    {
        base.OneTimeSetUp();

        storageFolder = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scaffolding", nameof(IconHelperFixture)));
        assetsFolder = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Assets"));
        Log.Info($"Storage directory: '{storageFolder.FullName}'");
        if (storageFolder.Exists)
        {
            storageFolder.RemoveEverythingInside();
        }
    }


    [Test]
    public void ShouldConvertBmp()
    {
        //Given
        var sourcePath = Path.Combine(assetsFolder.FullName, "switch.bmp");
        var outputPath = Path.Combine(storageFolder.FullName, "ShouldConvertBmp/", "switch.ico");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        //When
        using (var sourceBmp = File.OpenRead(sourcePath))
        using (var outputIco = File.OpenWrite(outputPath))
        {
            IconHelper.ConvertBitmapToIcon(sourceBmp, outputIco);
        }

        //Then
        File.Exists(outputPath).ShouldBeTrue($"Icon not found @ {outputPath}");
    }
}