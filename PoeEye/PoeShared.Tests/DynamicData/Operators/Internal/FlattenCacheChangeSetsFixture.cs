using System;
using DynamicData;
using PoeShared.Logging;

namespace PoeShared.Tests.DynamicData.Operators.Internal;

[TestFixture]
public class FlattenCacheChangeSetsFixture : FixtureBase
{
    [Test]
    public void ShouldFlattenCache()
    {
        //Given
        var root = new TestNode() { LogChanges = true };
        
        //When
        //Then
        DumpToLog(root);
        root.ChildrenCache.Count.ShouldBe(0);
        root.FlattenChildren.Count.ShouldBe(0);
    }
    
    [Test]
    public void ShouldFlattenCacheAdd()
    {
        //Given
        var root = new TestNode() { LogChanges = true };
 
        //When
        var child1 = new TestNode();
        root.ChildrenCache.AddOrUpdate(child1);

        //Then
        DumpToLog(root);
        root.ChildrenCache.Count.ShouldBe(1);
        root.FlattenChildren.Count.ShouldBe(1);
    }
    
    [Test]
    public void ShouldFlattenCacheRemove()
    {
        //Given
        var root = new TestNode() { LogChanges = true };
 
        var child1 = new TestNode();
        root.ChildrenCache.AddOrUpdate(child1);
        
        //When
        root.ChildrenCache.Remove(child1);

        //Then
        DumpToLog(root);
        root.ChildrenCache.Count.ShouldBe(0);
        root.FlattenChildren.Count.ShouldBe(0);
    }
    
    [Test]
    public void ShouldFlattenCacheAddToChild()
    {
        //Given
        var root = new TestNode() { LogChanges = true };
 
        var child1 = new TestNode();
        root.ChildrenCache.AddOrUpdate(child1);
        
        //When
        var child2 = new TestNode();
        child1.ChildrenCache.AddOrUpdate(child2);

        //Then
        DumpToLog(root);
        root.ChildrenCache.Count.ShouldBe(1);
        child1.ChildrenCache.Count.ShouldBe(1);
        child2.ChildrenCache.Count.ShouldBe(0);
        root.FlattenChildren.Count.ShouldBe(2);
    }
    
    
    [Test]
    public void ShouldFlattenCacheRemoveChild()
    {
        //Given
        var root = new TestNode() { LogChanges = true };

        var child1 = new TestNode();
        root.ChildrenCache.AddOrUpdate(child1);
        var child2 = new TestNode();
        child1.ChildrenCache.AddOrUpdate(child2);
        
        //When
        root.ChildrenCache.Remove(child1);

        //Then
        DumpToLog(root);
        root.ChildrenCache.Count.ShouldBe(0);
        child1.ChildrenCache.Count.ShouldBe(1);
        child2.ChildrenCache.Count.ShouldBe(0);
        root.FlattenChildren.Count.ShouldBe(0);
    }
    
    [Test]
    public void ShouldFlattenCacheRemoveChildOfChild()
    {
        //Given
        var root = new TestNode() { LogChanges = true };

        var child1 = new TestNode();
        root.ChildrenCache.AddOrUpdate(child1);
        var child2 = new TestNode();
        child1.ChildrenCache.AddOrUpdate(child2);
        
        //When
        child1.ChildrenCache.Remove(child2);

        //Then
        DumpToLog(root);
        root.ChildrenCache.Count.ShouldBe(1);
        child1.ChildrenCache.Count.ShouldBe(0);
        child2.ChildrenCache.Count.ShouldBe(0);
        root.FlattenChildren.Count.ShouldBe(1);
    }
    
    [Test]
    public void ShouldFlattenCacheRemoveChildOfChildAndChildItself()
    {
        //Given
        var root = new TestNode() { LogChanges = true };

        var child1 = new TestNode();
        root.ChildrenCache.AddOrUpdate(child1);
        var child2 = new TestNode();
        child1.ChildrenCache.AddOrUpdate(child2);
        
        //When
        child1.ChildrenCache.Remove(child2);
        root.ChildrenCache.Remove(child1);

        //Then
        DumpToLog(root);
        root.ChildrenCache.Count.ShouldBe(0);
        child1.ChildrenCache.Count.ShouldBe(0);
        child2.ChildrenCache.Count.ShouldBe(0);
        root.FlattenChildren.Count.ShouldBe(0);
    }

    private void DumpToLog(TestNode node)
    {
        Log.Info($"Node: {node}:\n\t{node.FlattenChildren.Items.DumpToTable()}");
    }

    private sealed class TestNode
    {
        private static readonly IFluentLog Log = typeof(TestNode).PrepareLogger();

        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public SourceCache<TestNode, string> ChildrenCache { get; } = new(x => x.Id);
        
        public IObservableCache<TestNode, string> FlattenChildren { get; }
        
        public bool LogChanges { get; set; }

        public TestNode()
        {
            FlattenChildren = ChildrenCache
                .Connect()
                .Flatten(node => node.FlattenChildren.Connect(), x => x.Id)
                .ForEachChange(change =>
                {
                    if (LogChanges)
                    {
                        Log.WithPrefix($"Flatten {this}").Info($"{change.Reason}: {change.Current}");
                    }
                })
                .AsObservableCache();
        }

        public override string ToString()
        {
            return $"Node #{Id.TakeMidChars(8)} (children: {ChildrenCache.Count})";
        }
    }
}