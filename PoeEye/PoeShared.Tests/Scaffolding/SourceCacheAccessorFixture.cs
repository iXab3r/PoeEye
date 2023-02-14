using System;
using System.Linq;
using DynamicData;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

[TestFixture(typeof(KvpClass))]
[TestFixture(typeof(KvpStruct))]
internal class SourceCacheAccessorFixture<TObject> : FixtureBase where TObject : IKvp, new()
{
    [Test]
    public void ShouldCreate()
    {
        //Given

        //When
        Action action = () => CreateInstance("test");

        //Then
        action.ShouldNotThrow();
    }

    [Test]
    public void ShouldAddIfEnabled()
    {
        //Given
        var instance = CreateInstance("test");
        var cache = CreateCache();
        instance.Cache = cache;
        cache.Lookup("test").HasValue.ShouldBe(false);
        
        //When
        instance.IsEnabled = true;
        instance.IsEnabled.ShouldBe(true);
        var newValue = new TObject() {Key = "test", Value = 2};
        instance.Value = newValue;

        //Then
        cache.Lookup("test").Value.ShouldBe(newValue);
    }

    [Test]
    public void ShouldNotUpdateCacheWhenAssigned()
    {
        //Given
        var instance = CreateInstance("test");
        var cache = CreateCache();
        var newValue = new TObject() {Key = "test", Value = 2};
        cache.AddOrUpdate(newValue);

        var changes = cache.Connect().Watch("test").Listen().Skip(1);
        instance.Cache = cache;

        //When
        instance.Value.ShouldBe(newValue);

        //Then
        changes.CollectionSequenceShouldBe();
    }

    [Test]
    public void ShouldTrackKey()
    {
        //Given
        var instance = CreateInstance("test");
        var cache = CreateCache();
        instance.Cache = cache;
        instance.IsEnabled = true;
        var newValue = new TObject() {Key = "test", Value = 2};

        //When
        cache.AddOrUpdate(newValue);

        //Then
        instance.Value.ShouldBe(newValue);
    }

    [Test]
    public void ShouldUpdateIfEnabled()
    {
        //Given
        var instance = CreateInstance("test");
        var cache = CreateCache();

        instance.Cache = cache;
        var oldValue = new TObject {Key = "test", Value = 2};
        cache.AddOrUpdate(oldValue);
        instance.Value.ShouldBe(oldValue);

        //When
        var newValue = new TObject {Key = "test", Value = 3};
        cache.AddOrUpdate(newValue);

        //Then
        instance.Value.ShouldBe(newValue);
    }

    [Test]
    public void ShouldUpdateCacheIfChanged()
    {
        //Given
        var instance = CreateInstance("test", CreateCache());
        instance.Value.ShouldBe(default);
        instance.Cache.Count.ShouldBe(0);

        //When
        var newValue = new TObject {Key = "test", Value = 3};
        instance.Value = newValue;

        //Then
        instance.Cache.Count.ShouldBe(1);
        instance.Cache.Lookup("test").ShouldBe(newValue);
    }
    
