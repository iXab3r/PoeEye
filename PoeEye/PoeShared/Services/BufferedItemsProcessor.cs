using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Threading.Channels;
using ReactiveUI;
using Unity;

namespace PoeShared.Services;

internal sealed class BufferedItemsProcessor : DisposableReactiveObject, IBufferedItemsProcessor
{
    private static readonly IFluentLog Log = typeof(BufferedItemsProcessor).PrepareLogger();

    private readonly Channel<Request> requests;
    private readonly IScheduler scheduler;
    private readonly WorkerTask processingTask;
    private readonly AutoResetEvent timeoutEvent;

    public BufferedItemsProcessor([OptionalDependency] IScheduler scheduler)
    {
        this.scheduler = scheduler;
        timeoutEvent = new AutoResetEvent(false);
        requests = Channel.CreateUnbounded<Request>();
        processingTask = new WorkerTask(HandleRequests){ Name = "BIP Task" }.AddTo(Anchors);
    }

    public TimeSpan BufferPeriod { get; set; } = TimeSpan.Zero;
    
    public uint Capacity { get; set; } = uint.MaxValue;

    public void Add<T>(BufferedItemState state, T item) where T : IBufferedItemId
    {
        EnqueueRequest(state, item, CancellationToken.None).AndForget();
        Log.Debug($"Adding item to the queue(capacity: {Capacity}, item count: {requests.Reader.Count})");
    }

    public async Task FlushAsync()
    {
        Log.Debug($"Adding flush request to the queue(capacity: {Capacity}, item count: {requests.Reader.Count})");
        var flushRequest = EnqueueFlush(CancellationToken.None).Result;
        timeoutEvent.Set();
        
        Log.Debug($"Awaiting for flush to complete");
        await flushRequest.CompletionSource.Task;
        Log.Debug($"Flush request has completed");
    }
    
    public void Flush(bool immediateFlush)
    {
        FlushAsync().Wait();
    }
    
     private async Task HandleRequests(CancellationToken cancellationToken)
    {
        try
        {
            Log.Debug($"Initializing processing loop");
            while (!cancellationToken.IsCancellationRequested && await requests.Reader.WaitToReadAsync(cancellationToken))
            {
                try
                {
                    var items = await GetItems(cancellationToken);
                    await HandleRequest(items, cancellationToken);
                }
                catch (Exception e)
                {
                    switch (e)
                    {
                        case TaskCanceledException:
                        {
                            Log.Debug("Processing loop has been cancelled", e);
                            break;
                        }
                        default:
                            Log.Warn("Critical error in processing loop", e);
                            break;
                    }
                }
            }
        }
        finally
        {
            Log.Debug($"Completed processing loop(cancelled: {cancellationToken.IsCancellationRequested})");
        }
    }
     
     
    private async Task HandleRequest(Request[] items, CancellationToken cancellationToken)
    {
        var sw = ValueStopwatch.StartNew();

        try
        {
            var changes = items.OfType<ItemUpdateRequest>().ToArray();
            await Observable.StartAsync(() => ProcessRequests(changes, cancellationToken), scheduler);
            foreach (var workItem in items)
            {
                workItem.CompletionSource.SetResult(true);
            }
        }
        catch (Exception e)
        {
            foreach (var workItem in items)
            {
                workItem.CompletionSource.SetException(e);
            }
        }
        finally
        {
            var timeout = BufferPeriod;
            Log.Debug($"Processed all changes in {sw.ElapsedMilliseconds}ms, timeout: {timeout}");
            if (timeout > TimeSpan.Zero)
            {
                while (!cancellationToken.IsCancellationRequested && requests.Reader.Count == 0)
                {
                    if (timeoutEvent.WaitOne(timeout))
                    {
                        Log.Debug("Timeout ended prematurely");
                        break;
                    }
                }
            }
        }
    }

    private async Task<Request[]> GetItems(CancellationToken cancellationToken)
    {
        var changesToProcess = requests.Reader.Count;
        Log.Debug($"Processing {changesToProcess} changes");

        var changes = new Request[changesToProcess];
        var itemIndex = 0;
        while (requests.Reader.TryRead(out var item) && itemIndex < changes.Length)
        {
            changes[itemIndex++] = item;
        }

        return itemIndex < changesToProcess ? changes[0..itemIndex] : changes;
    }
    
    private static async Task ProcessRequests(ItemUpdateRequest[] changes, CancellationToken cancellationToken)
    {
        var changesByItem = changes.GroupBy(x => x.Item.Id).ToArray();
        Log.Debug($"Grouped {changes.Length} changes into {changesByItem.Length} packs");

        foreach (var grouping in changesByItem)
        {
            var item = grouping.Key;
            var itemChanges = grouping.ToArray();
            try
            {
                for (var index = 0; index < itemChanges.Length; index++)
                {
                    var change = itemChanges[index];
                    
                    switch (change.Item)
                    {
                        case IBufferedItem bufferedItem:
                            bufferedItem.HandleState(change.State);
                            break;
                        case IBufferedItemAsync bufferedItemAsync:
                            await bufferedItemAsync.HandleStateAsync(change.State);
                            break;
                        default:
                            throw new NotSupportedException($"Items of type {change.Item.GetType()} are not supported");
                    }

                    if (change.State is BufferedItemState.Added or BufferedItemState.Changed)
                    {
                        var nextIndex = index;
                        while (nextIndex+1 < itemChanges.Length && itemChanges[nextIndex+1].State == BufferedItemState.Changed)
                        {
                            nextIndex++;
                        }
                        index = nextIndex;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to process item {item}, changes({itemChanges.Length}): {changes.Select(x => $"{x.State} {x.Item.Id}").DumpToString()}", e);
            }
        }
    }
    
    private async Task<ItemUpdateRequest> EnqueueRequest(
        BufferedItemState request,
        IBufferedItemId item,
        CancellationToken cancellationToken)
    {
        var newRequest = new ItemUpdateRequest
        {
            Item = item,
            State = request,
            CompletionSource = new TaskCompletionSource<bool>()
        };
        await requests.Writer.WriteAsync(newRequest, cancellationToken);
        return newRequest;
    }
    
    private async Task<FlushRequest> EnqueueFlush(
        CancellationToken cancellationToken)
    {
        var newRequest = new FlushRequest()
        {
            CompletionSource = new TaskCompletionSource<bool>()
        };
        await requests.Writer.WriteAsync(newRequest, cancellationToken);
        return newRequest;
    }

    private abstract record Request
    {
        public TaskCompletionSource<bool> CompletionSource { get; init; }
    }
    
    private sealed record FlushRequest : Request
    {
    }
    
    private sealed record ItemUpdateRequest : Request
    {
        public BufferedItemState State { get; init; }
        public IBufferedItemId Item { get; init; }
    }
}