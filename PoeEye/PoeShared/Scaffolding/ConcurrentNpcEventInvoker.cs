using System.ComponentModel;

namespace PoeShared.Scaffolding;

internal sealed class ConcurrentNpcEventInvoker : INpcEventInvoker
{
    private readonly ConcurrentDictionary<PropertyChangedEventHandler, DelegateHolder> delegates = new();
    private readonly object owner;
    private readonly LinkedList<DelegateHolder> delegatesList = new();
    private readonly ReaderWriterLockSlim listLocker = new();

    public ConcurrentNpcEventInvoker(object owner)
    {
        this.owner = owner;
    }

    public bool IsEmpty => delegatesList.Count <= 0;

    [DebuggerStepThrough]
    public void Add(PropertyChangedEventHandler value)
    {
        var holder = new DelegateHolder(value);
        if (!delegates.TryAdd(value, holder))
        {
            return;
        }

        listLocker.EnterWriteLock();
        try
        {
            delegatesList.AddLast(holder);
        }
        finally
        {
            listLocker.ExitWriteLock();
        }
    }

    [DebuggerStepThrough]
    public void Remove(PropertyChangedEventHandler value)
    {
        if (!delegates.TryRemove(value, out var holder))
        {
            return;
        }
        lock (holder)
        {
            holder.IsDeleted = true;
        }
    }

    [DebuggerStepThrough]
    public void Raise(string propertyName)
    {
        if (IsEmpty)
        {
            return;
        }

        var args = new PropertyChangedEventArgs(propertyName);
        DelegateHolder holder = null;
        try
        {
            listLocker.EnterReadLock();
            LinkedListNode<DelegateHolder> cursor;
            try
            {
                cursor = delegatesList.First;
            }
            finally
            {
                listLocker.ExitReadLock();
            }

            while (cursor != null)
            {
                listLocker.EnterReadLock();
                holder = cursor.Value;
                LinkedListNode<DelegateHolder> next;
                try
                {
                    next = cursor.Next;
                }
                finally
                {
                    listLocker.ExitReadLock();
                }

                PropertyChangedEventHandler action;
                lock (holder)
                {
                    if (!holder.IsDeleted)
                    {
                        action = holder.Action;
                    }
                    else
                    {
                        action = default;
                        if (!holder.IsDeletedFromList)
                        {
                            listLocker.EnterWriteLock();
                            try
                            {
                                delegatesList.Remove(cursor);
                                holder.IsDeletedFromList = true;
                            }
                            finally
                            {
                                listLocker.ExitWriteLock();
                            }
                        }
                    }
                }
                action?.DynamicInvoke(owner, args);
                cursor = next;
            }
        }
        catch
        {
            // clean up
            if (listLocker.IsReadLockHeld)
            {
                listLocker.ExitReadLock();
            }

            if (listLocker.IsWriteLockHeld)
            {
                listLocker.ExitWriteLock();
            }

            if (holder != null && Monitor.IsEntered(holder))
            {
                Monitor.Exit(holder);
            }

            throw;
        }
    }

    private sealed class DelegateHolder
    {
        public DelegateHolder(PropertyChangedEventHandler d)
        {
            Action = d;
        }

        public PropertyChangedEventHandler Action { get; }

        /// <summary>
        ///     flag shows if this delegate removed from list of calls
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        ///     flag shows if this instance was removed from all lists
        /// </summary>
        public bool IsDeletedFromList { get; set; }
    }
}