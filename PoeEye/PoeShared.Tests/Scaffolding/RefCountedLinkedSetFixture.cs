using System.Linq;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class RefCountedLinkedSetFixture : FixtureBase
{
    [Test]
    public void ShouldAddItem()
    {
        var instance = CreateInstance();

        using var _ = instance.Add("test");

        instance.ToArray().ShouldBe(new[] { "test" });
        instance.Count.ShouldBe(1);
        instance.Contains("test").ShouldBeTrue();
    }

    [Test]
    public void ShouldNotAddSameItemTwice()
    {
        var instance = CreateInstance();

        using var _1 = instance.Add("test");
        using var _2 = instance.Add("test");

        instance.ToArray().ShouldBe(new[] { "test" });
        instance.Count.ShouldBe(1);
    }

    [Test]
    public void ShouldKeepItemUntilLastDispose()
    {
        var instance = CreateInstance();

        var t1 = instance.Add("x");
        var t2 = instance.Add("x");

        instance.ToArray().ShouldBe(new[] { "x" });
        instance.Count.ShouldBe(1);

        t1.Dispose();
        instance.ToArray().ShouldBe(new[] { "x" });
        instance.Count.ShouldBe(1);

        t2.Dispose();
        instance.ShouldBeEmpty();
        instance.Count.ShouldBe(0);
        instance.Contains("x").ShouldBeFalse();
    }

    [Test]
    public void ShouldSupportOutOfOrderDisposal()
    {
        var instance = CreateInstance();

        var ta = instance.Add("a");
        var tb = instance.Add("b");

        instance.ToArray().ShouldBe(new[] { "b", "a" }, "MRU-first ordering");

        ta.Dispose(); // remove "a" first
        instance.ToArray().ShouldBe(new[] { "b" });

        tb.Dispose(); // then "b"
        instance.ShouldBeEmpty();
    }

    [Test]
    public void ShouldPreserveMRUOrdering()
    {
        var instance = CreateInstance();

        using var _a = instance.Add("a");
        using var _b = instance.Add("b");
        using var _c = instance.Add("c");

        instance.ToArray().ShouldBe(new[] { "c", "b", "a" });
    }

    [Test]
    public void DuplicateAdd_ShouldBumpRecency()
    {
        var instance = CreateInstance();

        using var _a1 = instance.Add("a");
        using var _b  = instance.Add("b");
        using var _a2 = instance.Add("a"); // bump "a" to MRU

        instance.ToArray().ShouldBe(new[] { "a", "b" });

        // disposing the bump should not drop "a" (refcount goes from 2 -> 1)
        _a2.Dispose();
        instance.ToArray().ShouldBe(new[] { "a", "b" });
        instance.Count.ShouldBe(2);
    }

    [Test]
    public void ShouldHandleMultipleAddsAsRefCounts()
    {
        var instance = CreateInstance();

        var t1 = instance.Add("v");
        var t2 = instance.Add("v");
        var t3 = instance.Add("v");

        instance.ToArray().ShouldBe(new[] { "v" });
        instance.Count.ShouldBe(1);

        t1.Dispose();
        instance.ToArray().ShouldBe(new[] { "v" });

        t2.Dispose();
        instance.ToArray().ShouldBe(new[] { "v" });

        t3.Dispose();
        instance.ShouldBeEmpty();
    }

    [Test]
    public void Add_Null_ShouldBeNoop()
    {
        var instance = CreateInstance();

        using var _ = instance.Add(null);

        instance.ShouldBeEmpty();
        instance.Count.ShouldBe(0);
    }

    [Test]
    public void Clear_ShouldRemoveEverything_AndTokensRemainHarmless()
    {
        var instance = CreateInstance();

        var ta = instance.Add("a");
        var tb = instance.Add("b");
        instance.Count.ShouldBe(2);

        instance.Clear();

        instance.ShouldBeEmpty();
        instance.Count.ShouldBe(0);

        // Disposing tokens after Clear should not throw and should not re-add
        ta.Dispose();
        tb.Dispose();

        instance.ShouldBeEmpty();
    }

    private RefCountedLinkedSet<string> CreateInstance() => new RefCountedLinkedSet<string>();
}