using System.Reactive.Subjects;
using DynamicData;
using PoeShared.Services;

namespace PoeShared.Scaffolding;

/// <summary>
/// Provides synchronization between two source lists. Changes made to one list will be reflected in the other and vice versa.
/// NOTE: The same approach for SourceList does not work as it is not really possible to distinguish between some operations in multi-threaded environment. It will work MOST of the time, but for some combinations of operation this will generate invalid sequences.
/// </summary>
/// <typeparam name="TObject">The type of items within the source lists.</typeparam>
/// <typeparam name="TKey">The type of item</typeparam>
public class SourceCachesSynchronizer<TObject, TKey> : DisposableReactiveObject
{
    private enum ActionType { SourceToTarget, TargetToSource }

    private readonly ConcurrentQueue<ChangeMessage> messageQueue = new();
    private readonly ISourceCache<TObject, TKey> sourceList;
    private readonly ISourceCache<TObject, TKey> targetList;
    private readonly object processingLock = new();
    
    public SourceCachesSynchronizer(ISourceCache<TObject, TKey> sourceList, ISourceCache<TObject, TKey> targetList)
    {
        this.sourceList = sourceList;
        this.targetList = targetList;

        // Clear the target list initially.
        targetList.Clear();

        // Subscribe to changes in the source list and propagate them to the message queue.
        sourceList.Connect().Subscribe(changes =>
        {
            if (Monitor.IsEntered(processingLock))
            {
                // That means that right now we're processing the queue of changes, i.e. the change is internal
                // State: lock of Source list is already taken + processing lock is also taken
                return;
            }
            messageQueue.Enqueue(new ChangeMessage(ActionType.SourceToTarget, changes));
            DoProcessing();
        }).AddTo(Anchors);

        // Subscribe to changes in the target list and propagate them to the message queue.
        targetList.Connect().Subscribe(changes =>
        {
            if (Monitor.IsEntered(processingLock))
            {
                // That means that right now we're processing the queue of changes, i.e. the change is internal
                // State: lock of Target list is already taken + processing lock is also taken
                return;
            }
            messageQueue.Enqueue(new ChangeMessage(ActionType.TargetToSource, changes));
            DoProcessing();
        }).AddTo(Anchors);
    }

    public static SourceCachesSynchronizer<TObject, TKey> From(ISourceCache<TObject, TKey> sourceList, ISourceCache<TObject, TKey> targetList)
    {
        return new SourceCachesSynchronizer<TObject, TKey>(sourceList, targetList);
    }

    private void DoProcessing()
    {
        if (!Monitor.TryEnter(processingLock))
        {
            return;
        }

        try
        {
            while (messageQueue.TryDequeue(out var message))
            {
                switch (message.ActionType)
                {
                    case ActionType.SourceToTarget:
                        ApplyChanges(targetList, message.Changes);
                        break;

                    case ActionType.TargetToSource:
                        ApplyChanges(sourceList, message.Changes);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        finally
        {
            Monitor.Exit(processingLock);
        }
    }

    private static void ApplyChanges(ISourceCache<TObject,TKey> destinationList, IChangeSet<TObject, TKey> changes)
    {
        destinationList.Edit(innerList =>
        {
            innerList.Clone(changes);
        });
    }

    private class ChangeMessage
    {
        public ActionType ActionType { get; }
        public IChangeSet<TObject, TKey> Changes { get; }

        public ChangeMessage(ActionType actionType, IChangeSet<TObject, TKey> changes)
        {
            ActionType = actionType;
            Changes = changes;
        }
    }
}
