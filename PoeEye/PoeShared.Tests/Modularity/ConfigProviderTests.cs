using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using AutoFixture;
using LiteDB;
using NUnit.Framework;
using PoeShared.Modularity;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Modularity;

[TestFixture(typeof(ConfigProviderFromFile))]
[TestFixture(typeof(ConfigProviderFromMultipleFiles))]
public class ConfigProviderTests<T> : FixtureBase where T : IConfigProvider
{
    public sealed record ConfigAlpha : IPoeEyeConfig
    {
        public string Text { get; init; }
        public int Integer { get; init; }
    }
    
    public sealed record ConfigBeta : IPoeEyeConfig
    {
        public string Content { get; init; }
    }

    private Mock<IAppArguments> appArguments;

    protected override void SetUp()
    {
        var appDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
        Log.Info($"AppData: {appDataDirectory} (exists: {Directory.Exists(appDataDirectory)})");
        appArguments = new Mock<IAppArguments>();
        appArguments
            .SetupGet(x => x.SharedAppDataDirectory)
            .Returns(appDataDirectory);
        appArguments
            .SetupGet(x => x.AppDomainDirectory)
            .Returns(AppDomain.CurrentDomain.BaseDirectory);
        
        appArguments
            .SetupGet(x => x.Profile)
            .Returns("profile");

        appArguments.SetupGet(x => x.IsDebugMode).Returns(true);
        
        var configConverter = new PoeConfigConverter(
            new PoeConfigMetadataReplacementService(),
            new PoeConfigConverterMigrationService());
        var configSerializer = new JsonConfigSerializer(configConverter);
        
        Container.Register<IConfigSerializer>(() => configSerializer);
        Container.Register(() => appArguments.Object);

        if (Directory.Exists(appDataDirectory))
        {
            var filesToRemove = Directory.GetFiles(appDataDirectory);
            Log.Info($"Cleaning up directory {appDataDirectory}, files: {filesToRemove.DumpToString()}");
            foreach (var file in filesToRemove)
            {
                Log.Info($"Removing file {file}");
                File.Delete(file);
            }
        }
    }

    [Test]
    public void ShouldCreate()
    {
        //Given
        //When
        var action = () => CreateInstance();

        //Then
        action.ShouldNotThrow();
    }

    [Test]
    public void ShouldReload()
    {
        //Given
        var initial = new ConfigAlpha {Text = "test"};
        var updated = initial with { Text = "updated" };

        var instance = CreateInstance();
        instance.Save(initial);
        instance.GetActualConfig<ConfigAlpha>().ShouldBe(initial);

        //When
        instance.Save(updated);

        //Then
        instance.GetActualConfig<ConfigAlpha>().ShouldBe(updated);
    }

    [Test]
    public void ShouldSave()
    {
        //Given
        var instance = CreateInstance();
        var config = new ConfigAlpha(){ Text = "test" };
        var configHasChangedListener = instance.ConfigHasChanged.Listen();

        //When
        instance.Save(config);
        var loadedConfig = instance.GetActualConfig<ConfigAlpha>();

        //Then
        loadedConfig.ShouldBe(config);
    }

    [Test]
    public void ShouldLoad()
    {
        //Given
        var config = new ConfigAlpha() {Text = "test"};
        var instance = CreateInstance();
        instance.Save(config);

        //When
        var loadedConfig = instance.GetActualConfig<ConfigAlpha>();

        //Then
        loadedConfig.ShouldBe(config);
    }

    [Test]
    public void ShouldSaveMultipleTypes()
    {
        //Given
        var configA = new ConfigAlpha(){ Text = "test" };
        var configB = new ConfigBeta(){ Content = "content" };
        var instance = CreateInstance();
        instance.Save(configA);
        instance.Save(configB);

        //When
        var loadedConfigA = instance.GetActualConfig<ConfigAlpha>();
        var loadedConfigB = instance.GetActualConfig<ConfigBeta>();

        //Then
        loadedConfigA.ShouldBe(configA);
        loadedConfigB.ShouldBe(configB);
    }
    
    private IConfigProvider CreateInstance()
    {
        return Container.Create<T>();
    }
}