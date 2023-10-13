using DynamicData;

namespace PoeShared.Scaffolding;

public class LoggingListChangeSetAdaptor<T> : LoggingChangeSetAdaptorBase, IChangeSetAdaptor<T>
{
    private readonly IChangeSetAdaptor<T> innerAdaptor;

    public LoggingListChangeSetAdaptor(string name = default, IFluentLog logger = default, FluentLogLevel logLevel = default, IChangeSetAdaptor<T> innerAdaptor = default)
    {
        this.innerAdaptor = innerAdaptor;
        Logger = logger?.WithPrefix($"{(string.IsNullOrEmpty(name) ? $"Collection<{typeof(T).Name}>" : name)}").WithSuffix($"TID {ParentThread}");
        LogLevel = logLevel;
        WriteLog($"Collection of type {typeof(T)} is created @ {ParentStackTraceInfo}");
    }

    public void Adapt(IChangeSet<T> changeSet)
    {
        try
        {
            if (changeSet is ChangeSet<T> changes)
            {
                for (var i = 0; i < changes.Count; i++)
                {
                    var change = changes[i];
                    WriteLog($"Adapting the change [{i}/{changes.Count}] : {new {change.Reason, change.Type, change.Range}}");
                }
            }
            else
            {
                WriteLog($"Adapting changeset: {new {changeSet.TotalChanges, changeSet.Count, changeSet.Replaced, changeSet.Adds, changeSet.Refreshes, changeSet.Removes}}");
            }

            innerAdaptor?.Adapt(changeSet);
        }
        catch (Exception e)
        {
            Logger?.Warn($"Full collection log:\n{Messages.DumpToTable("\n")}", e);
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            throw;
        }
    }
}

public class LoggingChangeSetAdaptor<T, TKey> : LoggingChangeSetAdaptorBase, IChangeSetAdaptor<T, TKey>
{
    private readonly IChangeSetAdaptor<T, TKey> innerAdaptor;

    public LoggingChangeSetAdaptor(string name = default, IFluentLog logger = default, FluentLogLevel logLevel = default, IChangeSetAdaptor<T, TKey> innerAdaptor = default)
    {
        this.innerAdaptor = innerAdaptor;
        Logger = logger?.WithPrefix($"{(string.IsNullOrEmpty(name) ? $"Cache<{typeof(T).Name}, {typeof(TKey).Name}>" : name)}").WithSuffix($"TID {ParentThread}");
        LogLevel = logLevel;
        WriteLog($"Collection of type {typeof(T)} is created @ {ParentStackTraceInfo}");
    }

    public void Adapt(IChangeSet<T, TKey> changeSet)
    {
        try
        {
            if (changeSet is ChangeSet<T, TKey> changes)
            {
                for (var i = 0; i < changes.Count; i++)
                {
                    var change = changes[i];
                    WriteLog($"Adapting the change [{i}/{changes.Count}] : {new {change.Reason, change.Key, change.CurrentIndex, change.PreviousIndex}}");
                }
            }
            else
            {
                WriteLog($"Adapting changeset: {new {changeSet.Adds, changeSet.Count, changeSet.Updates, changeSet.Refreshes, changeSet.Removes}}");
            }

            innerAdaptor?.Adapt(changeSet);
        }
        catch (Exception e)
        {
            Logger?.Warn($"Full collection log:\n{Messages.DumpToTable("\n")}", e);
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            throw;
        }
    }

}