using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class ChangeSetExtensionsFixture : FixtureBase
{
    [Test]
    public void ShouldPopulateFromListWhenAdded()
    {
        //Given
        var target = new SourceCache<(string, int), string>(x => x.Item1);
        var source = new SourceListEx<(string, int)>();
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
        var source = new SourceListEx<(string, int)>();
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
        var source = new SourceListEx<(string, int)>();
        using var anchor = target.PopulateFrom(source);
        source.Add(("test", 1));
        target.Lookup("test").Value.ShouldBe(("test", 1));

        //When
        source.Replace(("test", 1), ("test", 2));

        //Then
        target.Lookup("test").Value.ShouldBe(("test", 2));
    }

    [Test]
    public void ShouldReplicateNotificationChangesForAdd()
    {
        //Given
        var collection = PresetCollections(out var received, out var expected);

        //When
        collection.Add("1");

        //Then
        ShouldBeEqual(received.ToArray(), expected.ToArray());
    }

    [Test]
    public void ShouldReplicateNotificationChangesForRemove()
    {
        //Given
        var collection = PresetCollections(out var received, out var expected);

        //When
        collection.Add("1");
        collection.Remove("1");

        //Then
        ShouldBeEqual(received.ToArray(), expected.ToArray());
    }

    [Test]
    public void ShouldReplicateNotificationChangesForReplace()
    {
        //Given
        var collection = PresetCollections(out var received, out var expected);

        //When
        collection.Add("1");
        collection.Replace("1", "2");

        //Then
        ShouldBeEqual(received.ToArray(), expected.ToArray());
    }

    [Test]
    public void ShouldReplicateNotificationChangesForClear()
    {
        //Given
        var collection = PresetCollections(out var received, out var expected);

        //When
        collection.Add("1");
        collection.Add("2");
        collection.Clear();

        //Then
        ShouldBeEqual(received.ToArray(), expected.ToArray());
    }

    private ObservableCollection<string> PresetCollections(out IReadOnlyObservableCollection<NotifyCollectionChangedEventArgs> received, out IReadOnlyObservableCollection<NotifyCollectionChangedEventArgs> expected)
    {
        var collection = new ObservableCollection<string>();
        expected = collection.ObserveCollectionChanges().Select(x => x.EventArgs).Listen();
        received = collection.ToObservableChangeSet().ToNotifyCollectionChanged().Listen();
        return collection;
    }

    private void ShouldBeEqual(IList<NotifyCollectionChangedEventArgs> received, IList<NotifyCollectionChangedEventArgs> expected)
    {
        Log.Debug($"Received:\n\t{received.Select(x => x.ToJson()).DumpToTable()}");
        Log.Debug($"Expected:\n\t{expected.Select(x => x.ToJson()).DumpToTable()}");
        received.Count.ShouldBe(expected.Count);
        for (int idx = 0; idx < expected.Count; idx++)
        {
            ShouldBeEqual(received[idx], expected[idx]);
        }
    }

    private void ShouldBeEqual(NotifyCollectionChangedEventArgs first, NotifyCollectionChangedEventArgs expected)
    {
        first.Action.ShouldBe(expected.Action);
        first.OldStartingIndex.ShouldBe(expected.OldStartingIndex);
        first.NewStartingIndex.ShouldBe(expected.NewStartingIndex);
        first.NewItems.CollectionSequenceShouldBe(expected.NewItems);
        first.OldItems.CollectionSequenceShouldBe(expected.OldItems);
    }

    [Test]
    public void ShouldFilterAndSortSourceCache()
    {
        //Given
        var sourceCache = new SourceCache<string, string>(x => x);

        using var anchor = sourceCache
            .Connect()
            .RemoveKey()
            .Filter(x => x.Contains(" "))
            .Sort(new SortExpressionComparer<string>().ThenByDescending(x => x), resetThreshold: int.MaxValue)
            .Transform(x => $"{x} t")
            .BindToCollection(out var additionalFiles)
            .SubscribeToErrors(Log.HandleUiException);

        //When
        sourceCache.AddOrUpdate("1");
        sourceCache.AddOrUpdate("1 a");
        sourceCache.AddOrUpdate("3");
        sourceCache.AddOrUpdate("3 a");
        sourceCache.AddOrUpdate("2");
        sourceCache.AddOrUpdate("2 a");

        //Then
        additionalFiles.CollectionSequenceShouldBe("3 a t", "2 a t", "1 a t");
    }

    [Test]
    public void ShouldFilterAndSortSourceCacheAndNotCrash()
    {
        //Given
        var sourceCache = new SourceCache<string, string>(x => x);

        using var anchor = sourceCache
            .Connect()
            .RemoveKey()
            .Filter(x => x.Contains(" "))
            .Sort(new SortExpressionComparer<string>().ThenByDescending(x => x), resetThreshold: int.MaxValue)
            .Transform(x => $"{x} t")
            .BindToCollection(out var additionalFiles)
            .SubscribeToErrors(Log.HandleUiException);

        //When
        sourceCache.AddOrUpdate("1");
        sourceCache.AddOrUpdate("1 a");
        sourceCache.AddOrUpdate("3");
        sourceCache.AddOrUpdate("3 a");
        sourceCache.AddOrUpdate("2");
        sourceCache.AddOrUpdate("2 a");
        sourceCache.Clear();
        sourceCache.AddOrUpdate("1");
        sourceCache.AddOrUpdate("1 a");
        sourceCache.AddOrUpdate("2");

        //Then
        additionalFiles.CollectionSequenceShouldBe("1 a t");
    }

    [Test]
    [TestCase(1)]
    [TestCase(2)]
    public void ShouldNotCrash(int threads)
    {
        //Given
        var sourceCache = new SourceCache<string, string>(x => x);

        using var anchor = sourceCache
            .Connect()
            .RemoveKey()
            .Filter(x => x.Contains(" "))
            .Sort(new SortExpressionComparer<string>().ThenByDescending(x => x), resetThreshold: int.MaxValue)
            .Transform(x => $"{x} t")
            .BindToCollection(out var additionalFiles)
            .SubscribeToErrors(Log.HandleUiException);

        //When


        //Then
    }

    [Test]
    public void ShouldBindToSourceList_Synchronize()
    {
        //Given
        var outputCollection = new ObservableCollection<int>() { 2 };
        var sourceList = new SourceList<int>();
        using var subscription = outputCollection.BindToSourceList(sourceList);

        //When
        outputCollection.Add(1);

        //Then
        sourceList.Items.CollectionSequenceShouldBe(1);
    }

    /// <summary>
    /// Tests that when an item is added to the output collection, it gets synchronized to the source list.
    /// </summary>
    [Test]
    public void ShouldBindToSourceList_Addition()
    {
        //Given
        var outputCollection = new ObservableCollection<int>();
        var sourceList = new SourceList<int>();
        using var subscription = outputCollection.BindToSourceList(sourceList);

        //When
        outputCollection.Add(1);

        //Then
        sourceList.Items.CollectionSequenceShouldBe(1);
    }

    /// <summary>
    /// Tests that when an item is removed from the output collection, it gets removed from the source list as well.
    /// </summary>
    [Test]
    public void ShouldBindToSourceList_Removal()
    {
        //Given
        var outputCollection = new ObservableCollection<int>();
        var sourceList = new SourceList<int>();
        sourceList.AddRange(new[] {1, 2, 3});
        using var subscription = outputCollection.BindToSourceList(sourceList);

        //When
        outputCollection.Remove(2);

        //Then
        sourceList.Items.CollectionSequenceShouldBe(1, 3);
    }

    /// <summary>
    /// Tests that when the output collection is cleared, the source list gets cleared as well.
    /// </summary>
    [Test]
    public void ShouldBindToSourceList_Clear()
    {
        //Given
        var outputCollection = new ObservableCollection<int>();
        var sourceList = new SourceList<int>();
        sourceList.AddRange(new[] {1, 2, 3});

        using var subscription = outputCollection.BindToSourceList(sourceList);

        //When
        outputCollection.Clear();

        //Then
        Assert.IsEmpty(sourceList.Items);
    }

    /// <summary>
    /// Tests that when an item in the output collection is replaced, the corresponding item in the source list gets replaced as well.
    /// </summary>
    [Test]
    public void ShouldBindToSourceList_Replace()
    {
        //Given
        var outputCollection = new ObservableCollection<int>();
        var sourceList = new SourceList<int>();
        sourceList.AddRange(new[] {1, 2, 3});

        using var subscription = outputCollection.BindToSourceList(sourceList);

        //When
        outputCollection[1] = 4;

        //Then
        sourceList.Items.CollectionSequenceShouldBe(1, 4, 3);
    }

    /// <summary>
    /// Tests that when an item is moved within the output collection, the item's position is updated in the source list as well.
    /// </summary>
    [Test]
    public void ShouldBindToSourceList_MoveItem()
    {
        //Given
        var outputCollection = new ObservableCollection<int>();
        var sourceList = new SourceList<int>();
        sourceList.AddRange(new[] {1, 2, 3});

        using var subscription = outputCollection.BindToSourceList(sourceList);

        //When
        outputCollection.Move(0, 2); // Move first item to last position

        //Then
        sourceList.Items.CollectionSequenceShouldBe(2, 3, 1);
    }

    /// <summary>
    /// Tests that when multiple items are added to the output collection, they are synchronized to the source list.
    /// </summary>
    [Test]
    public void ShouldBindToSourceList_MultipleAdditions()
    {
        //Given
        var outputCollection = new ObservableCollection<int>();
        var sourceList = new SourceList<int>();
        using var subscription = outputCollection.BindToSourceList(sourceList);

        //When
        outputCollection.Add(1);
        outputCollection.Add(2);
        outputCollection.Add(3);

        //Then
        sourceList.Items.CollectionSequenceShouldBe(1, 2, 3);
    }

    /// <summary>
    /// Tests that when multiple items are removed from the output collection, they are removed from the source list.
    /// </summary>
    [Test]
    public void ShouldBindToSourceList_MultipleRemovals()
    {
        //Given
        var outputCollection = new ObservableCollection<int>();
        var sourceList = new SourceList<int>();
        sourceList.AddRange(new[] {1, 2, 3, 4, 5});
        using var subscription = outputCollection.BindToSourceList(sourceList);

        //When
        outputCollection.Remove(1);
        outputCollection.Remove(3);
        outputCollection.Remove(5);

        //Then
        sourceList.Items.CollectionSequenceShouldBe(2, 4);
    }

    /// <summary>
    /// Tests that complex operations on the output collection are reflected in the source list.
    /// </summary>
    [Test]
    public void ShouldBindToSourceList_ComplexOperations()
    {
        //Given
        var outputCollection = new ObservableCollection<int>();
        var sourceList = new SourceList<int>();
        sourceList.AddRange(new[] {1, 2, 3, 4});

        using var subscription = outputCollection.BindToSourceList(sourceList);

        //When
        outputCollection.Add(5); // Add 5 -> 1,2,3,4,5
        outputCollection.Move(4, 0); // Move 5 to first position -> 5,1,2,3,4
        outputCollection.Remove(2); // Remove 2 -> 5,1,3,4
        outputCollection[1] = 6; // Replace 1 with 6 -> 5,6,3,4

        //Then
        sourceList.Items.CollectionSequenceShouldBe(5, 6, 3, 4);
    }

    /// <summary>
    /// Tests that when an item is inserted into the output collection at a specific index, 
    /// the same item gets inserted at the same index in the source list.
    /// </summary>
    [Test]
    public void ShouldBindToSourceList_SingleInsertion()
    {
        //Given
        var outputCollection = new ObservableCollection<int>();
        var sourceList = new SourceList<int>();
        sourceList.AddRange(new[] {1, 3});

        using var subscription = outputCollection.BindToSourceList(sourceList);

        //When
        outputCollection.Insert(1, 2); // Insert 2 at index 1

        //Then
        sourceList.Items.CollectionSequenceShouldBe(1, 2, 3);
    }

    /// <summary>
    /// Tests that inserting multiple items at specific indices in the output collection 
    /// results in the items being added at the correct positions in the source list.
    /// </summary>
    [Test]
    public void ShouldBindToSourceList_MultipleInsertions()
    {
        //Given
        var outputCollection = new ObservableCollection<int>();
        var sourceList = new SourceList<int>();
        sourceList.AddRange(new[] {1, 3});

        using var subscription = outputCollection.BindToSourceList(sourceList);

        //When
        outputCollection.Insert(1, 2);
        outputCollection.Insert(3, 4);

        //Then
        sourceList.Items.CollectionSequenceShouldBe(1, 2, 3, 4);
    }

    /// <summary>
    /// Tests that inserting an item and then removing it results in correct synchronization with the source list.
    /// </summary>
    [Test]
    public void ShouldBindToSourceList_InsertAndRemove()
    {
        //Given
        var outputCollection = new ObservableCollection<int>();
        var sourceList = new SourceList<int>();
        sourceList.AddRange(new[] {1, 3});

        using var subscription = outputCollection.BindToSourceList(sourceList);

        //When
        outputCollection.Insert(1, 2);
        outputCollection.Remove(2);

        //Then
        sourceList.Items.CollectionSequenceShouldBe(1, 3);
    }

    /// <summary>
    /// Tests that inserting an item and then replacing it results in correct synchronization with the source list.
    /// </summary>
    [Test]
    public void ShouldBindToSourceList_InsertAndReplace()
    {
        //Given
        var outputCollection = new ObservableCollection<int>();
        var sourceList = new SourceList<int>();
        sourceList.AddRange(new[] {1, 3});

        using var subscription = outputCollection.BindToSourceList(sourceList);

        //When
        outputCollection.Insert(1, 2);
        outputCollection[1] = 4; // Replace the previously inserted item

        //Then
        sourceList.Items.CollectionSequenceShouldBe(1, 4, 3);
    }
}