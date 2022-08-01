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
    public void ShouldProtectSecureClass()
    {
        //Given
        var settings = CreateInstance();

        //When
        var serialized = JsonConvert.SerializeObject(new SecureContainerClass()
        {
            Container = new ContainerClass()
            {
                SafeObject = new TestClass("a")
            }
        }, settings);

        //Then
        JsonConvert.DeserializeObject<SecureContainerClass>(serialized, settings).Container.SafeObject.Value.ShouldBe("a");
    }
    
    [Test]
    public void ShouldUnprotectContainer()
    {
        //Given
        var settings = CreateInstance();

        //When
        var serialized = "{ 'SafeString': 'a' }";

        //Then
        JsonConvert.DeserializeObject<ContainerClass>(serialized, settings).SafeString.ShouldBe("a");
    }

    [Test]
    public void ShouldProtectInherited()
    {
        //Given
        var settings = CreateInstance();


        //When
        var serialized = JsonConvert.SerializeObject(new ShareAuraSubscriptionConfig()
        {
            UserName = "test".ToSecuredString(),
            AuraTree = new TestClass("a"),
            ShareId = "b"
        }, settings);

        //Then
        var result = JsonConvert.DeserializeObject<ShareAuraSubscriptionConfig>(serialized, settings);
        result.ShareId.ShouldBe("b");
        result.UserName.ToUnsecuredString().ShouldBe("test");
        result.AuraTree.Value.ShouldBe("a");
    } 
    
    [Test]
    public void ShouldThrowOnCircularReference()
    {
        //Given
        var settings = CreateInstance();


        //When
        var serializeAction = () => JsonConvert.SerializeObject(new SecuredContainerClass()
        {
            SafeSecureString = "a".ToSecuredString()
        }, settings);

        //Then
        serializeAction.ShouldThrow<JsonSerializationException>();
    }

    private JsonSerializerSettings CreateInstance()
    {
        var result = new JsonSerializerSettings();
        return result;
    }
    
    [JsonConverter(typeof(SafeDataConverter))]
    private record SecuredContainerClass : ContainerClass
    {
    }

    private record SecureContainerClass
    {
        [JsonConverter(typeof(SafeDataConverter))]
        public ContainerClass Container { get; set; }
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
    
    private sealed record ShareAuraSubscriptionConfig : AuraFolderSubscriptionConfig
    {
        public string ShareId { get; set; }
        [JsonConverter(typeof(SafeDataConverter))] public SecureString UserName { get; set; }
        public override bool IsValid => !string.IsNullOrEmpty(ShareId);
        public override int Version { get; set; } = 1;
    }
    
    private abstract record AuraFolderSubscriptionConfig 
    {
        [JsonConverter(typeof(SafeDataConverter))]
        public TestClass AuraTree { get; set; }

        [JsonIgnore]
        public abstract bool IsValid { get; }
    
        public abstract int Version { get; set; }
    }
}