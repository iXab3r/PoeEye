using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using AutoFixture;
using LiteDB;
using Newtonsoft.Json.Linq;
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

    private IPoeConfigMetadataReplacementService metadataReplacementService;
    private IPoeConfigConverterMigrationService poeConfigConverterMigrationService;

    protected override void SetUp()
    {
        var appDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appdata");
        Log.Info(() => $"AppData: {appDataDirectory} (exists: {Directory.Exists(appDataDirectory)})");
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

        metadataReplacementService = new PoeConfigMetadataReplacementService();
        poeConfigConverterMigrationService = new PoeConfigConverterMigrationService();

        Container.Register(() => metadataReplacementService);
        Container.Register(() => poeConfigConverterMigrationService);
        Container.Register(() => appArguments.Object);
        Container.Register<IConfigSerializer>(() => Container.Create<JsonConfigSerializer>());

        if (Directory.Exists(appDataDirectory))
        {
            var filesToRemove = Directory.GetFiles(appDataDirectory);
            Log.Info(() => $"Cleaning up directory {appDataDirectory}, files: {filesToRemove.DumpToString()}");
            foreach (var file in filesToRemove)
            {
                Log.Info(() => $"Removing file {file}");
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

    [Test]
    public void ShouldMigrate()
    {
        //Given
        var source = Container.Create<PoeEyeConfigProviderInMemory>();
        var configA = new ConfigAlpha(){ Text = "test" };
        var configB = new ConfigBeta(){ Content = "content" };
        
        source.Save(configA);
        source.Save(configB);

        var instance = CreateInstance();

        var migrator = Container.Create<ConfigMigrator>();

        //When
        migrator.Migrate(source, instance);

        //Then
        foreach (var sourceConfig in source.Configs.Items)
        {
            var method = instance.GetType()
                .GetMethod(nameof(instance.GetActualConfig))
                .MakeGenericMethod(sourceConfig.GetType());
            var result = (IPoeEyeConfig)method.Invoke(instance, Array.Empty<object>());
            result.ShouldBe(sourceConfig);
        }
    }
    
    [Test]
    public void ShouldMigrateMetadata()
    {
        //Given
        var source = Container.Create<PoeEyeConfigProviderInMemory>();
        var metadataConfig = new PoeConfigMetadata()
        {
            AssemblyName = "EyeAuras.Loader",
            TypeName = "EyeAuras.Loader.Prism.PlusLoaderConfig",
            ConfigValue = JToken.Parse(@"{
        'AssemblyName': 'EyeAuras.Loader',
        'TypeName': 'EyeAuras.Loader.Prism.PlusLoaderConfig',
        'Version': 7,
        'ConfigValue': {
          'Username': 'Xab3r',
          'Version': 7
        }
      }")
        };
        
        source.Save(metadataConfig);

        var instance = CreateInstance();

        var migrator = Container.Create<ConfigMigrator>();

        //When
        migrator.Migrate(source, instance);

        //Then
        var src = source.Configs.Items.ToArray().Single();
        var dst = source.Configs.Items.ToArray().Single();
        src.ShouldBe(dst);
    }
    
    private IConfigProvider CreateInstance()
    {
        return Container.Create<T>();
    }
}