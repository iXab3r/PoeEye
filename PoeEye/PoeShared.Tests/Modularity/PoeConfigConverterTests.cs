using log4net;
using NUnit.Framework;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Modularity
{
    [TestFixture]
    public class PoeConfigConverterTests
    {
        private static readonly IFluentLog Log = typeof(PoeConfigConverterTests).PrepareLogger();
        
        private static readonly string SerializedUnknownType = @"{
  'AssemblyName': '" + typeof(SampleVersionedConfig).Assembly.GetName().Name + @"',
  'TypeName': 'Unknown',
  'ConfigValue': {
    'Value': 'value'
  },
  'Version': 12
}";
        
        private static readonly string SerializedUnknownAssembly = @"{
  'AssemblyName': 'UnknownAssembly',
  'TypeName': 'Unknown',
  'ConfigValue': {
    'Value': 'value'
  },
  'Version': 12
}";
        
        private static readonly string SerializedWellKnownVersioned = @"{
  'AssemblyName': '" + typeof(SampleVersionedConfig).Assembly.GetName().Name + @"',
  'TypeName': '" + typeof(SampleVersionedConfig).FullName + @"',
  'ConfigValue': {
    'Value': 'Version#1'
  },
  'Version': 1
}";
        
        private static readonly string SerializedWellKnown = @"{
  'AssemblyName': '" + typeof(SampleConfig).Assembly.GetName().Name + @"',
  'TypeName': '" + typeof(SampleConfig).FullName + @"',
  'ConfigValue': {
    'Value': 'Version#1'
  },
  'Version': 1
}";

        public interface IPoeEyeConfigInherited : IPoeEyeConfig
        {
        }

        public sealed class CombinedConfig : IPoeEyeConfig
        {
            public IPoeEyeConfig[] Configs { get; set; }
        }

        public sealed class SampleConfig : IPoeEyeConfig
        {
            public string Value { get; set; }
        }
        
        public sealed class SampleInheritedConfig : IPoeEyeConfigInherited
        {
            public string InheritedValue { get; set; }
        }

        public sealed class SampleVersionedConfig : IPoeEyeConfigVersioned
        {
            public string Value { get; set; } = "Version#2";

            public int Version { get; set; } = 2;
        }

        public sealed class SampleNestedConfig : IPoeEyeConfig
        {
            public IPoeEyeConfig InnerConfig { get; set; }
        }

        [Test]
        public void ShouldLoadUnknownTypes()
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Deserialize<IPoeEyeConfig>(SerializedUnknownType);

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
            var result = instance.Deserialize<IPoeEyeConfig>(SerializedUnknownAssembly);

            //Then
            result.ShouldNotBeNull();
            result.ShouldBeAssignableTo<IPoeEyeConfig>();
            result.ShouldBeAssignableTo<PoeConfigMetadata>();
        }
        
        [Test]
        public void ShouldLoadUnknownMetadata()
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Deserialize<PoeConfigMetadata>(SerializedUnknownType);

            //Then
            result.ShouldNotBeNull();
            result.ShouldBeOfType<PoeConfigMetadata>();
            result.TypeName.ShouldBe("Unknown");
            result.Version.ShouldBe(12);
        }
        
        [Test]
        public void ShouldLoadUnknownGenericMetadata()
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Deserialize<PoeConfigMetadata<SampleConfig>>(SerializedUnknownType);

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
            var result = instance.Deserialize<PoeConfigMetadata<SampleConfig>>(SerializedWellKnown);

            //Then
            result.ShouldNotBeNull();
            result.ShouldBeAssignableTo<PoeConfigMetadata>();
            result.ShouldBeOfType<PoeConfigMetadata<SampleConfig>>();
            
            result.Version.ShouldBe(1);
            result.ConfigValue["Value"].ShouldBe("Version#1");
            result.Value.ShouldNotBeNull();
            result.Value.Value.ShouldBe("Version#1");
        }
        
        [Test]
        public void ShouldSerializeLoadedUnknownTypes()
        {
            //Given
            var instance = CreateInstance();
            var deserializedValue = instance.Deserialize<IPoeEyeConfig>(SerializedUnknownType);

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
            var result = instance.Deserialize<IPoeEyeConfig>(SerializedWellKnownVersioned);

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
            var valueToSerialize = new CombinedConfig
            {
                Configs = new IPoeEyeConfig[] {sampleConfig}
            };

            //When
            var serializedValue = instance.Serialize(valueToSerialize);
            var result = instance.Deserialize<CombinedConfig>(serializedValue);

            //Then
            result.Configs.Length.ShouldBe(1, () => $"Serialized value:\n{serializedValue}");
            result.Configs[0].ShouldBeOfType<SampleConfig>();
            ((SampleConfig) result.Configs[0]).Value.ShouldBe("value");
        }

        [Test]
        public void ShouldSaveNested()
        {
            //Given
            var instance = CreateInstance();
            var value = new SampleNestedConfig {InnerConfig = new SampleConfig {Value = "value"}};

            //When
            var serializedValue = instance.Serialize(value);
            var result = instance.Deserialize<SampleNestedConfig>(serializedValue);

            //Then
            result.ShouldBeOfType<SampleNestedConfig>();
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
        public void ShouldSaveGenericMetadata()
        {
            //Given
            var instance = CreateInstance();
            var sampleConfig = new SampleConfig {Value = "value"};
            var serializedMetadata = instance.Serialize(sampleConfig);
            var metadata = instance.Deserialize<PoeConfigMetadata<SampleConfig>>(serializedMetadata);

            //When
            var result = instance.Serialize(metadata);

            //Then
            result.ShouldBe(serializedMetadata);
        }
        
        private JsonConfigSerializer CreateInstance()
        {
            var result = new JsonConfigSerializer();
            return result;
        }
    }
}