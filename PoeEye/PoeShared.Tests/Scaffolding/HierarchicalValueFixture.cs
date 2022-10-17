using System;
using System.Linq.Expressions;
using LinqKit;
using NUnit.Framework;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class HierarchicalValueFixture : FixtureBase
{
    [Test]
    public void ShouldCalculateSingle()
    {
        //Given
        var root = new ValueContainer() { Value = 1 };

        //When
        var instance = new HierarchicalValue<ValueContainer, int>(root, x => x.Value, x => x.Parent, (x, y) => x.Value + y);

        //Then
        instance.Parent.ShouldBeNull();
        instance.Owner.ShouldBeSameAs(root);
        instance.OwnerValue.ShouldBe(1);
        instance.CalculatedValue.ShouldBe(1);
    }
    
    [Test]
    public void ShouldCalculateTree()
    {
        //Given
        var root = new ValueContainer()
        {
            Value = 1 
        };
        var inner = new ValueContainer()
        {
            Parent = root,
            Value = 2
        };

        //When
        var instance = new HierarchicalValue<ValueContainer, int>(inner, x => x.Value, x => x.Parent, (x, y) => x.Value + y);

        //Then
        instance.Parent.ShouldBeSameAs(root);
        instance.Owner.ShouldBeSameAs(inner);
        instance.OwnerValue.ShouldBe(2);
        instance.CalculatedValue.ShouldBe(3);
    }
    
    public sealed class ValueContainer : DisposableReactiveObject
    {
        public ValueContainer Parent { get; set; }
        
        public int Value { get; set; }
    }
}