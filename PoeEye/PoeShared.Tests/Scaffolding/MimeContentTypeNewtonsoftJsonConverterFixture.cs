using PoeShared.Modularity;

namespace PoeShared.Tests.Scaffolding;

using System;
using System.Globalization;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;

public class MimeContentTypeNewtonsoftJsonConverterFixture : FixtureBase
{
    private static JsonSerializerSettings SettingsWithConverter() => new JsonSerializerSettings
    {
        Converters = { new MimeContentTypeNewtonsoftJsonConverter() },
        Culture = CultureInfo.InvariantCulture
    };

    private sealed class Holder
    {
        public MimeContentType Ct { get; set; }
    }

    [Test]
    public void Should_Serialize_As_String()
    {
        //Given
        var holder = new Holder { Ct = new MimeContentType("text/html") };
        var settings = SettingsWithConverter();
        Log.Info("Prepared holder with Ct=text/html");

        //When
        var json = JsonConvert.SerializeObject(holder, settings);
        Log.Debug($"Serialized JSON: {json}");

        //Then
        json.ShouldBe("{\"Ct\":\"text/html\"}");
    }

    [Test]
    public void Should_Deserialize_From_String()
    {
        //Given
        var json = "{\"Ct\":\"application/json\"}";
        var settings = SettingsWithConverter();
        Log.Info("Deserializing from string application/json");

        //When
        var holder = JsonConvert.DeserializeObject<Holder>(json, settings);

        //Then
        holder.ShouldNotBeNull();
        holder!.Ct.MediaType.ShouldBe("application/json");
    }

    [Test]
    public void Should_Deserialize_From_Object_With_MediaType()
    {
        //Given
        var json = "{\"Ct\":{\"MediaType\":\"image/png\"}}";
        var settings = SettingsWithConverter();
        Log.Info("Deserializing from object with MediaType=image/png");

        //When
        var holder = JsonConvert.DeserializeObject<Holder>(json, settings);

        //Then
        holder.ShouldNotBeNull();
        holder!.Ct.MediaType.ShouldBe("image/png");
    }

    [Test]
    public void Should_Deserialize_From_Object_With_Value_CaseInsensitive()
    {
        //Given
        var json = "{\"Ct\":{\"value\":\"text/plain\"}}"; // lower-case 'value'
        var settings = SettingsWithConverter();

        //When
        var holder = JsonConvert.DeserializeObject<Holder>(json, settings);

        //Then
        holder.ShouldNotBeNull();
        holder!.Ct.MediaType.ShouldBe("text/plain");
    }

    [Test]
    public void Should_RoundTrip_Through_Serialization()
    {
        //Given
        var original = new Holder { Ct = new MimeContentType("application/xml") };
        var settings = SettingsWithConverter();
        Log.Info("Round-tripping application/xml");

        //When
        var json = JsonConvert.SerializeObject(original, settings);
        Log.Debug($"JSON: {json}");
        var copy = JsonConvert.DeserializeObject<Holder>(json, settings);

        //Then
        copy.ShouldNotBeNull();
        copy!.Ct.MediaType.ShouldBe("application/xml");
    }

    [Test]
    public void Should_Throw_When_Object_Missing_MediaType_Or_Value()
    {
        //Given
        var json = "{\"Ct\":{}}";
        var settings = SettingsWithConverter();

        //When
        var ex = Should.Throw<JsonSerializationException>(() =>
        {
            JsonConvert.DeserializeObject<Holder>(json, settings);
        });

        //Then
        ex.Message.ShouldContain("missing 'MediaType' (or 'Value')", Case.Insensitive);
    }

    [Test]
    public void Should_Throw_When_MediaType_Is_Not_String()
    {
        //Given
        var json = "{\"Ct\":{\"MediaType\":123}}";
        var settings = SettingsWithConverter();

        //When
        var ex = Should.Throw<JsonSerializationException>(() =>
        {
            JsonConvert.DeserializeObject<Holder>(json, settings);
        });

        //Then
        ex.Message.ShouldContain("must be a string", Case.Insensitive);
    }

    [Test]
    public void Should_Throw_When_String_Is_Null_Or_Empty()
    {
        //Given
        var settings = SettingsWithConverter();

        //When
        var ex1 = Should.Throw<JsonSerializationException>(() =>
        {
            JsonConvert.DeserializeObject<Holder>("{\"Ct\":\"\"}", settings);
        });

        var ex2 = Should.Throw<JsonSerializationException>(() =>
        {
            JsonConvert.DeserializeObject<Holder>("{\"Ct\":\"   \"}", settings);
        });

        //Then
        ex1.Message.ShouldContain("cannot be null or empty", Case.Insensitive);
        ex2.Message.ShouldContain("cannot be null or empty", Case.Insensitive);
    }
}