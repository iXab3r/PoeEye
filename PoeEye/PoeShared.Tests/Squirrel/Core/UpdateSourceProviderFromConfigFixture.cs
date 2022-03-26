using NUnit.Framework;
using AutoFixture;
using System;
using PoeShared.Modularity;
using PoeShared.Squirrel.Updater;
using PoeShared.Tests.Helpers;
using Shouldly;

namespace PoeShared.Tests.Squirrel.Core;

[TestFixture]
public class UpdateSourceProviderFromConfigFixture : FixtureBase
{
    private IConfigProvider<UpdateSettingsConfig> configProvider;

    protected override void SetUp()
    {
        configProvider = new TestConfigProvider<UpdateSettingsConfig>();
        
        Container.Register(() => configProvider);
    }

    [Test]
    public void ShouldCreate()
    {
        // Given
        // When 
        Action action = () => CreateInstance();

        // Then
        action.ShouldNotThrow();
    }

    [Test]
    public void ShouldResetToFirstKnown()
    {
        // Given
        var config = configProvider.ActualConfig with
        {
            UpdateSource = CreateSource("a")
        };
        configProvider.Save(config);
        
        var instance = CreateInstance();

        // When
        instance.KnownSources = new[] { CreateSource("b") };

        // Then
        instance.UpdateSource.Id.ShouldBe("b");
    }

    private UpdateSourceInfo CreateSource(string id)
    {
        var result = new UpdateSourceInfo()
        {
            Id = $"{id}",
            Name = $"{id} name",
            Uris = new[] { $"{id} URL" }
        };
        result.IsValid.ShouldBe(true);
        return result;
    }

    private UpdateSourceProviderFromConfig CreateInstance()
    {
        return Container.Build<UpdateSourceProviderFromConfig>().Create();
    }
}