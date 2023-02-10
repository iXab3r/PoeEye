using AutoFixture;
using DynamicData;
using NUnit.Framework;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class HierarchicalSourceCacheFixture : FixtureBase
{
    [Test]
    public void ShouldAdd()
    {
        //Given
        var cache = CreateInstance();

        //When
        cache.AddOrUpdate(new Pair("key", "value"));

        //Then
        cache.Lookup("key").Value.Value.ShouldBe("value");
        cache.Count.ShouldBe(1);
    }

    [Test]
    public void ShouldUpdate()
    {
        //Given
        var cache = CreateInstance();
        cache.AddOrUpdate(new Pair("key", "value"));

        //When
        cache.AddOrUpdate(new Pair("key", "updated"));

        //Then
        cache.Lookup("key").Value.Value.ShouldBe("updated");
        cache.Count.ShouldBe(1);
    }

    [Test]
    public void ShouldRemove()
    {
        //Given
        var cache = CreateInstance();
        cache.AddOrUpdate(new Pair("key", "value"));

        //When
        cache.RemoveKey("key");

        //Then
        cache.Lookup("key").HasValue.ShouldBeFalse();
        cache.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldClear()
    {
        //Given
        var cache = CreateInstance();
        cache.AddOrUpdate(new Pair("key", "value"));

        //When
        cache.Clear();

        //Then
        cache.Lookup("key").HasValue.ShouldBeFalse();
        cache.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldProcessParentAdd()
    {
        //Given
        var cache = CreateInstance();
        var parentCache = CreateInstance();
        cache.Parent = parentCache;

        //When
        parentCache.AddOrUpdate(new Pair("key", "value"));

        //Then
        cache.Lookup("key").Value.Value.ShouldBe("value");
        cache.Count.ShouldBe(1);
    }

    [Test]
    public void ShouldProcessParentUpdate()
    {
        //Given
        var cache = CreateInstance();
        var parentCache = CreateInstance();
        cache.Parent = parentCache;
        parentCache.AddOrUpdate(new Pair("key", "value"));

        //When
        parentCache.AddOrUpdate(new Pair("key", "updated"));

        //Then
        cache.Lookup("key").Value.Value.ShouldBe("updated");
        cache.Count.ShouldBe(1);
    }
    
    [Test]
    public void ShouldProcessParentRemove()
    {
        //Given
        var cache = CreateInstance();
        var parentCache = CreateInstance();
        cache.Parent = parentCache;
        parentCache.AddOrUpdate(new Pair("key", "value"));

        //When
        parentCache.RemoveKey("key");

        //Then
        cache.Lookup("key").HasValue.ShouldBeFalse();
        cache.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldCalculateEffectiveAdd()
    {
        //Given
        var cache = CreateInstance();
        var parentCache = CreateInstance();
        cache.Parent = parentCache;
        parentCache.AddOrUpdate(new Pair("key", "parentValue"));
        cache.Lookup("key").Value.Value.ShouldBe("parentValue");

        //When
        cache.AddOrUpdate(new Pair("key", "value"));

        //Then
        cache.Lookup("key").Value.Value.ShouldBe("value");
        cache.Count.ShouldBe(1);
    }
    
    [Test]
    public void ShouldCalculateEffectiveAddReverse()
    {
        //Given
        var cache = CreateInstance();
        var parentCache = CreateInstance();
        cache.Parent = parentCache;
        cache.AddOrUpdate(new Pair("key", "value"));
        cache.Lookup("key").Value.Value.ShouldBe("value");

        //When
        parentCache.AddOrUpdate(new Pair("key", "parentValue"));

        //Then
        cache.Lookup("key").Value.Value.ShouldBe("value");
        cache.Count.ShouldBe(1);
    }
    
    [Test]
    public void ShouldCalculateEffectiveOnRemoval()
    {
        //Given
        var cache = CreateInstance();
        var parentCache = CreateInstance();
        cache.Parent = parentCache;
        cache.AddOrUpdate(new Pair("key", "value"));
        parentCache.AddOrUpdate(new Pair("key", "parentValue"));
        cache.Lookup("key").Value.Value.ShouldBe("value");

        //When
        cache.RemoveKey("key");

        //Then
        cache.Lookup("key").Value.Value.ShouldBe("parentValue");
        cache.Count.ShouldBe(1);
    }
    
    [Test]
    public void ShouldCalculateEffectiveOnRemovalFromParent()
    {
        //Given
        var cache = CreateInstance();
        var parentCache = CreateInstance();
        cache.Parent = parentCache;
        cache.AddOrUpdate(new Pair("key", "value"));
        parentCache.AddOrUpdate(new Pair("key", "parentValue"));
        cache.Lookup("key").Value.Value.ShouldBe("value");

        //When
        parentCache.RemoveKey("key");

        //Then
        cache.Lookup("key").Value.Value.ShouldBe("value");
        cache.Count.ShouldBe(1);
    }

    [Test]
    public void ShouldProcessParentRemoval()
    {
        //Given
        var cache = CreateInstance();
        var parentCache = CreateInstance();
        cache.Parent = parentCache;
        parentCache.AddOrUpdate(new Pair("key", "parentValue"));
        cache.Lookup("key").Value.Value.ShouldBe("parentValue");

        //When
        cache.Parent = null;

        //Then
        cache.Lookup("key").HasValue.ShouldBeFalse();
        cache.Count.ShouldBe(0);
    }


    [Test]
    public void ShouldProcessParentRemovalWhenLocalValueIsPresent()
    {
        //Given
        var cache = CreateInstance();
        var parentCache = CreateInstance();
        cache.Parent = parentCache;
        parentCache.AddOrUpdate(new Pair("key", "parentValue"));
        cache.AddOrUpdate(new Pair("key", "value"));
        cache.Lookup("key").Value.Value.ShouldBe("value");

        //When
        cache.Parent = null;

        //Then
        cache.Lookup("key").Value.Value.ShouldBe("value");
        cache.Count.ShouldBe(1);
    }
    
    private sealed record Pair(string Key, string Value);

    private HierarchicalSourceCache<Pair, string> CreateInstance()
    {
        return new HierarchicalSourceCache<Pair, string>(x => x.Key);
    }
}