using log4net;
using NUnit.Framework;
using PoeShared.Modularity;
using Shouldly;

namespace PoeShared.Tests.Modularity
{
    [TestFixture]
    public class ConfigSerializerTests
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ConfigSerializerTests));
        
        private static readonly string SerializedUnknown = @"{
  'TypeName': 'Unknown',
  'ConfigValue': {
    'Value': 'value'
  },
  'Version': null
}";
        
        private static readonly string SerializedWellKnownVersioned = @"{
  'TypeName': 'PoeShared.Tests.Modularity.ConfigSerializerTests+PoeSampleVersionedConfig, PoeShared.Tests',
  'ConfigValue': {
    'Value': 'Version#1'
  },
  'Version': 1
}";

        public sealed class PoeCombined : IPoeEyeConfig
        {
            public IPoeEyeConfig[] Configs { get; set; }
        }

        public sealed class PoeSampleConfig : IPoeEyeConfig
        {
            public string Value { get; set; }
        }

        public sealed class PoeSampleNestedConfig : IPoeEyeConfig
        {
            public IPoeEyeConfig InnerConfig { get; set; }
        }
        
        [Test]
        public void ShouldSave()
        {
            //Given
            var instance = CreateInstance();
            var value = new PoeSampleConfig {Value = "value"};

            //When
            var serializedValue = instance.Serialize(value);
            var result = instance.Deserialize<PoeSampleConfig>(serializedValue);

            //Then
            result.Value.ShouldBe(value.Value);
        }

        [Test]
        public void ShouldSaveCombined()
        {
            //Given
            var instance = CreateInstance();
            var sampleConfig = new PoeSampleConfig {Value = "value"};
            var valueToSerialize = new PoeCombined
            {
                Configs = new IPoeEyeConfig[] {sampleConfig}
            };

            //When
            var serializedValue = instance.Serialize(valueToSerialize);
            var result = instance.Deserialize<PoeCombined>(serializedValue);

            //Then
            result.Configs.Length.ShouldBe(1, () => $"Serialized value:\n{serializedValue}");
            result.Configs[0].ShouldBeOfType<PoeSampleConfig>();
            ((PoeSampleConfig) result.Configs[0]).Value.ShouldBe("value");
        }

        [Test]
        public void ShouldSaveNested()
        {
            //Given
            var instance = CreateInstance();
            var value = new PoeSampleNestedConfig {InnerConfig = new PoeSampleConfig {Value = "value"}};

            //When
            var serializedValue = instance.Serialize(value);
            var result = instance.Deserialize<PoeSampleNestedConfig>(serializedValue);

            //Then
            result.ShouldBeOfType<PoeSampleNestedConfig>();
            result.InnerConfig.ShouldBeOfType<PoeSampleConfig>();
            ((PoeSampleConfig) result.InnerConfig).Value.ShouldBe("value");
        }
        
        private JsonConfigSerializer CreateInstance()
        {
            var result = new JsonConfigSerializer();
            return result;
        }
    }
}