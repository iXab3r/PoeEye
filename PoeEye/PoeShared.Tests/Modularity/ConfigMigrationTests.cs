using NUnit.Framework;
using System;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.Services;
using Shouldly;

namespace PoeShared.Tests.Modularity;

[TestFixture]
public class ConfigMigrationTests : FixtureBase
{
    private sealed record ConfigV1 : IPoeEyeConfigVersioned
    {
        public string StringValue { get; set; }

        public int Version { get; set; } = 1;
    }

    private sealed record ConfigV2 : IPoeEyeConfigVersioned
    {
        public int IntValue { get; set; }

        public int Version { get; set; } = 2;
    }

    private sealed record ConfigV3 : IPoeEyeConfigVersioned
    {
        public double DoubleValue { get; set; }

        public int Version { get; set; } = 3;
    }

    private sealed record Config : IPoeEyeConfigVersioned
    {
        public string StringValue { get; set; }

        public int Version { get; set; } = 4;
    }

    private abstract class ConfigMetadataConverterV1ToV2Base : ConfigMetadataConverter<ConfigMigrationTests.ConfigV1, ConfigMigrationTests.ConfigV2>
    {
    }
        
    private sealed class ConfigMetadataConverterV1ToV2 : ConfigMetadataConverterV1ToV2Base
    {
        public override ConfigMigrationTests.ConfigV2 Convert(ConfigMigrationTests.ConfigV1 value)
        {
            return new ConfigMigrationTests.ConfigV2()
            {
                IntValue = Int32.Parse(value.StringValue) + 1
            };
        }
    }

    private sealed class ConfigMetadataConverterV2ToV3 : ConfigMetadataConverter<ConfigMigrationTests.ConfigV2, ConfigMigrationTests.ConfigV3>
    {
        public override ConfigMigrationTests.ConfigV3 Convert(ConfigMigrationTests.ConfigV2 value)
        {
            return new ConfigMigrationTests.ConfigV3()
            { 
                DoubleValue = value.IntValue * 10
            };
        }
    }

    private sealed class ConfigMetadataConverterV3ToV4 : ConfigMetadataConverter<ConfigMigrationTests.ConfigV3, ConfigMigrationTests.Config>
    {
        public override ConfigMigrationTests.Config Convert(ConfigMigrationTests.ConfigV3 value)
        {
            return new ConfigMigrationTests.Config()
            { 
                StringValue = $"Number is {value.DoubleValue:F0}"
            };
        }
    }

