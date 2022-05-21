using Moq;
using PoeShared.Caching;

namespace PoeShared.Tests.Caching;

[TestFixture]
internal sealed class NaiveMemoryCacheTests
{
    private Mock<IClock> clock;
    private Mock<IItemResolver<string, Item>> itemResolver;

    [SetUp]
    public void SetUp()
    {
        clock = new Mock<IClock>();
        itemResolver = new Mock<IItemResolver<string, Item>>();
    }

    [Test]
    public void ShouldReturn()
    {
        //Given
        var instance = CreateInstance();
        itemResolver.Setup(x => x.Resolve("new", default)).Returns(new Item(1));

        //When

        var item = instance.GetOrUpdate("new", itemResolver.Object.Resolve);

        //Then
        item.ShouldBe(new Item(1));
    }

    [Test]
    public void ShouldUpdateItem()
    {
        //Given
        var instance = CreateInstance();
        itemResolver.Setup(x => x.Resolve("new", default)).Returns(new Item(1));
        var item1 = instance.GetOrUpdate("new", itemResolver.Object.Resolve);
        itemResolver.Setup(x => x.Resolve("new", It.IsAny<Item>())).Returns(new Item(2));

        //When
        var item2 = instance.GetOrUpdate("new", itemResolver.Object.Resolve);

        //Then
        item2.ShouldBe(new Item(2));
    }
        
    [Test]
    public void ShouldDisposeUpdatedItem()
    {
        //Given
        var instance = CreateInstance();
        itemResolver.Setup(x => x.Resolve("new", default)).Returns(new Item(1));
        var item1 = instance.GetOrUpdate("new", itemResolver.Object.Resolve);
        itemResolver.Setup(x => x.Resolve("new", It.IsAny<Item>())).Returns(new Item(2));

        //When
        var item2 = instance.GetOrUpdate("new", itemResolver.Object.Resolve);

        //Then
        item1.IsDisposed.ShouldBe(true);
        item2.IsDisposed.ShouldBe(false);
    }

    [Test]
    public void ShouldReturnExisting()
    {
        //Given
        var instance = CreateInstance();
        itemResolver.Setup(x => x.Resolve("new", default)).Returns(new Item(1));
        itemResolver.Setup(x => x.Resolve("new", new Item(1))).Returns(new Item(2));

        //When
        var item1 = instance.GetOrUpdate("new", itemResolver.Object.Resolve);
        var item2 = instance.GetOrUpdate("new", itemResolver.Object.Resolve);

        //Then
        item1.ShouldBe(new Item(1));
        item2.ShouldBe(new Item(2));
    }

    [Test]
    public void ShouldSupportMultipleKeys()
    {
        //Given
        var instance = CreateInstance();
        itemResolver.Setup(x => x.Resolve("a", default)).Returns(new Item(1));
        itemResolver.Setup(x => x.Resolve("a", new Item(1))).Returns(new Item(2));
        itemResolver.Setup(x => x.Resolve("b", default)).Returns(new Item(10));
        itemResolver.Setup(x => x.Resolve("b", new Item(10))).Returns(new Item(11));

        //When
        var itemA1 = instance.GetOrUpdate("a", itemResolver.Object.Resolve);
        var itemB1 = instance.GetOrUpdate("b", itemResolver.Object.Resolve);
        var itemA2 = instance.GetOrUpdate("a", itemResolver.Object.Resolve);
        var itemB2 = instance.GetOrUpdate("b", itemResolver.Object.Resolve);

        //Then
        itemA1.ShouldBe(new Item(1));
        itemB1.ShouldBe(new Item(10));
        itemA2.ShouldBe(new Item(2));
        itemB2.ShouldBe(new Item(11));
    }

    [Test]
    [Ignore("Not implemented yet")]
    public void ShouldAllowReentryOnUpdate()
    {
        //Given
        var instance = CreateInstance();
        itemResolver.Setup(x => x.Resolve("new", default)).Returns(new Item(1));
        itemResolver.Setup(x => x.Resolve("new", new Item(1))).Returns(() => 
        {
            var newItem = instance.GetOrUpdate("new", (key, existing) => new Item(3));
            return newItem;
        });

        //When
        var item1 = instance.GetOrUpdate("new", itemResolver.Object.Resolve);
        var item2 = instance.GetOrUpdate("new", itemResolver.Object.Resolve);

        //Then
        item1.ShouldBe(new Item(1));
        item1.ShouldBe(new Item(3));
    }
        
    private NaiveMemoryCache<string, Item> CreateInstance()
    {
        return new(clock.Object);
    }

        
}