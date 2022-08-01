using System;
using System.Security;
using AutoFixture;
using Newtonsoft.Json;
using NUnit.Framework;
using PoeShared.Converters;

namespace PoeShared.Tests.Native;

[TestFixture]
public class SafeDataConverterTests : FixtureBase
{
    [Test]
    public void ShouldProtectSecureString()
    {
        //Given
        var settings = CreateInstance();

        //When
        var serialized = JsonConvert.SerializeObject(new ContainerClass()
        {
            SafeSecureString = "a".ToSecuredString()
        }, settings);

        //Then
        JsonConvert.DeserializeObject<ContainerClass>(serialized, settings).SafeSecureString.ToUnsecuredString().ShouldBe("a");
    }
    
    [Test]
    public void ShouldProtectString()
    {
        //Given
        var settings = CreateInstance();

        //When
        var serialized = JsonConvert.SerializeObject(new ContainerClass()
        {
           SafeString = "a"
        }, settings);

        //Then
        JsonConvert.DeserializeObject<ContainerClass>(serialized, settings).SafeString.ShouldBe("a");
    }
    
    [Test]
    public void ShouldProtectInteger()
    {
        //Given
        var settings = CreateInstance();

        //When
        var serialized = JsonConvert.SerializeObject(new ContainerClass()
        {
            SafeInt = 18
        }, settings);

        //Then
        JsonConvert.DeserializeObject<ContainerClass>(serialized, settings).SafeInt.ShouldBe(18);
    }
    
    [Test]
    public void ShouldProtectClass()
    {
        //Given
        var settings = CreateInstance();

        //When
        var serialized = JsonConvert.SerializeObject(new ContainerClass()
        {
            SafeObject = new TestClass("a")
        }, settings);

        //Then
        JsonConvert.DeserializeObject<ContainerClass>(serialized, settings).SafeObject.Value.ShouldBe("a");
    }
    
    [Test]
    public void ShouldThrowOnSelfReferencingLoop()
    {
        //Given
        var settings = CreateInstance();

        //When
        var action = () => JsonConvert.SerializeObject(new SecureContainerClass()
        {
            SafeObject = new TestClass("a")
        }, settings);

        //Then
        action.ShouldThrow<JsonSerializationException>();
    }

    private JsonSerializerSettings CreateInstance()
    {
        var result = new JsonSerializerSettings();
        return result;
    }

    [JsonConverter(typeof(SafeDataConverter))]
    private record SecureContainerClass : ContainerClass
    {
    }
    
    private record ContainerClass
    {
        [JsonConverter(typeof(SafeDataConverter))]
        public SecureString SafeSecureString { get; set; }
        
        [JsonConverter(typeof(SafeDataConverter))]
        public string SafeString { get; set; }
        
        [JsonConverter(typeof(SafeDataConverter))]
        public int SafeInt { get; set; }
        
        [JsonConverter(typeof(SafeDataConverter))]
        public TestClass SafeObject { get; set; }
    }

    private sealed record TestClass(string Value);
}