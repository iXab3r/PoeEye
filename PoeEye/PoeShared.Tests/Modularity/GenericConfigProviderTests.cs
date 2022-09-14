using System.Reactive;
using System.Reactive.Subjects;
using AutoFixture;
using DynamicData;
using NUnit.Framework;
using PoeShared.Modularity;
using PoeShared.Services;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Modularity;

[TestFixture]
public class GenericConfigProviderTests : FixtureBase
{
    private Mock<IConfigProvider> configProvider;
    private Mock<IComparisonService> comparisonService;

    private ISubject<Unit> configHasChangedSink;
    private IntermediateCache<IPoeEyeConfig, string> configsCache;

    protected override void SetUp()
    {
        configsCache = new IntermediateCache<IPoeEyeConfig, string>();
        configHasChangedSink = new Subject<Unit>();

        comparisonService = Container.RegisterMock<IComparisonService>();
        
        configProvider = Container.RegisterMock<IConfigProvider>();
        configProvider.SetupGet(x => x.Configs).Returns(configsCache);
        configProvider.SetupGet(x => x.ConfigHasChanged).Returns(configHasChangedSink);
    }

    [Test]
    public void ShouldLoadInitialConfig()
    {
        //Given
        var config = new SampleConfig();
        configProvider.Setup(x => x.GetActualConfig<SampleConfig>()).Returns(config);
        
        var instance = CreateInstance();

        //When

        //Then
        instance.ActualConfig.ShouldBeSameAs(config);
    }

    [Test]
    public void ShouldReloadConfigWhenNotified()
    {
        //Given
        var instance = CreateInstance();
        var config = new SampleConfig();
        configProvider.Setup(x => x.GetActualConfig<SampleConfig>()).Returns(config);

        //When
        configProvider.Invocations.Clear();
        configHasChangedSink.OnNext(Unit.Default);

        //Then
        configProvider.Verify(x => x.GetActualConfig<SampleConfig>(), Times.Once);
        instance.ActualConfig.ShouldBeSameAs(config);
    }

    private GenericConfigProvider<SampleConfig> CreateInstance()
    {
        return Container.Create<GenericConfigProvider<SampleConfig>>();
    }
}