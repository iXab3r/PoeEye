using System;
using System.Linq;
using PoeShared.Scaffolding;

namespace PoeShared.Tests.Scaffolding;

public class BiMapFixture : FixtureBase
{
    [Test]
    public void ShouldGetForward()
    {
        //Given
        var instance = CreateInstance();

        //When
        instance.Add(1, "1");

        //Then
        instance[1].ShouldBe("1");
        instance.Forward[1].ShouldBe("1");
        instance.Reverse["1"].ShouldBe(1);
    }

    [Test]
    public void ShouldGetReverse()
    {
        //Given
        var instance = CreateInstance();

        //When
        instance.Add(2, "two");

        //Then
        instance["two"].ShouldBe(2);
        instance.Reverse["two"].ShouldBe(2);
        instance.Forward[2].ShouldBe("two");
    }

    [Test]
    public void ShouldThrowOnDuplicateForwardAdd()
    {
        //Given
        var instance = CreateInstance();
        instance.Add(1, "one");

        //When
        var action = () => instance.Add(1, "uno");

        //Then
        action.ShouldThrow<ArgumentException>();
    }

    [Test]
    public void ShouldThrowOnDuplicateReverseAdd()
    {
        //Given
        var instance = CreateInstance();
        instance.Add(1, "dup");

        //When
        var action = () => instance.Add(2, "dup");

        //Then
        action.ShouldThrow<ArgumentException>();
    }

    [Test]
    public void ShouldTryAddReturnFalseOnDuplicate()
    {
        //Given
        var instance = CreateInstance();
        instance.Add(1, "one");

        //When
        var result1 = instance.TryAdd(1, "uno");
        var result2 = instance.TryAdd(2, "one");

        //Then
        result1.ShouldBeFalse();
        result2.ShouldBeFalse();
        instance.Count.ShouldBe(1);
        instance[1].ShouldBe("one");
    }

    [Test]
    public void ShouldSetOverwriteExistingMappings()
    {
        //Given
        var instance = CreateInstance();
        instance.Add(1, "one");
        instance.Add(2, "two");

        //When
        instance.Set(1, "two"); // now 2->two should be removed

        //Then
        instance.Count.ShouldBe(1);
        instance[1].ShouldBe("two");
        instance.ContainsForward(2).ShouldBeFalse();
        instance.ContainsReverse("one").ShouldBeFalse();
    }

    [Test]
    public void ShouldRemoveByForward()
    {
        //Given
        var instance = CreateInstance();
        instance.Add(1, "one");
        instance.Add(2, "two");

        //When
        var removed = instance.RemoveByForward(1);

        //Then
        removed.ShouldBeTrue();
        instance.ContainsForward(1).ShouldBeFalse();
        instance.ContainsReverse("one").ShouldBeFalse();
        instance.Count.ShouldBe(1);
    }

    [Test]
    public void ShouldRemoveByReverse()
    {
        //Given
        var instance = CreateInstance();
        instance.Add(1, "one");
        instance.Add(2, "two");

        //When
        var removed = instance.RemoveByReverse("two");

        //Then
        removed.ShouldBeTrue();
        instance.ContainsForward(2).ShouldBeFalse();
        instance.ContainsReverse("two").ShouldBeFalse();
        instance.Count.ShouldBe(1);
    }

    [Test]
    public void ShouldClear()
    {
        //Given
        var instance = CreateInstance();
        instance.Add(1, "one");
        instance.Add(2, "two");

        //When
        instance.Clear();

        //Then
        instance.Count.ShouldBe(0);
        instance.ContainsForward(1).ShouldBeFalse();
        instance.ContainsReverse("two").ShouldBeFalse();
    }

    [Test]
    public void ShouldContainAndTryGet()
    {
        //Given
        var instance = CreateInstance();
        instance.Add(1, "one");

        //When
        var containsF = instance.ContainsForward(1);
        var containsR = instance.ContainsReverse("one");
        var tryGetF = instance.TryGetByForward(1, out var r1);
        var tryGetR = instance.TryGetByReverse("one", out var f1);

        //Then
        containsF.ShouldBeTrue();
        containsR.ShouldBeTrue();
        tryGetF.ShouldBeTrue();
        tryGetR.ShouldBeTrue();
        r1.ShouldBe("one");
        f1.ShouldBe(1);
    }

    [Test]
    public void ShouldEnumeratePairs()
    {
        //Given
        var instance = CreateInstance();
        instance.Add(1, "one");
        instance.Add(2, "two");

        //When
        var pairs = instance.ToArray();

        //Then
        pairs.Length.ShouldBe(2);
        pairs.ShouldContain(kv => kv.Key == 1 && kv.Value == "one");
        pairs.ShouldContain(kv => kv.Key == 2 && kv.Value == "two");
    }
    
    public BiMap<int, string> CreateInstance()
    {
        return new BiMap<int, string>();
    }
}