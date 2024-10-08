using System.IO;
using Moq;
using NUnit.Framework;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Modularity;

[TestFixture]
public class PoeConfigConverterTests : FixtureBase
{
    private Mock<IPoeConfigConverterMigrationService> migrationService;
    private Mock<IPoeConfigMetadataReplacementService> replacementService;
    private PoeConfigConverter configConverter;

    private delegate void TryGetReplacementDelegate(PoeConfigMetadata input, out PoeConfigMetadata output);

    protected override void SetUp()
    {
        migrationService = new Mock<IPoeConfigConverterMigrationService>();
        replacementService = new Mock<IPoeConfigMetadataReplacementService>();

        PoeConfigMetadata replacementMetadata;
        replacementService
            .Setup(x => x.TryGetReplacement(It.IsAny<PoeConfigMetadata>(), out replacementMetadata))
            .Returns((PoeConfigMetadata _, PoeConfigMetadata _) => false);
        
        configConverter = new PoeConfigConverter(replacementService.Object, migrationService.Object);
    }

    [Test]
    public void ShouldLoadUnknownMetadata()
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.Deserialize<PoeConfigMetadata>(PrepareSerialized("UnknownType.json"));

        //Then
        result.ShouldNotBeNull();
        result.ShouldBeOfType<PoeConfigMetadata>();
        result.TypeName.ShouldBe("Unknown");
        result.Version.ShouldBe(12);
    }

    [Test]
    public void ShouldLoadUnknownTypes()
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.Deserialize<IPoeEyeConfig>(PrepareSerialized("UnknownType.json"));

        //Then
        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<IPoeEyeConfig>();
        result.ShouldBeAssignableTo<PoeConfigMetadata>();
    }

    [Test]
    public void ShouldLoadUnknownAssembly()
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.Deserialize<IPoeEyeConfig>(PrepareSerialized("UnknownAssembly.json"));

        //Then
        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<IPoeEyeConfig>();
        result.ShouldBeAssignableTo<PoeConfigMetadata>();
    }

    [Test]
    public void ShouldLoadUnknownGenericMetadata()
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.Deserialize<PoeConfigMetadata<SampleConfig>>(PrepareSerialized("UnknownType.json"));

        //Then
        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<PoeConfigMetadata>();
        result.ShouldBeOfType<PoeConfigMetadata<SampleConfig>>();
            
        result.TypeName.ShouldBe("Unknown");
        result.Version.ShouldBe(12);
        result.ConfigValue["Value"].ShouldBe("value");
        result.Value.ShouldBeNull();
    }

    [Test]
    public void ShouldLoadGenericMetadata()
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.Deserialize<PoeConfigMetadata<SampleConfig>>(PrepareSerialized("SampleConfig.json"));

        //Then
        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<PoeConfigMetadata>();
        result.ShouldBeOfType<PoeConfigMetadata<SampleConfig>>();
            
        result.Version.ShouldBeNull();
        result.ConfigValue["Value"].ShouldBe("Version#1");
        result.Value.ShouldNotBeNull();
        result.Value.Value.ShouldBe("Version#1");
    }
        
    [Test]
    public void ShouldLoadGenericVersionedMetadata()
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.Deserialize<PoeConfigMetadata<SampleVersionedConfig>>(PrepareSerialized("SampleConfigVersioned.json"));

        //Then
        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<PoeConfigMetadata>();
        result.ShouldBeOfType<PoeConfigMetadata<SampleVersionedConfig>>();
            
        result.Version.ShouldBe(2);
        result.ConfigValue["Value"].ShouldBe("Version#1");
        result.Value.ShouldNotBeNull();
        result.Value.Value.ShouldBe("Version#1");
    }
        
    [Test]
    public void ShouldLoadGenericVersionedMetadataWithUnknownVersion()
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.Deserialize<PoeConfigMetadata<SampleVersionedConfig>>(PrepareSerialized("SampleConfigWrongVersion.json"));

        //Then
        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<PoeConfigMetadata>();
        result.ShouldBeOfType<PoeConfigMetadata<SampleVersionedConfig>>();
            
        result.Version.ShouldBe(1);
        result.ConfigValue["Value"].ShouldBe("Version#1");
        result.Value.ShouldNotBeNull();
        result.Value.Value.ShouldBe("Version#2");
    }

    [Test]
    public void ShouldSerializeLoadedUnknownTypes()
    {
        //Given
        var instance = CreateInstance();
        var deserializedValue = instance.Deserialize<IPoeEyeConfig>(PrepareSerialized("UnknownType.json"));

        //When
        var secondarySerializedValue = instance.Serialize(deserializedValue);
        var result = instance.Deserialize<IPoeEyeConfig>(secondarySerializedValue);

        //Then
        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<IPoeEyeConfig>();
    }
    
    [Test]
    public void ShouldLoadDeepNestedConfig()
    {
        //Given
        var instance = CreateInstance();
        var deserializedValue = instance.Deserialize<IPoeEyeConfig>(PrepareSerialized("DeepSampleConfig.json"));

        //When
        var secondarySerializedValue = instance.Serialize(deserializedValue);
        var result = instance.Deserialize<IPoeEyeConfig>(secondarySerializedValue);

        //Then
        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<IPoeEyeConfig>();
    }

    [Test]
    public void ShouldLoadVersionedTypes()
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.Deserialize<IPoeEyeConfig>(PrepareSerialized("SampleConfigVersioned.json"));

        //Then
        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<SampleVersionedConfig>();
        ((SampleVersionedConfig) result).Version.ShouldBe(2);
        ((SampleVersionedConfig) result).Value.ShouldBe("Version#1");
    }
        
        
    [Test]
    public void ShouldLoadVersionedTypesWithWrongVersion()
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.Deserialize<IPoeEyeConfig>(PrepareSerialized("SampleConfigWrongVersion.json"));

        //Then
        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<SampleVersionedConfig>();
        ((SampleVersionedConfig) result).Version.ShouldBe(2);
        ((SampleVersionedConfig) result).Value.ShouldBe("Version#2");
    }

    [Test]
    public void ShouldSave()
    {
        //Given
        var instance = CreateInstance();
        var value = new SampleConfig {Value = "value"};

        //When
        var serializedValue = instance.Serialize(value);
        var result = instance.Deserialize<SampleConfig>(serializedValue);

        //Then
        result.Value.ShouldBe(value.Value);
    }

    [Test]
    public void ShouldSaveCombined()
    {
        //Given
        var instance = CreateInstance();
        var sampleConfig = new SampleConfig {Value = "value"};
        var valueToSerialize = new SampleCombinedConfig
        {
            Configs = new IPoeEyeConfig[] {sampleConfig}
        };

        //When
        var serializedValue = instance.Serialize(valueToSerialize);
        var result = instance.Deserialize<SampleCombinedConfig>(serializedValue);

        //Then
        result.Configs.Length.ShouldBe(1, $"Serialized value:\n{serializedValue}");
        result.Configs[0].ShouldBeOfType<SampleConfig>();
        ((SampleConfig) result.Configs[0]).Value.ShouldBe("value");
    }

    [Test]
    public void ShouldSaveNested()
    {
        //Given
        var instance = CreateInstance();
        var value = new SampleContainerConfig {InnerConfig = new SampleConfig {Value = "value"}};

        //When
        var serializedValue = instance.Serialize(value);
        var result = instance.Deserialize<SampleContainerConfig>(serializedValue);

        //Then
        result.ShouldBeOfType<SampleContainerConfig>();
        result.InnerConfig.ShouldBeOfType<SampleConfig>();
        ((SampleConfig) result.InnerConfig).Value.ShouldBe("value");
    }

    [Test]
    public void ShouldSaveMetadata()
    {
        //Given
        var instance = CreateInstance();
        var sampleConfig = new SampleConfig {Value = "value"};
        var serializedMetadata = instance.Serialize(sampleConfig);
        var metadata = instance.Deserialize<PoeConfigMetadata>(serializedMetadata);

        //When
        var result = instance.Serialize(metadata);

        //Then
        result.ShouldBe(serializedMetadata);
    }

    [Test]
    public void ShouldReplaceMetadata()
    {
        //Given
        var instance = CreateInstance();
        var sourceMetadata = new PoeConfigMetadata()
        {
            AssemblyName = "EyeAuras",
            TypeName = "test",
            Version = 1
        };
        var destinationMetadata = new PoeConfigMetadata();
        
        PoeConfigMetadata replacementMetadata;
        replacementService
            .Setup(x => x.TryGetReplacement(It.Is<PoeConfigMetadata>(y => y.TypeName == sourceMetadata.TypeName), out replacementMetadata))
            .Callback(new TryGetReplacementDelegate((PoeConfigMetadata input, out PoeConfigMetadata output) => { output = destinationMetadata; }))
            .Returns((PoeConfigMetadata _, PoeConfigMetadata _) => true);
        
        var serializedMetadata = instance.Serialize(sourceMetadata);

        //When
        var metadata = instance.Deserialize<PoeConfigMetadata>(serializedMetadata);

        //Then
        metadata.ShouldBeSameAs(destinationMetadata);
    }

    [Test]
    public void ShouldThrowWhenVersionIsNotSupported()
    {
        //Given
        var instance = CreateInstance();
        var value = new SampleVersionedConfig() {Value = "value", Version = new SampleVersionedConfig().Version + 1};

        //When
        var serializedValue = instance.Serialize(value);
        var action = () => instance.Deserialize<SampleVersionedConfig>(serializedValue);

        //Then
        action.ShouldThrow<PoeConfigException>();
    }
    
    [Test]
    public void ShouldThrowWhenFailedToReplaceMetadata()
    {
        //Given
        var instance = CreateInstance();
        var sourceMetadata = new SampleConfig();
        var serializedMetadata = instance.Serialize(sourceMetadata);

        PoeConfigMetadata replacementMetadata;
        replacementService
            .Setup(x => x.TryGetReplacement(It.IsAny<PoeConfigMetadata>(), out replacementMetadata))
            .Callback(new TryGetReplacementDelegate((PoeConfigMetadata input, out PoeConfigMetadata output) => { output = default; }))
            .Returns((PoeConfigMetadata _, PoeConfigMetadata _) => true);

        //When
        var action = () =>  instance.Deserialize<PoeConfigMetadata>(serializedMetadata);

        //Then
        action.ShouldThrow<PoeConfigException>();
    }

    [Test]
    [Ignore("Does not work because currently everything is hard-wired onto IPoeEyeConfig")]
    public void ShouldSerializeMetadataContainer()
    {
        //Given
        var instance = CreateInstance();
        var config = new SampleMetadataContainerConfig()
        {
            Metadata = PoeConfigMetadata.FromValue("string value")
        };
        var serialized = instance.Serialize(config);

        //When
        var deserialized = instance.Deserialize<SampleMetadataContainerConfig>(serialized);

        //Then
        //var metadata = (PoeConfigMetadata<string>) deserialized.Metadata;
        //metadata.Value.ShouldBe("string value");
    }
        
    private static string PrepareSerialized(string fileName)
    {
        var filePath = Path.Combine(@"Modularity\\Samples", fileName);
        return File.ReadAllText(filePath);
    }
        
    private JsonConfigSerializer CreateInstance()
    {
        var instance = new JsonConfigSerializer();
        instance.RegisterConverter(configConverter);
        return instance;
    }
}