    [Test]
    public void ShouldNotUpdateCacheIfWrongKey()
    {
        //Given
        var instance = CreateInstance("test", CreateCache());
        instance.Value.ShouldBe(default);
        instance.Cache.Count.ShouldBe(0);

        //When
        var newValue = new TObject {Key = "abcd", Value = 3};
        Action action = () => instance.Value = newValue;

        //Then
        action.ShouldThrow<ArgumentException>();
        instance.Cache.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldRemoveIfDisabled()
    {
        //Given
        var instance = CreateInstance("test");
        var cache = CreateCache();
        cache.AddOrUpdate(new TObject(){ Key = "test", Value = 2});
        
        instance.Cache = cache;
        instance.IsEnabled.ShouldBe(true);
        cache.Lookup("test").HasValue.ShouldBe(true);

        //When
        instance.IsEnabled = false;

        //Then
        cache.Lookup("test").HasValue.ShouldBe(false);
        instance.Value.ShouldBe(default);
    }

    [Test]
    public void ShouldRemoveIfAssignedDefault()
    {
        //Given
        var instance = CreateInstance("test");
        var cache = CreateCache();
        var newValue = new TObject() {Key = "test", Value = 2};
        cache.AddOrUpdate(newValue);
        instance.Cache = cache;
        instance.IsEnabled.ShouldBe(true);
        cache.Lookup("test").HasValue.ShouldBe(true);
        instance.Value.ShouldBe(newValue);
        
        //When
        var badValue = new TObject() {Key = null};
        instance.Value = badValue;

        //Then
        instance.Value.ShouldBe(badValue);
        instance.IsEnabled.ShouldBe(false);
    }

    [Test]
    public void ShouldThrowIfAssigningValueWithoutCache()
    {
        //Given
        var instance = CreateInstance("test");
        instance.IsEnabled = true;

        //When
        Action action = () => instance.Value = new TObject(){ Key = "test", Value = 2};

        //Then  
        action.ShouldThrow<InvalidOperationException>();
    }

    [Test]
    public void ShouldTrackChangesIfDisabled()
    {
        //Given
        var instance = CreateInstance("test");
        var cache = CreateCache();
        instance.Cache = cache;
        instance.IsEnabled = true;
        instance.Value = new TObject() { Key = "test", Value = 2};
        instance.IsEnabled = false;
        instance.Value.ShouldBe(default);
        cache.Count.ShouldBe(0);
        
        //When
        var newValue = new TObject() {Key = "test", Value = 3};
        cache.AddOrUpdate(newValue);
        
        //Then
        instance.Value.ShouldBe(newValue);
        instance.IsEnabled.ShouldBeTrue();
        cache.Count.ShouldBe(1);
    }

    [Test]
    public void ShouldEnableIfValueIsAssigned()
    {
        //Given
        var instance = CreateInstance("test");
        var cache = CreateCache();
        instance.Cache = cache;
        instance.IsEnabled.ShouldBe(false);
        instance.Value.ShouldBe(default);
        instance.Cache.Count.ShouldBe(0);
        
        //When
        var newValue = new TObject() { Key = "test", Value = 1};
        instance.Value = newValue;

        //Then
        instance.IsEnabled.ShouldBe(true);
        instance.Value.ShouldBe(newValue);
        instance.Cache.Lookup("test").Value.ShouldBe(newValue);
    }
    
    [Test]
    public void ShouldTrackRemoval()
    {
        //Given
        var instance = CreateInstance("test");
        var cache = CreateCache();
        instance.Cache = cache;
        var newValue = new TObject() { Key = "test", Value = 1};
        cache.AddOrUpdate(newValue);
        instance.Value.ShouldBe(newValue);

        //When
        cache.RemoveKey("test");

        //Then
        instance.IsEnabled.ShouldBe(false);
        instance.Value.ShouldBe(default);
    }

    [Test]
    public void ShouldTrackCacheChange()
    {
        //Given
        var instance = CreateInstance("test");
        var cache1 = CreateCache();
        var cache2 = CreateCache();

        instance.Cache = cache1;
        var newValue = new TObject() { Key = "test", Value = 1};
        instance.Value = newValue;
        cache1.Lookup("test").Value.ShouldBe(newValue);
        
        //When
        instance.Cache = cache2;

        //Then
        cache1.Lookup("test").Value.ShouldBe(newValue);
        cache2.Count.ShouldBe(0);
        instance.IsEnabled.ShouldBe(false);
        instance.Value.ShouldBe(default);
    }
    
    [Test]
    public void ShouldTrackCacheUnassignment()
    {
        //Given
        var instance = CreateInstance("test");
        var cache1 = CreateCache();

        instance.Cache = cache1;
        
        //When
        instance.Cache = null;

        //Then
        instance.IsEnabled.ShouldBe(false);
        instance.Value.ShouldBe(default);
    }

    [Test]
    public void ShouldNotWriteToCache2()
    {
        //Given
        var instance = CreateInstance("test");
        var cache1 = CreateCache();
        var cache2 = CreateCache();

        var value1 = new TObject() { Key = "test", Value = 1};
        cache1.AddOrUpdate(value1);
        var value2 = new TObject() { Key = "test", Value = 2};
        cache2.AddOrUpdate(value2);
        
        instance.Cache = cache1;
        instance.Value.ShouldBe(value1);
        cache1.Lookup("test").Value.ShouldBe(value1);
        cache2.Lookup("test").Value.ShouldBe(value2);

        //When
        instance.Cache = cache2;

        //Then
        instance.Value.ShouldBe(value2);
        cache1.Lookup("test").Value.ShouldBe(value1);
        cache2.Lookup("test").Value.ShouldBe(value2);
    }

    private ISourceCache<TObject, string> CreateCache()
    {
        return new SourceCache<TObject, string>(x => x.Key);
    }

    private SourceCacheAccessor<TObject, string> CreateInstance(string key, ISourceCache<TObject, string> cache)
    {
        var result = CreateInstance(key);
        result.Cache = cache;
        result.IsEnabled = true;
        return result;
    }
    
    private SourceCacheAccessor<TObject, string> CreateInstance(string key)
    {
        return new SourceCacheAccessor<TObject, string>(key);
    }
}

public interface IKvp
{
    public string Key { get; init; }
        
    public int Value { get; init; }
}

public sealed record KvpClass : IKvp
{
    public string Key { get; init; }
        
    public int Value { get; init; }
}
    
public readonly struct KvpStruct : IKvp
{
    public string Key { get; init; }
        
    public int Value { get; init; }
}