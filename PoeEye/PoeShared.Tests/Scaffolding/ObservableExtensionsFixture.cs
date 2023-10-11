using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
internal class ObservableExtensionsFixtureTests : FixtureBase
{
    [Test]
    public async Task ShouldSubscribeAsync()
    {
        //Given
        var processedItems = new ConcurrentQueue<int>();

        var observable = new Subject<int>();

        const int itemsCount = 100;
        var isCompleted = new ManualResetEventSlim(false);
        using var subscription = observable
            .SubscribeAsync(async item =>
        {
            processedItems.Enqueue(item);
            await Task.Delay(10);
            processedItems.Enqueue(item);

            if (item == itemsCount)
            {
                isCompleted.Set();
            }
        });

        //When
        await Task.Run(() =>
        {
            for (int i = 0; i <= itemsCount; i++)
            {
                observable.OnNext(i);
            }
        });
        isCompleted.Wait();

        //Then
        processedItems.CollectionSequenceShouldBe(Enumerable.Range(0, itemsCount + 1).SelectMany(x => new[] { x, x }).ToArray());
    }
    
    [Test]
    public async Task ShouldSelectAsync()
    {
        //Given
        var processedItems = new ConcurrentQueue<int>();

        var observable = new Subject<int>();

        const int itemsCount = 100;
        var isCompleted = new ManualResetEventSlim(false);
        using var subscription = observable
            .SelectAsync(async item =>
            {
                await Task.Delay(10);
                return item;
            })
            .Subscribe(item =>
            {
                processedItems.Enqueue(item);
                if (item == itemsCount)
                {
                    isCompleted.Set();
                }
            });

        //When
        await Task.Run(() =>
        {
            for (int i = 0; i <= itemsCount; i++)
            {
                observable.OnNext(i);
            }
        });
        isCompleted.Wait();

        //Then
        processedItems.CollectionSequenceShouldBe(Enumerable.Range(0, itemsCount + 1).ToArray());
    }
}