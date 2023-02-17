using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using NUnit.Framework;
using DynamicData;
using DynamicData.Binding;
using PoeShared.Scaffolding;
using PoeShared.Tests.Helpers;
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
}