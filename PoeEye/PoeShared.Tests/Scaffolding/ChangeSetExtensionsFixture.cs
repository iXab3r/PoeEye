using NUnit.Framework;
using DynamicData;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class ChangeSetExtensionsFixture : FixtureBase
{

    [Test]
    public void ShouldPopulateFromListWhenAdded()
    {
        //Given
        var target = new SourceCache<(string, int), string>(x => x.Item1);
        var source = new SourceList<(string, int)>();
        using var anchor = target.PopulateFrom(source);

        //When
        source.Add(("test", 1));

        //Then
        target.Count.ShouldBe(1);
        target.Lookup("test").Value.ShouldBe(("test", 1));
    }
        
    [Test]
    public void ShouldPopulateFromListWhenRemoved()
    {
        //Given
        var target = new SourceCache<(string, int), string>(x => x.Item1);
        var source = new SourceList<(string, int)>();
        using var anchor = target.PopulateFrom(source);
        source.Add(("test", 1));
        target.Lookup("test").Value.ShouldBe(("test", 1));

        //When
        source.Remove(("test", 1));

        //Then
        target.Count.ShouldBe(0);
    }
        
    [Test]
    public void ShouldPopulateFromListWhenReplaced()
    {
        //Given
        var target = new SourceCache<(string, int), string>(x => x.Item1);
        var source = new SourceList<(string, int)>();
        using var anchor = target.PopulateFrom(source);
        source.Add(("test", 1));
        target.Lookup("test").Value.ShouldBe(("test", 1));

        //When
        source.Replace(("test", 1), ("test", 2));

        //Then
        target.Lookup("test").Value.ShouldBe(("test", 2));
    }
}