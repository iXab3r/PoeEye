using System;
using System.Linq;
using System.Net.Mime;
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

        var data = Enumerable.Range(0, dataLength).Select(x => (byte) x).ToArray();
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

        var data = Enumerable.Range(0, dataLength).Select(x => (byte) x).ToArray();
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

        var data = Enumerable.Range(0, dataLength).Select(x => (byte) x).ToArray();
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

        var data = Enumerable.Range(0, dataLength).Select(x => (byte) x).ToArray();
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

        var data = Enumerable.Range(0, dataLength).Select(x => (byte) x).ToArray();
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

    [Test]
    public void ShouldHandleInvalidBase64InDataField()
    {
        // Given
        var instance = CreateInstance();
        var serialized = "{ \"Data\": \"InvalidBase64==\" }"; // Invalid Base64 string

        // When
        var action = () => Deserialize<BinaryResourceReference>(serialized, instance);

        // Then
        action.ShouldThrow<FormatException>(); // Expecting a format exception due to invalid Base64
    }

    [Test]
    public void ShouldHandleMissingUriField()
    {
        // Given
        var instance = CreateInstance();
        var serialized = $$"""
                           {
                             "SHA1": "someSHA1",
                             "Data": "VGhpcyBpcyBzb21lIGRhdGE=", // Sample base64 data
                             "FileName": "sample.txt",
                             "ContentType": "text/plain",
                             "ContentLength": 123,
                             "LastModified": "2021-01-01T12:00:00Z"
                           }
                           """;

        // When
        var result = Deserialize<BinaryResourceReference>(serialized, instance);

        // Then
        result.Uri.ShouldBe(null);
        result.SHA1.ShouldBe("someSHA1");
        result.Data.ShouldNotBeNull();
        result.FileName.ShouldBe("sample.txt");
        result.ContentType.ToString().ShouldBe("text/plain");
        result.ContentLength.ShouldBe(123);
        result.LastModified.ShouldBe(DateTimeOffset.Parse("2021-01-01T12:00:00Z"));
    }


    [Test]
    public void ShouldHandleMissingSHA1Field()
    {
        // Given
        var instance = CreateInstance();
        var serialized = $$"""
                           {
                             "Uri": "someUri",
                             "Data": "VGhpcyBpcyBzb21lIGRhdGE=", // Sample base64 data
                             "FileName": "sample.txt",
                             "ContentType": "text/plain",
                             "ContentLength": 123,
                             "LastModified": "2021-01-01T12:00:00Z"
                           }
                           """;

        // When
        var result = Deserialize<BinaryResourceReference>(serialized, instance);

        // Then
        result.Uri.ShouldBe("someUri");
        result.SHA1.ShouldBe(null);
        result.Data.ShouldNotBeNull();
        result.FileName.ShouldBe("sample.txt");
        result.ContentType.ToString().ShouldBe("text/plain");
        result.ContentLength.ShouldBe(123);
        result.LastModified.ShouldBe(DateTimeOffset.Parse("2021-01-01T12:00:00Z"));
    }

    [Test]
    public void ShouldHandleMissingContentTypeField()
    {
        // Given
        var instance = CreateInstance();
        var serialized = $$"""
                           {
                             "Uri": "someUri",
                             "SHA1": "someSHA1",
                             "Data": "VGhpcyBpcyBzb21lIGRhdGE=", 
                             "FileName": "sample.txt",
                             "ContentLength": 123,
                             "LastModified": "2021-01-01T12:00:00Z"
                           }
                           """;

        // When
        var result = Deserialize<BinaryResourceReference>(serialized, instance);

        // Then
        result.Uri.ShouldBe("someUri");
        result.SHA1.ShouldBe("someSHA1");
        result.Data.ShouldNotBeNull();
        result.FileName.ShouldBe("sample.txt");
        result.ContentType.ShouldBe(null);
        result.ContentLength.ShouldBe(123);
        result.LastModified.ShouldBe(DateTimeOffset.Parse("2021-01-01T12:00:00Z"));
    }
    
    [Test]
    public void ShouldSerializeWithAllFields()
    {
        // Given
        var instance = CreateInstance();
        var resource = new BinaryResourceReference
        {
            Uri = "someUri",
            SHA1 = "someSHA1",
            Data = Convert.FromBase64String("VGhpcyBpcyBzb21lIGRhdGE="), // Sample base64 data
            FileName = "sample.txt",
            ContentType = new ContentType("text/plain"),
            ContentLength = 123,
            LastModified = DateTimeOffset.Parse("2021-01-01T12:00:00Z")
        };

        // When
        var serialized = Serialize(resource, instance);
        var jsonObject = JObject.Parse(serialized);

        // Then
        jsonObject["Uri"].ShouldBe("someUri");
        jsonObject["SHA1"].ShouldBe("someSHA1");
        jsonObject["Data"].ShouldBe("VGhpcyBpcyBzb21lIGRhdGE=");
        jsonObject["FileName"].ShouldBe("sample.txt");
        jsonObject["ContentType"].ShouldBe("text/plain");
        jsonObject["ContentLength"].ShouldBe(123);
        jsonObject["LastModified"].ShouldBe(DateTimeOffset.Parse("2021-01-01T12:00:00Z"));
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
        var result = new JsonConfigSerializer();
        result.RegisterConverter(new BinaryResourceReferenceConverter());
        return result;
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

        public BinaryResourceReference ByteArray { get; set; }
    }
}