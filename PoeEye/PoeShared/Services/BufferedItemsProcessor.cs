using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using ReactiveUI;
using Unity;

namespace PoeShared.Services;

internal sealed class BufferedItemsProcessor : DisposableReactiveObject, IBufferedItemsProcessor
{
    private static readonly IFluentLog Log = typeof(BufferedItemsProcessor).PrepareLogger();

    private readonly BlockingCollection<ItemUpdateRequest> requestsBuffer = new();
    private readonly IScheduler scheduler;
    private readonly ISubject<bool> flushSink = new Subject<bool>();
    private readonly ISubject<string> updateSink = new Subject<string>();

    public BufferedItemsProcessor([OptionalDependency] IScheduler scheduler)
    {
        this.scheduler = scheduler;

        var immediate = scheduler == null || ReferenceEquals(scheduler, Scheduler.Immediate);

        var updateRequests = this.WhenAnyValue(x => x.BufferPeriod)
            .Select(x => x > TimeSpan.Zero
                ? x < TimeSpan.MaxValue ? updateSink.Sample(x) : Observable.Never<string>()
                : updateSink)
            .Switch()
            .Select(x => immediate ? Observable.Return(x) : Observable.Return(x, this.scheduler))
            .Switch();

        var flushRequests = flushSink
            .Select(x => x || immediate ? Observable.Return("Immediate flush") : Observable.Return("Delayed flush", this.scheduler))
            .Switch();
            
        updateRequests
            .Merge(flushRequests)
            .SubscribeSafe(ProcessRequests, Log.HandleUiException)
            .AddTo(Anchors);
    }

    public TimeSpan BufferPeriod { get; set; } = TimeSpan.Zero;
    
    public uint Capacity { get; set; } = uint.MaxValue;

    public void Add(BufferedItemState state, IBufferedItem item)
    {
        while (requestsBuffer.Count > Capacity - 1 && requestsBuffer.TryTake(out var removedItem))
        {
            Log.Debug(() => $"Removed item from the queue(capacity: {Capacity}, item count: {requestsBuffer.Count}): {removedItem}");
        }

        var itemToAdd = new ItemUpdateRequest {Item = item, State = state};
        Log.Debug(() => $"Adding item to the queue(capacity: {Capacity}, item count: {requestsBuffer.Count}): {itemToAdd}");
        requestsBuffer.Add(itemToAdd);
        updateSink.OnNext($"Update: {itemToAdd}");
    }

    public void Flush(bool immediateFlush)
    {
        flushSink.OnNext(immediateFlush);
    }
    
    private void ProcessRequests(string reason)
    {
        var sw = Stopwatch.StartNew();

        var changesToProcess = requestsBuffer.Count;
        if (changesToProcess <= 0)
        {
            Log.Debug(() => $"No changes to flush");
            return;
        }
        
        Log.Debug(() => $"Processing {changesToProcess} changes:\n\t{requestsBuffer.DumpToTable()}");
        var changes = new ItemUpdateRequest[changesToProcess];
        var itemIndex = 0;
        while (requestsBuffer.TryTake(out var itemChange) && itemIndex < changes.Length)
        {
            changes[itemIndex++] = itemChange;
        }

        var changesByItem = changes.GroupBy(x => x.Item.Id).ToArray();
        Log.Debug(() => $"Grouped {changes.Length} changes into {changesByItem.Length} packs");

        foreach (var grouping in changesByItem)
        {
            var item = grouping.Key;
            var itemChanges = grouping.ToArray();
            try
            {
                for (var index = 0; index < itemChanges.Length; index++)
                {
                    var change = itemChanges[index];
                    change.Item.HandleState(change.State);

                    if (change.State is BufferedItemState.Added or BufferedItemState.Changed)
                    {
                        while (index < itemChanges.Length && itemChanges[index].State == BufferedItemState.Changed)
                        {
                            index++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to process item {item}, changes({itemChanges.Length}): {changes.Select(x => x.State).DumpToString()}", e);
            }
        }

        Log.Debug(() => $"Processed all changes in {sw.ElapsedMilliseconds}ms");
    }

    private readonly record struct ItemUpdateRequest
    {
        public BufferedItemState State { get; init; }
        public IBufferedItem Item { get; init; }
    }
}