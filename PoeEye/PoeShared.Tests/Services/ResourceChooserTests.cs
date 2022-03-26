using System;
using System.Linq;
using NUnit.Framework;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.Tests.Helpers;
using Shouldly;

namespace PoeShared.Tests.Services;

[TestFixture]
public class ResourceChooserTests : FixtureBase
{
    [Test]
    public void ShouldCreate()
    {
        // Given
        
        // When 
        var action = () => CreateInstance();

        // Then
        action.ShouldNotThrow();
    }

    [Test]
    public void ShouldReturn()
    {
        // Given
        var instance = CreateInstance();
        instance.Add("a");
        
        // When 
        var resource = instance.GetAlive();

        // Then
        resource.ShouldBe("a");
    }

    [Test]
    public void ShouldReturnSame()
    {
        // Given
        var instance = CreateInstance();
        instance.Add("a");
        instance.GetAlive();

        // When 
        var resource = instance.GetAlive();

        // Then
        resource.ShouldBe("a");
    }

    [Test]
    public void ShouldThrowWhenNoResources()
    {
        // Given
        var instance = CreateInstance();

        // When 
        var action = () => instance.GetAlive();

        // Then
        action.ShouldThrow<InvalidOperationException>();
    }

    [Test]
    public void ShouldRemove()
    {
        // Given
        var instance = CreateInstance();
        instance.Add("a");
        instance.Add("b");
        instance.GetAlive().ShouldBe("a");

        // When 
        instance.Remove("a");
        
        // Then
        instance.GetAlive().ShouldBe("b");
    }

    [Test]
    public void ShouldRotateBrokenNonFirst()
    {
        // Given
        var instance = CreateInstance();
        instance.Add("a");
        instance.Add("b");
        instance.GetAlive().ShouldBe("a");
        
        // When 
        instance.ReportBroken("b");

        // Then
        instance.GetAlive().ShouldBe("a");
    }
    
    [Test]
    public void ShouldRotateBrokenFirst()
    {
        // Given
        var instance = CreateInstance();
        instance.Add("a");
        instance.Add("b");
        instance.GetAlive().ShouldBe("a");
        
        // When 
        instance.ReportBroken("a");

        // Then
        instance.GetAlive().ShouldBe("b");
    }

    [Test]
    public void ShouldReturnLastWhenAllItemsAreExhausted()
    {
        // Given
        var instance = CreateInstance();
        instance.Add("a");
        instance.Add("b");
        instance.ReportBroken("a");
        
        // When 
        instance.ReportBroken("b");

        // Then
        instance.GetAlive().ShouldBe("a");
    }

    [Test]
    public void ShouldKeepCurrentEvenAfterResurrection()
    {
        // Given
        var instance = CreateInstance();
        instance.Add("a");
        instance.Add("b");
        instance.ReportBroken("a");
        instance.GetAlive().ShouldBe("b");

        // When 
        instance.ReportAlive("a");

        // Then
        instance.GetAlive().ShouldBe("b");
    }
    
    [Test]
    public void ShouldKeepPlaceResurrectedAfterCurrentlyAlive()
    {
        // Given
        var instance = CreateInstance();
        instance.Add("a");
        instance.Add("b");
        instance.ReportBroken("a");
        instance.GetAlive().ShouldBe("b");
        instance.ReportAlive("a");

        // When 
        instance.ReportBroken("b");

        // Then
        instance.GetAlive().ShouldBe("a");
    }
    
    [Test]
    public void ShouldReturnLastWhenAllItemsAreExhaustedAndReturned()
    {
        // Given
        var instance = CreateInstance();
        instance.Add("a");
        instance.Add("b");
        instance.ReportBroken("a");
        instance.ReportBroken("b");
        instance.GetAlive().ShouldBe("a");
        
        // When 
        instance.ReportBroken("a");

        // Then
        instance.GetAlive().ShouldBe("b");
    }

    [Test]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(10)]
    public void ShouldHandleOverflow(long count)
    {
        // Given
        var instance = CreateInstance();
        var items = new[] { "a", "b" };
        items.ForEach(instance.Add);
        
        // When 
        // Then
        for (int i = 0; i < count; i++)
        {
            var itemToReport = items[i % items.Length];
            var itemToExpect = items[(i+1) % items.Length];
            instance.ReportBroken(itemToReport);
            instance.GetAlive().ShouldBe(itemToExpect);
        }
    }

    [Test]
    public void ShouldAdd()
    {
        // Given
        var instance = CreateInstance();
        instance.Add("a");
        instance.Add("b");
        instance.ReportBroken("a");
        instance.ReportBroken("b");
        
        // When 
        instance.Add("c");

        // Then
        instance.GetAlive().ShouldBe("c");
    }

    [Test]
    public void ShouldEnumerate()
    {
        // Given
        var instance = CreateInstance();
        instance.Add("a");
        instance.Add("b");
        
        // When 
        var items = instance.ToArray();

        // Then
        items.CollectionSequenceShouldBe("a", "b");
    }
    
    [Test]
    public void ShouldEnumerateAliveFirst()
    {
        // Given
        var instance = CreateInstance();
        instance.Add("a");
        instance.Add("b");
        instance.ReportBroken("a");
        
        // When 
        var items = instance.ToArray();

        // Then
        items.CollectionSequenceShouldBe("b", "a");
    }
    
    [Test]
    public void ShouldEnumerateResurrected()
    {
        // Given
        var instance = CreateInstance();
        instance.Add("a");
        instance.Add("b");
        instance.Add("c");
        instance.ReportBroken("a");
        instance.CollectionSequenceShouldBe("b", "c", "a");
        instance.ReportBroken("b");
        instance.CollectionSequenceShouldBe("c", "a", "b");

        // When 
        instance.ReportAlive("b");

        // Then
        instance.CollectionSequenceShouldBe("c", "b", "a");
    }

    private ResourceChooser<string> CreateInstance()
    {
        return new ResourceChooser<string>();
    }
}