using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
internal class SourceCachesSynchronizerFixture : FixtureBase
{
    private SourceCache<int, string> sourceList;
    private SourceCache<int, string> targetList;

    protected override void SetUp()
    {
        base.SetUp();
        sourceList = new SourceCache<int, string>(x => x.ToString());
        targetList = new SourceCache<int, string>(x => x.ToString());
    }

    [Test]
    public void ShouldCreate()
    {
        //Given

        //When
        Action action = () => CreateInstance();

        //Then
        action.ShouldNotThrow();
    }


    [Test]
    public void ShouldSynchronizeAddFromSourceToTarget()
    {
        //Given
        using var synchronizer = CreateInstance();

        //When
        sourceList.AddOrUpdate(1);

        //Then
        targetList.Items.ShouldContain(1);
    }

    [Test]
    public void ShouldSynchronizeAddFromTargetToSource()
    {
        //Given
        using var synchronizer = CreateInstance();

        //When
        targetList.AddOrUpdate(2);

        //Then
        sourceList.Items.ShouldContain(2);
    }

    [Test]
    public void ShouldSynchronizeRemoveFromSource()
    {
        //Given
        using var synchronizer = CreateInstance();
        sourceList.AddOrUpdate(1);

        //When
        sourceList.Remove(1);

        //Then
        targetList.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldSynchronizeRemoveFromTarget()
    {
        //Given
        using var synchronizer = CreateInstance();
        targetList.AddOrUpdate(2);

        //When
        targetList.Remove(2);

        //Then
        sourceList.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldSynchronizeMultipleChangesFromSource()
    {
        //Given
        using var synchronizer = CreateInstance();

        //When
        sourceList.AddOrUpdate(new[] {1, 2, 3});
        sourceList.Remove(2);

        //Then
        targetList.Items.ShouldContain(1);
        targetList.Items.ShouldContain(3);
        targetList.Items.ShouldNotContain(2);
    }

    [Test]
    public void ShouldSynchronizeMultipleChangesFromTarget()
    {
        //Given
        using var synchronizer = CreateInstance();

        //When
        targetList.AddOrUpdate(new[] {4, 5, 6});
        targetList.Remove(5);

        //Then
        sourceList.Items.ShouldContain(4);
        sourceList.Items.ShouldContain(6);
        sourceList.Items.ShouldNotContain(5);
    }

    [Test]
    public void ShouldSynchronizeClearFromSource()
    {
        //Given
        using var synchronizer = CreateInstance();
        sourceList.AddOrUpdate(new[] {1, 2, 3, 4, 5});

        //When
        sourceList.Clear();

        //Then
        targetList.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldSynchronizeClearFromTarget()
    {
        //Given
        using var synchronizer = CreateInstance();
        targetList.AddOrUpdate(new[] {1, 2, 3, 4, 5});

        //When
        targetList.Clear();

        //Then
        sourceList.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldSynchronizeComplexAddRemoveSequenceFromSource()
    {
        //Given
        using var synchronizer = CreateInstance();
        sourceList.AddOrUpdate(new[] {1, 2, 3});

        //When
        sourceList.AddOrUpdate(4);
        sourceList.Remove(2);
        sourceList.AddOrUpdate(new[] {5, 6});
        sourceList.Remove(1);

        //Then
        targetList.Items.OrderBy(x => x).CollectionSequenceShouldBe(3, 4, 5, 6);
        targetList.Items.ShouldNotContain(1);
        targetList.Items.ShouldNotContain(2);
    }

    [Test]
    public void ShouldSynchronizeComplexAddRemoveSequenceFromTarget()
    {
        //Given
        using var synchronizer = CreateInstance();
        targetList.AddOrUpdate(new[] {7, 8, 9});

        //When
        targetList.AddOrUpdate(10);
        targetList.Remove(8);
        targetList.AddOrUpdate(new[] {11, 12});
        targetList.Remove(7);

        //Then
        sourceList.Items.OrderBy(x => x).CollectionSequenceShouldBe(9, 10, 11, 12);
        sourceList.Items.ShouldNotContain(7);
        sourceList.Items.ShouldNotContain(8);
    }

    [Test]
    public void ShouldSynchronizeMixedChangesFromBothLists()
    {
        //Given
        using var synchronizer = CreateInstance();
        sourceList.AddOrUpdate(1);
        targetList.AddOrUpdate(2);

        //When
        sourceList.AddOrUpdate(3);
        targetList.AddOrUpdate(4);

        //Then
        sourceList.Items.CollectionSequenceShouldBe(1, 2, 3, 4);
        targetList.Items.CollectionSequenceShouldBe(1, 2, 3, 4);
    }

    [Test]
    public void ShouldNotPropagateChangesBackToOriginalList()
    {
        //Given
        using var synchronizer = CreateInstance();
        int sourceChangesCount = 0;
        int targetChangesCount = 0;

        sourceList.Connect().Subscribe(_ => sourceChangesCount++);
        targetList.Connect().Subscribe(_ => targetChangesCount++);

        //When
        sourceList.AddOrUpdate(1);

        //Then
        sourceChangesCount.ShouldBe(1); // only the original change should count
        targetChangesCount.ShouldBe(1); // the propagated change should count
    }

    [Test]
    public void SourceShouldNotReceiveChangesAfterDisposal()
    {
        //Given
        var synchronizer = CreateInstance();
        synchronizer.Dispose();

        //When
        targetList.AddOrUpdate(2);

        //Then
        sourceList.Count.ShouldBe(0);
    }

    [Test]
    public void TargetShouldNotReceiveChangesAfterDisposal()
    {
        //Given
        var synchronizer = CreateInstance();
        synchronizer.Dispose();

        //When
        sourceList.AddOrUpdate(1);

        //Then
        targetList.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldSupportMultiThreadedInsertions()
    {
        //Given
        using var synchronizer = CreateInstance();

        var itemsToInsertIntoSource = Enumerable.Range(0, 999).ToArray();
        var itemsToInsertIntoTarget = Enumerable.Range(1000, 999).ToArray();

        var startEvent = new ManualResetEvent(false);

        var insertionTasksIntoSource = Task.Factory.StartNew(() =>
        {
            startEvent.WaitOne();
            foreach (var i in itemsToInsertIntoSource)
            {
                sourceList.AddOrUpdate(i);
                Thread.Sleep(0); // switch context
            }
        });
        var insertionTasksIntoTarget = Task.Factory.StartNew(() =>
        {
            startEvent.WaitOne();
            foreach (var i in itemsToInsertIntoTarget)
            {
                targetList.AddOrUpdate(i);
                Thread.Sleep(0); // switch context
            }
        });

        //When
        startEvent.Set();
        Task.WaitAll(insertionTasksIntoSource, insertionTasksIntoTarget);

        //Then
        sourceList.Items.OrderBy(x => x).CollectionSequenceShouldBe(itemsToInsertIntoSource.Concat(itemsToInsertIntoTarget).ToArray());
        targetList.Items.OrderBy(x => x).CollectionSequenceShouldBe(itemsToInsertIntoSource.Concat(itemsToInsertIntoTarget).ToArray());
    }

    [Test]
    public void ShouldSupportMultiThreadedDeletions()
    {
        //Given
        using var synchronizer = CreateInstance();

        var initialItems = Enumerable.Range(0, 2000).ToArray();
        sourceList.AddOrUpdate(initialItems);

        var itemsToDeleteFromSource = Enumerable.Range(0, 1000).ToArray();
        var itemsToDeleteFromTarget = Enumerable.Range(1000, 1000).ToArray();

        var startEvent = new ManualResetEvent(false);

        var deletionTasksFromSource = Task.Factory.StartNew(() =>
        {
            startEvent.WaitOne();
            foreach (var i in itemsToDeleteFromSource)
            {
                sourceList.Remove(i);
                Thread.Sleep(0); // switch context
            }
        });
        var deletionTasksFromTarget = Task.Factory.StartNew(() =>
        {
            startEvent.WaitOne();
            foreach (var i in itemsToDeleteFromTarget)
            {
                targetList.Remove(i);
                Thread.Sleep(0); // switch context
            }
        });

        //When
        startEvent.Set();
        Task.WaitAll(deletionTasksFromSource, deletionTasksFromTarget);

        //Then
        sourceList.Count.ShouldBe(0);
        targetList.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldSupportMixedOperations()
    {
        //Given
        using var synchronizer = CreateInstance();

        var initialItems = Enumerable.Range(0, 1000).ToArray();
        sourceList.AddOrUpdate(initialItems);

        var itemsToDelete = Enumerable.Range(0, 500).ToArray();
        var itemsToAdd = Enumerable.Range(1000, 500).ToArray();

        var startEvent = new ManualResetEvent(false);

        var mixedTasksSource = Task.Factory.StartNew(() =>
        {
            startEvent.WaitOne();
            foreach (var i in itemsToDelete)
            {
                sourceList.Remove(i);
                Thread.Sleep(0); // switch context
            }

            foreach (var i in itemsToAdd)
            {
                sourceList.AddOrUpdate(i);
                Thread.Sleep(0); // switch context
            }
        });
        var mixedTasksTarget = Task.Factory.StartNew(() =>
        {
            startEvent.WaitOne();
            foreach (var i in itemsToAdd)
            {
                targetList.AddOrUpdate(i);
                Thread.Sleep(0); // switch context
            }
        });

        //When
        startEvent.Set();
        Task.WaitAll(mixedTasksSource, mixedTasksTarget);

        //Then
        var expectedItems = initialItems.Skip(500).Concat(itemsToAdd).ToArray();
        sourceList.Items.OrderBy(x => x).CollectionSequenceShouldBe(expectedItems);
        targetList.Items.OrderBy(x => x).CollectionSequenceShouldBe(expectedItems);
    }


    [Test]
    public void ShouldHandleBulkChanges()
    {
        //Given
        using var synchronizer = CreateInstance();

        var initialItems = Enumerable.Range(0, 999).ToArray();
        sourceList.AddOrUpdate(initialItems);

        var itemsToReplace = initialItems.Take(999).ToArray();
        var replacementItems = Enumerable.Range(1000, 999).ToArray();

        var startEvent = new ManualResetEvent(false);

        var bulkChangeTask = Task.Factory.StartNew(() =>
        {
            startEvent.WaitOne();
            sourceList.Edit(innerList =>
            {
                foreach (var i in itemsToReplace)
                {
                    innerList.Remove(i);
                }

                innerList.AddOrUpdate(replacementItems);
            });
        });

        //When
        startEvent.Set();
        bulkChangeTask.Wait();

        //Then
        sourceList.Items.OrderBy(x => x).CollectionSequenceShouldBe(replacementItems);
        targetList.Items.OrderBy(x => x).CollectionSequenceShouldBe(replacementItems);
    }


    private SourceCachesSynchronizer<int, string> CreateInstance()
    {
        return new SourceCachesSynchronizer<int, string>(sourceList, targetList);
    }
}