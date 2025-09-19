namespace PoeShared.Tests.Scaffolding;

using System;
using System.Globalization;
using System.Text.Json;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class PercentageSystemTextJsonConverterFixture : FixtureBase
{
    private static JsonSerializerOptions OptionsWithConverter()
        => new JsonSerializerOptions
        {
            // Keep default naming policy (property names preserved)
            // Add the converter under test
            Converters = { new PercentageSystemTextJsonConverter() }
        };

    private sealed class Holder
    {
        public Percentage P { get; set; }
    }

    [Test]
    public void Should_Serialize_As_Number()
    {
        //Given
        var dto = new Holder { P = new Percentage(42.5f) };
        var options = OptionsWithConverter();
        Log.Info("Prepared holder with P=42.5");

        //When
        var json = JsonSerializer.Serialize(dto, options);
        Log.Debug($"Serialized JSON: {json}");

        //Then
        json.ShouldBe("{\"P\":42.5}");
    }

    [Test]
    public void Should_Deserialize_From_Number()
    {
        //Given
        var json = "{\"P\":42.5}";
        var options = OptionsWithConverter();
        Log.Info("Deserializing from number 42.5");

        //When
        var dto = JsonSerializer.Deserialize<Holder>(json, options);

        //Then
        dto.ShouldNotBeNull();
        dto!.P.Value.ShouldBe(42.5f);
    }

    [Test]
    public void Should_Deserialize_From_Integer_Number()
    {
        //Given
        var json = "{\"P\":13}";
        var options = OptionsWithConverter();

        //When
        var dto = JsonSerializer.Deserialize<Holder>(json, options);

        //Then
        dto.ShouldNotBeNull();
        dto!.P.Value.ShouldBe(13f);
    }

    [Test]
    public void Should_Deserialize_From_String_Number()
    {
        //Given
        var json = "{\"P\":\"42.5\"}";
        var options = OptionsWithConverter();

        //When
        var dto = JsonSerializer.Deserialize<Holder>(json, options);

        //Then
        dto.ShouldNotBeNull();
        dto!.P.Value.ShouldBe(42.5f);
    }

    [Test]
    public void Should_Deserialize_From_String_WithPercent()
    {
        //Given
        var json = "{\"P\":\"42.5%\"}";
        var options = OptionsWithConverter();

        //When
        var dto = JsonSerializer.Deserialize<Holder>(json, options);

        //Then
        dto.ShouldNotBeNull();
        dto!.P.Value.ShouldBe(42.5f);
    }

    [Test]
    public void Should_Deserialize_From_Object_With_Value_Property_Number()
    {
        //Given
        var json = "{\"P\":{\"Value\":42.5}}";
        var options = OptionsWithConverter();
        Log.Info("Deserializing from object with Value: 42.5");

        //When
        var dto = JsonSerializer.Deserialize<Holder>(json, options);

        //Then
        dto.ShouldNotBeNull();
        dto!.P.Value.ShouldBe(42.5f);
    }

    [Test]
    public void Should_Deserialize_From_Object_With_Value_Property_String_WithPercent_CaseInsensitive()
    {
        //Given
        var json = "{\"P\":{\"value\":\"42.5%\"}}"; // lower-case "value"
        var options = OptionsWithConverter();

        //When
        var dto = JsonSerializer.Deserialize<Holder>(json, options);

        //Then
        dto.ShouldNotBeNull();
        dto!.P.Value.ShouldBe(42.5f);
    }

    [Test]
    public void Should_RoundTrip_Through_Serialization()
    {
        //Given
        var original = new Holder { P = new Percentage(77.7f) };
        var options = OptionsWithConverter();
        Log.Info("Round-tripping 77.7");

        //When
        var json = JsonSerializer.Serialize(original, options);
        Log.Debug($"JSON: {json}");
        var copy = JsonSerializer.Deserialize<Holder>(json, options);

        //Then
        copy.ShouldNotBeNull();
        copy!.P.Value.ShouldBe(77.7f, 0.0001);
    }

    [Test]
    public void Should_Parse_Using_Invariant_Culture_When_Current_Is_NonInvariant()
    {
        //Given
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE"); // comma decimal culture
            var json = "{\"P\":\"42.5\"}"; // dot decimal
            var options = OptionsWithConverter();
            Log.Info($"CurrentCulture set to {CultureInfo.CurrentCulture}");

            //When
            var dto = JsonSerializer.Deserialize<Holder>(json, options);

            //Then
            dto.ShouldNotBeNull();
            dto!.P.Value.ShouldBe(42.5f);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public void Should_Throw_When_Object_Missing_Value_Property()
    {
        //Given
        var json = "{\"P\":{}}";
        var options = OptionsWithConverter();

        //When
        var ex = Should.Throw<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Holder>(json, options);
        });

        //Then
        ex.Message.ShouldContain("missing 'Value' property", Case.Insensitive);
    }

    [Test]
    public void Should_Throw_When_Value_Property_Is_Invalid_Type()
    {
        //Given
        var json = "{\"P\":{\"Value\":true}}";
        var options = OptionsWithConverter();

        //When
        var ex = Should.Throw<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Holder>(json, options);
        });

        //Then
        ex.Message.ShouldContain("Unsupported 'Value' property type", Case.Insensitive);
    }

    [Test]
    public void Should_Throw_When_String_Is_Not_Numeric()
    {
        //Given
        var json = "{\"P\":\"not-a-number%\"}";
        var options = OptionsWithConverter();

        //When
        var ex = Should.Throw<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Holder>(json, options);
        });

        //Then
        ex.Message.ShouldContain("Invalid string for Percentage", Case.Insensitive);
    }
}
