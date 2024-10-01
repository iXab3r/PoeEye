using System;
using DynamicData;

namespace PoeShared.Tests.DynamicData.Operators.Internal;

[TestFixture]
public class FlattenCacheChangeSetsFixture : FixtureBase
{
    [Test]
    public void ShouldFlattenCache()
    {
        //Given
        var root = new TestNode();
        
        
        //When
        var flatten = root.ChildrenCache.Connect()
            .Flatten(x => x.ChildrenCache.Connect(), x => x.Id)
            .AsObservableCache();

        //Then
        root.ChildrenCache.Count.ShouldBe(0);
        flatten.Count.ShouldBe(0);
    }
    
    [Test]
    public void ShouldFlattenCacheAdd()
    {
        //Given
        var root = new TestNode();
        var flatten = root.ChildrenCache.Connect()
            .Flatten(x => x.ChildrenCache.Connect(), x => x.Id)
            .AsObservableCache();
        
        //When
        var child1 = new TestNode();
        root.ChildrenCache.AddOrUpdate(child1);

        //Then
        root.ChildrenCache.Count.ShouldBe(1);
        flatten.Count.ShouldBe(1);
    }
    
    [Test]
    public void ShouldFlattenCacheRemove()
    {
        //Given
        var root = new TestNode();
        var flatten = root.ChildrenCache.Connect()
            .Flatten(x => x.ChildrenCache.Connect(), x => x.Id)
            .AsObservableCache();
        
        var child1 = new TestNode();
        root.ChildrenCache.AddOrUpdate(child1);
        
        //When
        root.ChildrenCache.Remove(child1);

        //Then
        root.ChildrenCache.Count.ShouldBe(0);
        flatten.Count.ShouldBe(0);
    }
    
    [Test]
    public void ShouldFlattenCacheAddToChild()
    {
        //Given
        var root = new TestNode();
        var flatten = root.ChildrenCache.Connect()
            .Flatten(x => x.ChildrenCache.Connect(), x => x.Id)
            .AsObservableCache();
        
        var child1 = new TestNode();
        root.ChildrenCache.AddOrUpdate(child1);
        
        //When
        var child2 = new TestNode();
        child1.ChildrenCache.AddOrUpdate(child2);

        //Then
        root.ChildrenCache.Count.ShouldBe(1);
        child1.ChildrenCache.Count.ShouldBe(1);
        child2.ChildrenCache.Count.ShouldBe(0);
        flatten.Count.ShouldBe(2);
    }
    
    
    [Test]
    public void ShouldFlattenCacheRemoveChild()
    {
        //Given
        var root = new TestNode();
        var flatten = root.ChildrenCache.Connect()
            .Flatten(x => x.ChildrenCache.Connect(), x => x.Id)
            .AsObservableCache();
        
        var child1 = new TestNode();
        root.ChildrenCache.AddOrUpdate(child1);
        var child2 = new TestNode();
        child1.ChildrenCache.AddOrUpdate(child2);
        
        //When
        root.ChildrenCache.Remove(child1);

        //Then
        root.ChildrenCache.Count.ShouldBe(0);
        child1.ChildrenCache.Count.ShouldBe(1);
        child2.ChildrenCache.Count.ShouldBe(0);
        flatten.Count.ShouldBe(0);
    }
    
    [Test]
    public void ShouldFlattenCacheRemoveChildOfChild()
    {
        //Given
        var root = new TestNode();
        var flatten = root.ChildrenCache.Connect()
            .Flatten(x => x.ChildrenCache.Connect(), x => x.Id)
            .AsObservableCache();
        
        var child1 = new TestNode();
        root.ChildrenCache.AddOrUpdate(child1);
        var child2 = new TestNode();
        child1.ChildrenCache.AddOrUpdate(child2);
        
        //When
        child1.ChildrenCache.Remove(child2);

        //Then
        root.ChildrenCache.Count.ShouldBe(1);
        child1.ChildrenCache.Count.ShouldBe(0);
        child2.ChildrenCache.Count.ShouldBe(0);
        flatten.Count.ShouldBe(1);
    }
    
    private sealed class TestNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public SourceCache<TestNode, string> ChildrenCache { get; } = new(x => x.Id);
    }
}