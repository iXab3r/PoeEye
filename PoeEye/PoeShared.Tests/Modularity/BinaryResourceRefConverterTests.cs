using System;
using System.Linq;
using System.Net.Mime;
using Newtonsoft.Json.Linq;
using PoeShared.Modularity;

namespace PoeShared.Tests.Modularity;

[TestFixture]
public class BinaryResourceRefConverterTests : FixtureBase
{
    [Test]
    public void ShouldCreate()
    {
        //Given
        //When
        var action = () => new BinaryResourceRefConverter();

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
        json[nameof(container.ByteArray)].ShouldBe(base64);
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
            ByteArray = new BinaryResourceRef()
            {
                Data = data
            }
        };

        //When
        var serialized = Serialize(container, instance);

        //Then
        serialized.ShouldNotBeEmpty();
        var json = JToken.Parse(serialized);
        json[nameof(container.ByteArray)].ShouldBe(base64);
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
            ByteArray = new BinaryResourceRef()
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
        json[nameof(container.ByteArray)][nameof(BinaryResourceRef.Data)].ShouldBe(base64);
        json[nameof(container.ByteArray)][nameof(BinaryResourceRef.Uri)].ShouldBe(container.ByteArray.Uri);
        json[nameof(container.ByteArray)][nameof(BinaryResourceRef.IsMaterialized)].ShouldBeNull();
        json[nameof(container.ByteArray)][nameof(BinaryResourceRef.HasMetadata)].ShouldBeNull();
    }

    [Test]
    public void ShouldSerializeBinaryDataWithoutDataToObject()
    {
        //Given
        var instance = CreateInstance();

        var container = new TestBinaryClass()
        {
            ByteArray = new BinaryResourceRef()
            {
                Uri = "file"
            }
        };

        //When
        var serialized = Serialize(container, instance);

        //Then
        serialized.ShouldNotBeEmpty();
        var json = JToken.Parse(serialized);
        json[nameof(container.ByteArray)][nameof(BinaryResourceRef.Uri)].ShouldBe(container.ByteArray.Uri);
        json[nameof(container.ByteArray)][nameof(BinaryResourceRef.IsMaterialized)].ShouldBeNull();
        json[nameof(container.ByteArray)][nameof(BinaryResourceRef.HasMetadata)].ShouldBeNull();
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
        container.ByteArray.IsMaterialized.ShouldBe(true);
        container.ByteArray.IsValid.ShouldBe(true);
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
        container.ByteArray.IsMaterialized.ShouldBe(false);
        container.ByteArray.IsValid.ShouldBe(true);
    }

    [Test]
    public void ShouldHandleInvalidBase64InDataField()
    {
        // Given
        var instance = CreateInstance();
        var serialized = "{ \"Data\": \"InvalidBase64==\" }"; // Invalid Base64 string

        // When
        Action action = () => Deserialize<BinaryResourceRef>(serialized, instance);

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
                             "Hash": "someSHA1",
                             "Data": "VGhpcyBpcyBzb21lIGRhdGE=", // Sample base64 data
                             "FileName": "sample.txt",
                             "ContentType": "text/plain",
                             "ContentLength": 123,
                             "LastModified": "2021-01-01T12:00:00Z",
                             "SupportsMaterialization": "true"
                           }
                           """;

        // When
        var result = Deserialize<BinaryResourceRef>(serialized, instance);

        // Then
        result.Uri.ShouldBe(null);
        result.Hash.ShouldBe("someSHA1");
        result.Data.ShouldNotBeNull();
        result.FileName.ShouldBe("sample.txt");
        result.ContentType.ToString().ShouldBe("text/plain");
        result.ContentLength.ShouldBe(123);
        result.LastModified.ShouldBe(DateTimeOffset.Parse("2021-01-01T12:00:00Z"));
        result.IsMaterialized.ShouldBe(true);
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
                             "LastModified": "2021-01-01T12:00:00Z",
                             "SupportsMaterialization": "false"
                           }
                           """;

        // When
        var result = Deserialize<BinaryResourceRef>(serialized, instance);

        // Then
        result.Uri.ShouldBe("someUri");
        result.Hash.ShouldBe(null);
        result.Data.ShouldNotBeNull();
        result.FileName.ShouldBe("sample.txt");
        result.ContentType.ToString().ShouldBe("text/plain");
        result.ContentLength.ShouldBe(123);
        result.LastModified.ShouldBe(DateTimeOffset.Parse("2021-01-01T12:00:00Z"));
        result.IsMaterialized.ShouldBe(true);
    }

    [Test]
    public void ShouldHandleMissingContentTypeField()
    {
        // Given
        var instance = CreateInstance();
        var serialized = $$"""
                           {
                             "Uri": "someUri",
                             "Hash": "someSHA1",
                             "Data": "VGhpcyBpcyBzb21lIGRhdGE=", 
                             "FileName": "sample.txt",
                             "ContentLength": 123,
                             "LastModified": "2021-01-01T12:00:00Z"
                           }
                           """;

        // When
        var result = Deserialize<BinaryResourceRef>(serialized, instance);

        // Then
        result.Uri.ShouldBe("someUri");
        result.Hash.ShouldBe("someSHA1");
        result.Data.ShouldNotBeNull();
        result.FileName.ShouldBe("sample.txt");
        result.ContentType.ShouldBe(null);
        result.ContentLength.ShouldBe(123);
        result.LastModified.ShouldBe(DateTimeOffset.Parse("2021-01-01T12:00:00Z"));
        result.IsMaterialized.ShouldBe(true);
    }
    
    [Test]
    public void ShouldSerializeWithAllFields()
    {
        // Given
        var instance = CreateInstance();
        var resource = new BinaryResourceRef
        {
            Uri = "someUri",
            Hash = "someSHA1",
            Data = Convert.FromBase64String("VGhpcyBpcyBzb21lIGRhdGE="), // Sample base64 data
            FileName = "sample.txt",
            ContentType = new MimeContentType("text/plain"),
            ContentLength = 123,
            LastModified = DateTimeOffset.Parse("2021-01-01T12:00:00Z")
        };

        // When
        var serialized = Serialize(resource, instance);
        var jsonObject = JObject.Parse(serialized);

        // Then
        jsonObject[nameof(BinaryResourceRef.Uri)].ShouldBe("someUri");
        jsonObject[nameof(BinaryResourceRef.Hash)].ShouldBe("someSHA1");
        jsonObject[nameof(BinaryResourceRef.Data)].ShouldBe("VGhpcyBpcyBzb21lIGRhdGE=");
        jsonObject[nameof(BinaryResourceRef.FileName)].ShouldBe("sample.txt");
        jsonObject[nameof(BinaryResourceRef.ContentType)].ShouldBe("text/plain");
        jsonObject[nameof(BinaryResourceRef.ContentLength)].ShouldBe(123);
        jsonObject[nameof(BinaryResourceRef.LastModified)].ShouldBe(DateTimeOffset.Parse("2021-01-01T12:00:00Z"));
    }

    [Test]
    public void ShouldSerializeEmpty()
    {
        //Given
        var instance = CreateInstance();
        var resource = BinaryResourceRef.Empty;

        //When
        var serialized = Serialize(resource, instance);

        //Then
        serialized.ShouldBe("\"\"");

    }
    
    [Test]
    public void ShouldDeserializeEmpty()
    {
        //Given
        var instance = CreateInstance();
        var serialized = "\"\"";

        //When
        var result = Deserialize<BinaryResourceRef>(serialized, instance);

        //Then
        result.ShouldBe(BinaryResourceRef.Empty);
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
        result.RegisterConverter(new BinaryResourceRefConverter());
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

        public BinaryResourceRef ByteArray { get; set; }
    }
}