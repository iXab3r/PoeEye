#if DEBUG && false
#define OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
#endif


using DynamicData;
using DynamicData.Binding;

namespace PoeShared.Scaffolding;

public class ObservableCollectionAdaptorEx<T> : IChangeSetAdaptor<T>
{
    private static readonly IFluentLog Log = typeof(ObservableCollectionAdaptorEx<T>).PrepareLogger();

    private readonly IChangeSetAdaptor<T> changeSetAdaptor;

    public ObservableCollectionAdaptorEx(IObservableCollection<T> observableCollectionEx)
    {
        var observableChangeSetAdaptor = new ObservableCollectionAdaptor<T>(observableCollectionEx, refreshThreshold: int.MaxValue);
#if OBSERVABLECOLLECTIONEX_ENABLE_STACKTRACE_LOG
        var loggingChangeSetAdaptor = new LoggingChangeSetAdaptor<T>(observableChangeSetAdaptor);
        this.changeSetAdaptor = loggingChangeSetAdaptor;
#else
        this.changeSetAdaptor = observableChangeSetAdaptor;
#endif
    }

    public void Adapt(IChangeSet<T> changeSet)
    {
        try
        {
            changeSetAdaptor.Adapt(changeSet);
        }
        catch (Exception e)
        {
            Log.Error($"Unhandled exception in adaptor, change: {changeSet}", e);
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            throw;
        }
    }
}