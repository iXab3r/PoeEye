using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoeShared.Modularity;

namespace PoeShared.Tests.Modularity;

[TestFixture]
public class BinaryResourceReferenceConverterTests : FixtureBase
{
    [Test]
    public void ShouldCreate()
    {
        //Given
        //When
        var action = () => new BinaryResourceReferenceConverter();

        //Then
        action.ShouldNotThrow();
    }

    [Test]
    public void ShouldSerializeTestClass()
    {
        //Given
        var instance = CreateInstance();
        var container = new TestClass()
        {
            IntValue = 1,
            StringValue = "test"
        };

        //When
        var serialized = Serialize(container, instance);

        //Then
        serialized.ShouldNotBeEmpty();
    }
    
    [Test]
    public void ShouldDeserializeTestClass()
    {
        //Given
        var instance = CreateInstance();
        var serialized = """
                         {
                           "IntValue": 1,
                           "StringValue": "test"
                         }
                         """;

        //When
        var container = Deserialize<TestClass>(serialized, instance);

        //Then
        container.ShouldNotBeNull();
        container.IntValue.ShouldBe(1);
        container.StringValue.ShouldBe("test");
    }
    
    [Test]
    [TestCase(1)]
    [TestCase(10)]
    [TestCase(10000)]
    [TestCase(1_000_000)]
    [TestCase(10_000_000)]
    public void ShouldSerializeByteArray(int dataLength)
    {
        //Given
        var instance = CreateInstance();

        var data = Enumerable.Range(0, dataLength).Select(x => (byte)x).ToArray();
        var base64 = Convert.ToBase64String(data);
        var container = new TestClass()
        {
            ByteArray = data,
        };
        
        //When
        var serialized = Serialize(container, instance);

        //Then
        serialized.ShouldNotBeEmpty();
        var json = JToken.Parse(serialized);
        json["ByteArray"].ShouldBe(base64);
    }

    [Test]
    [TestCase(1)]
    [TestCase(10)]
    [TestCase(10000)]
    [TestCase(1_000_000)]
    [TestCase(10_000_000)]
    public void ShouldDeserializeByteArray(int dataLength)
    {
        //Given
        var instance = CreateInstance();

        var data = Enumerable.Range(0, dataLength).Select(x => (byte)x).ToArray();
        var base64 = Convert.ToBase64String(data);
        var serialized = $$"""
                           {
                             "ByteArray": "{{base64}}"
                           }
                           """;

        //When
        var container = Deserialize<TestClass>(serialized, instance);

        //Then
        container.ByteArray.ShouldBe(data);
    }

    [Test]
    [TestCase(1)]
    [TestCase(10)]
    [TestCase(10000)]
    [TestCase(1_000_000)]
    [TestCase(10_000_000)]
    public void ShouldSerializeBinaryDataToByteArray(int dataLength)
    {
        //Given
        var instance = CreateInstance();

        var data = Enumerable.Range(0, dataLength).Select(x => (byte)x).ToArray();
        var base64 = Convert.ToBase64String(data);
        var container = new TestBinaryClass()
        {
            ByteArray = new BinaryResourceReference()
            {
                Data = data
            }
        };
        
        //When
        var serialized = Serialize(container, instance);

        //Then
        serialized.ShouldNotBeEmpty();
        var json = JToken.Parse(serialized);
        json["ByteArray"].ShouldBe(base64);
    }
    
    
    [Test]
    [TestCase(1)]
    [TestCase(10)]
    public void ShouldSerializeBinaryDataToObject(int dataLength)
    {
        //Given
        var instance = CreateInstance();

        var data = Enumerable.Range(0, dataLength).Select(x => (byte)x).ToArray();
        var base64 = Convert.ToBase64String(data);
        var container = new TestBinaryClass()
        {
            ByteArray = new BinaryResourceReference()
            {
                Data = data,
                Uri = "file"
            }
        };
        
        //When
        var serialized = Serialize(container, instance);

        //Then
        serialized.ShouldNotBeEmpty();
        var json = JToken.Parse(serialized);
        json["ByteArray"]["Data"].ShouldBe(base64);
        json["ByteArray"]["Uri"].ShouldBe(container.ByteArray.Uri);
    }
    
    [Test]
    public void ShouldSerializeBinaryDataWithoutDataToObject()
    {
        //Given
        var instance = CreateInstance();

        var container = new TestBinaryClass()
        {
            ByteArray = new BinaryResourceReference()
            {
                Uri = "file"
            }
        };
        
        //When
        var serialized = Serialize(container, instance);

        //Then
        serialized.ShouldNotBeEmpty();
        var json = JToken.Parse(serialized);
        json["ByteArray"]["Uri"].ShouldBe(container.ByteArray.Uri);
    }


    [Test]
    [TestCase(1)]
    [TestCase(10)]
    [TestCase(10000)]
    [TestCase(1_000_000)]
    [TestCase(10_000_000)]
    public void ShouldDeserializeBinaryDataFromByteArray(int dataLength)
    {
        //Given
        var instance = CreateInstance();

        var data = Enumerable.Range(0, dataLength).Select(x => (byte)x).ToArray();
        var base64 = Convert.ToBase64String(data);
        var serialized = $$"""
                         {
                           "ByteArray": "{{base64}}"
                         }
                         """;

        //When
        var container = Deserialize<TestBinaryClass>(serialized, instance);

        //Then
        container.ByteArray.Data.ShouldBe(data);
    }
    
    [Test]
    public void ShouldDeserializeBinaryDataFromObject()
    {
        //Given
        var instance = CreateInstance();

        var serialized = $$"""
                           {
                             "IntValue": 0,
                             "ByteArray": {
                               "Uri": "file"
                             }
                           }
                           """;

        //When
        var container = Deserialize<TestBinaryClass>(serialized, instance);

        //Then
        container.ByteArray.Data.ShouldBe(null);
        container.ByteArray.Uri.ShouldBe("file");
    }

    private string Serialize(object container, JsonConfigSerializer serializer)
    {
        Log.Info($"Serializing container:\n{container}");
        var serialized = serializer.Serialize(container);
        Log.Info($"Serialized:\n{serialized}");
        return serialized;
    }
    
    private T Deserialize<T>(string serialized, JsonConfigSerializer serializer)
    {
        Log.Info($"Deserializing:\n{serialized}");
        var container = serializer.Deserialize<T>(serialized);
        Log.Info($"Deserialized container:\n{container}");
        return container;
    }

    private JsonConfigSerializer CreateInstance()
    {
        return new JsonConfigSerializer();
    }
    
    public sealed record TestClass
    {
        public int IntValue { get; set; }
        
        public string StringValue { get; set; }
        
        public byte[] ByteArray { get; set; }
    }
    
    public sealed record TestBinaryClass
    {
        public int IntValue { get; set; }
        
        public string StringValue { get; set; }
        
        [JsonConverter(typeof(BinaryResourceReferenceConverter))]
        public BinaryResourceReference ByteArray { get; set; }
    }
}