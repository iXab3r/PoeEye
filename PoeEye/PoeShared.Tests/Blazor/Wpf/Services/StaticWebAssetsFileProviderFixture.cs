using PoeShared.Blazor.Wpf.Services;

namespace PoeShared.Tests.Blazor.Wpf.Services;

public class StaticWebAssetsFileProviderFixture : FixtureBase
{
    [Test]
    public void ShouldCreate()
    {
        //Given

        //When
        var provider = new StaticWebAssetsFileProvider();

        //Then
        provider.RuntimeAssetsFile.ShouldNotBeNull();
    }
}