    private static readonly string SerializedConfigV1 = @"{
  'TypeName': 'PoeShared.Tests.Modularity.ConfigMigrationTests+Config, PoeShared.Tests',
  'ConfigValue': {
    'StringValue': '1'
  },
  'Version': 1
}";

    private static readonly string SerializedConfigV2 = @"{
  'TypeName': 'PoeShared.Tests.Modularity.ConfigMigrationTests+Config, PoeShared.Tests',
  'ConfigValue': {
    'IntValue': '2'
  },
  'Version': 2
}";

    private static readonly string SerializedConfigV3 = @"{
  'TypeName': 'PoeShared.Tests.Modularity.ConfigMigrationTests+Config, PoeShared.Tests',
  'ConfigValue': {
    'DoubleValue': '20'
  },
  'Version': 3
}";

    private static readonly string SerializedConfigV4 = @"{
  'TypeName': 'PoeShared.Tests.Modularity.ConfigMigrationTests+Config, PoeShared.Tests',
  'ConfigValue': {
    'StringValue': 'Number is 20'
  },
  'Version': 4
}";

    private PoeConfigConverter configConverter;
    private PoeConfigConverterMigrationService migrationService;
    private PoeConfigMetadataReplacementService replacementService;

    protected override void SetUp()
    {
        var assemblyTracker = new AssemblyTracker();
        migrationService = new PoeConfigConverterMigrationService(assemblyTracker)
        {
            AutomaticallyLoadConverters = false
        };
        migrationService.Clear();
        
        replacementService = new PoeConfigMetadataReplacementService(assemblyTracker);
        configConverter = new PoeConfigConverter(replacementService, migrationService);
    }

    [Test]
    public void ShouldDeserializeV1()
    {
        //Given
        var instance = CreateInstance();
        RegisterAll();

        //When
        var result = instance.Deserialize<IPoeEyeConfigVersioned>(SerializedConfigV1);


        //Then
        result.Version.ShouldBe(4);
        result.GetPropertyValue<string>(nameof(Config.StringValue)).ShouldBe("Number is 20");
    }

    [Test]
    public void ShouldDeserializeV2()
    {
        //Given
        var instance = CreateInstance();
        RegisterAll();

        //When
        var result = instance.Deserialize<IPoeEyeConfigVersioned>(SerializedConfigV2);


        //Then
        result.Version.ShouldBe(4);
        result.GetPropertyValue<string>(nameof(Config.StringValue)).ShouldBe("Number is 20");
    }


    [Test]
    public void ShouldDeserializeV3()
    {
        //Given
        var instance = CreateInstance();
        RegisterAll();

        //When
        var result = instance.Deserialize<IPoeEyeConfigVersioned>(SerializedConfigV3);


        //Then
        result.Version.ShouldBe(4);
        result.GetPropertyValue<string>(nameof(Config.StringValue)).ShouldBe("Number is 20");
    }

    [Test]
    public void ShouldDeserializeV4()
    {
        //Given
        var instance = CreateInstance();
        RegisterAll();

        //When
        var result = instance.Deserialize<IPoeEyeConfigVersioned>(SerializedConfigV4);


        //Then
        result.Version.ShouldBe(4);
        result.GetPropertyValue<string>(nameof(Config.StringValue)).ShouldBe("Number is 20");
    }

    [Test]
    public void ShouldDeserializeToDefaultIfConverterInChainIsMissing()
    {
        //Given
        var instance = CreateInstance();
        migrationService.RegisterMetadataConverter(new ConfigMetadataConverterV1ToV2());
        migrationService.RegisterMetadataConverter(new ConfigMetadataConverterV3ToV4());

        //When
        var result = instance.Deserialize<IPoeEyeConfigVersioned>(SerializedConfigV2);

        //Then
        result.Version.ShouldBe(4);
        result.GetPropertyValue<string>(nameof(Config.StringValue)).ShouldBeNullOrEmpty();
    }

    [Test]
    public void ShouldAutomaticallyLoadConvertersWhenNeeded()
    {
        //Given
        migrationService.AutomaticallyLoadConverters = true;
        var instance = CreateInstance();

        //When
        var result = instance.Deserialize<IPoeEyeConfigVersioned>(SerializedConfigV1);

        //Then
        result.Version.ShouldBe(4);
        result.GetPropertyValue<string>(nameof(Config.StringValue)).ShouldBe("Number is 20");
    }

    [Test]
    [TestCase(typeof(ConfigMetadataConverter<,>), false)]
    [TestCase(typeof(ConfigMetadataConverterV1ToV2Base), false)]
    [TestCase(typeof(ConfigMetadataConverter<SampleVersionedConfig,SampleVersionedConfig>), false)]
    [TestCase(typeof(ConfigMetadataConverterV1ToV2), true)]
    [TestCase(typeof(ConfigMetadataConverterV2ToV3), true)]
    [TestCase(typeof(ConfigMetadataConverterV3ToV4), true)]
    [TestCase(typeof(PoeConfigMetadata), false)]
    public void ShouldDetectMetadataConverter(Type type, bool expected)
    {
        //Given
        //When
        var isConverter = migrationService.IsMetadataConverter(type);

        //Then
        isConverter.ShouldBe(expected);
    }

    [Test]
    [TestCase(typeof(object), 0, 0, false)]
    [TestCase(typeof(Config), 1, 4, true)]
    public void ShouldResolveConverter(Type targetType, int sourceVersion, int targetVersion, bool expected)
    {
        //Given
        migrationService.AutomaticallyLoadConverters = true;

        //When
        var result = migrationService.TryGetConverter(targetType, sourceVersion, targetVersion, out var converterResult);


        //Then
        result.ShouldBe(expected);
        if (result)
        {
            converterResult.ShouldNotBe(default);
        }
        else
        {
            converterResult.ShouldBe(default);
        }
    }

    [Test]
    public void ShouldThrowOnDuplicateRegistration()
    {
        //Given
        migrationService.AutomaticallyLoadConverters = true;

        //When
        var action = () => RegisterAll();

        //Then
        action.ShouldThrow<InvalidOperationException>();
    }
    
    [Test]
    public void ShouldReplaceIfNeeded()
    {
        //Given
        var instance = CreateInstance();
        var sourceMetadata = new PoeConfigMetadata()
        {
            AssemblyName = "EyeAuras",
            TypeName = "test",
            Version = 1
        };
        replacementService.AddMetadataReplacement(sourceMetadata.TypeName, typeof(SampleConfig));
        var serializedMetadata = instance.Serialize(sourceMetadata);

        //When
        var result = instance.Deserialize<PoeConfigMetadata>(serializedMetadata);

        //Then
        result.AssemblyName.ShouldBe(typeof(SampleConfig).Assembly.GetName().Name);
        result.TypeName.ShouldBe(typeof(SampleConfig).FullName);
    }
        
    private void RegisterAll()
    {
        migrationService.RegisterMetadataConverter(new ConfigMetadataConverterV3ToV4());
        migrationService.RegisterMetadataConverter(new ConfigMetadataConverterV1ToV2());
        migrationService.RegisterMetadataConverter(new ConfigMetadataConverterV2ToV3());
    }

    private JsonConfigSerializer CreateInstance()
    {
        var instance = new JsonConfigSerializer();
        instance.RegisterConverter(configConverter);
        return instance;
    }
}