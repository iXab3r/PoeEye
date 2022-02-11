using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;

namespace PoeShared.GCLog;

internal sealed class ClrEventsManager
{
    private readonly ContentionInfoStore contentionStore;
    private readonly ClrEventFilter filter;
    private readonly int targetProcessId;
    private readonly TraceEventSession session;
    private readonly ClrTypesInfo clrTypes;

    // constructor for EventPipe traces
    public ClrEventsManager(int targetProcessId, ClrEventFilter filter)
    {
        this.targetProcessId = targetProcessId;
        clrTypes = new ClrTypesInfo();
        contentionStore = new ContentionInfoStore();
        contentionStore.AddProcess(targetProcessId);
        this.filter = filter;
    }

    // constructor for TraceEvent + ETW traces
    public ClrEventsManager(TraceEventSession session, int processId, ClrEventFilter filter)
        : this(processId, filter)
    {
        if (session == null)
        {
            throw new NullReferenceException($"{nameof(session)} must be provided");
        }

        this.session = session;
    }

    public event EventHandler<ClrExceptionArgs> FirstChanceException;
    public event EventHandler<ClrFinalizeArgs> Finalize;
    public event EventHandler<ContentionArgs> Contention;
    public event EventHandler<ClrThreadPoolStarvationArgs> ThreadPoolStarvation;
    public event EventHandler<GarbageCollectionArgs> GarbageCollection;
    public event EventHandler<AllocationTickArgs> AllocationTick;

    private static ulong GetKeywords()
    {
        return (ulong)(
            ClrTraceEventParser.Keywords.Contention | // thread contention timing
            ClrTraceEventParser.Keywords.Threading | // threadpool events
            ClrTraceEventParser.Keywords.Exception | // get the first chance exceptions
            ClrTraceEventParser.Keywords.GCHeapAndTypeNames | // for finalizer type names
            ClrTraceEventParser.Keywords.Type | // for TypeBulkType definition of types
            ClrTraceEventParser.Keywords.GC // garbage collector details
        );
    }

    public void ProcessEvents()
    {
        if (session != null)
        {
            ProcessEtwEvents();
            return;
        }
    }

    private void ProcessEtwEvents()
    {
        // setup process filter if any
        TraceEventProviderOptions options = null;
        if (targetProcessId != -1)
        {
            options = new TraceEventProviderOptions()
            {
                ProcessIDFilter = new List<int>() { targetProcessId }
            };
        }

        // register handlers for events on the session source
        // --------------------------------------------------
        RegisterListeners(session.Source);

        // decide which provider to listen to with filters if needed
        session.EnableProvider(
            ClrTraceEventParser.ProviderGuid, // CLR provider
            ((filter & ClrEventFilter.AllocationTick) == ClrEventFilter.AllocationTick) ? TraceEventLevel.Verbose : TraceEventLevel.Informational,
            GetKeywords(),
            options
        );


        // this is a blocking call until the session is disposed
        session.Source.Process();
    }

    private void RegisterListeners(TraceEventDispatcher source)
    {
        if ((filter & ClrEventFilter.Exception) == ClrEventFilter.Exception)
        {
            // get exceptions
            source.Clr.ExceptionStart += OnExceptionStart;
        }

        if ((filter & ClrEventFilter.Finalizer) == ClrEventFilter.Finalizer)
        {
            // get finalizers
            source.Clr.TypeBulkType += OnTypeBulkType;
            source.Clr.GCFinalizeObject += OnGCFinalizeObject;
        }

        if ((filter & ClrEventFilter.ThreadStarvation) == ClrEventFilter.ThreadStarvation)
        {
            // detect ThreadPool starvation
            source.Clr.ThreadPoolWorkerThreadAdjustmentAdjustment += OnThreadPoolWorkerAdjustment;
        }

        if ((filter & ClrEventFilter.Gc) == ClrEventFilter.Gc)
        {
            source.NeedLoadedDotNetRuntimes();
            source.AddCallbackOnProcessStart(proc =>
            {
                if (proc.ProcessID != targetProcessId)
                    return;

                proc.AddCallbackOnDotNetRuntimeLoad(runtime => { runtime.GCEnd += (p, gc) => { NotifyCollection(gc); }; });
            });
        }

        if ((filter & ClrEventFilter.AllocationTick) == ClrEventFilter.AllocationTick)
        {
            // sample every ~100 KB of allocations
            source.Clr.GCAllocationTick += OnGCAllocationTick;
        }
    }

    private void OnGCAllocationTick(GCAllocationTickTraceData data)
    {
        NotifyAllocationTick(data);
    }

    private void OnExceptionStart(ExceptionTraceData data)
    {
        if (data.ProcessID != targetProcessId)
            return;

        NotifyFirstChanceException(data.TimeStamp, data.ProcessID, data.ExceptionType, data.ExceptionMessage);
    }

    private void OnTypeBulkType(GCBulkTypeTraceData data)
    {
        if (data.ProcessID != targetProcessId)
            return;

        // keep track of the id/name type associations
        for (int currentType = 0; currentType < data.Count; currentType++)
        {
            GCBulkTypeValues value = data.Values(currentType);
            clrTypes[value.TypeID] = string.Intern(value.TypeName);
        }
    }

    private void OnGCFinalizeObject(FinalizeObjectTraceData data)
    {
        if (data.ProcessID != targetProcessId)
            return;

        // the type id should have been associated to a name via a previous TypeBulkType event
        NotifyFinalize(data.TimeStamp, data.ProcessID, data.TypeID, clrTypes[data.TypeID]);
    }

    private void OnThreadPoolWorkerAdjustment(ThreadPoolWorkerThreadAdjustmentTraceData data)
    {
        var listeners = ThreadPoolStarvation;
        listeners?.Invoke(this, new ClrThreadPoolStarvationArgs(data.TimeStamp, data.ProcessID, data.NewWorkerThreadCount));
    }


    private void NotifyFirstChanceException(DateTime timestamp, int processId, string typeName, string message)
    {
        var listeners = FirstChanceException;
        listeners?.Invoke(this, new ClrExceptionArgs(timestamp, processId, typeName, message));
    }

    private void NotifyFinalize(DateTime timeStamp, int processId, ulong typeId, string typeName)
    {
        var listeners = Finalize;
        listeners?.Invoke(this, new ClrFinalizeArgs(timeStamp, processId, typeId, typeName));
    }

    private void NotifyContention(DateTime timeStamp, int processId, int threadId, TimeSpan duration, bool isManaged)
    {
        var listeners = Contention;
        listeners?.Invoke(this, new ContentionArgs(timeStamp, processId, threadId, duration, isManaged));
    }

    private void NotifyCollection(TraceGC gc)
    {
        var listeners = GarbageCollection;
        if (listeners == null)
            return;

        var sizesBefore = GetBeforeGenerationSizes(gc);
        var sizesAfter = GetAfterGenerationSizes(gc);
        listeners?.Invoke(this, new GarbageCollectionArgs(
            targetProcessId,
            gc.StartRelativeMSec,
            gc.Number,
            gc.Generation,
            (GarbageCollectionReason)gc.Reason,
            (GarbageCollectionType)gc.Type,
            !gc.IsNotCompacting(),
            gc.HeapStats.GenerationSize0,
            gc.HeapStats.GenerationSize1,
            gc.HeapStats.GenerationSize2,
            gc.HeapStats.GenerationSize3,
            sizesBefore,
            sizesAfter,
            gc.SuspendDurationMSec,
            gc.PauseDurationMSec,
            gc.BGCFinalPauseMSec
        ));
    }

    private long[] GetBeforeGenerationSizes(TraceGC gc)
    {
        var before = true;
        return GetGenerationSizes(gc, before);
    }

    private long[] GetAfterGenerationSizes(TraceGC gc)
    {
        var after = false;
        return GetGenerationSizes(gc, after);
    }

    private long[] GetGenerationSizes(TraceGC gc, bool before)
    {
        var sizes = new long[4];
        if (gc.PerHeapHistories == null)
        {
            return sizes;
        }

        for (int heap = 0; heap < gc.PerHeapHistories.Count; heap++)
        {
            // LOH = 3
            for (int gen = 0; gen <= 3; gen++)
            {
                sizes[gen] += before ? gc.PerHeapHistories[heap].GenData[gen].ObjSpaceBefore : gc.PerHeapHistories[heap].GenData[gen].ObjSizeAfter;
            }
        }

        return sizes;
    }


    private void NotifyAllocationTick(GCAllocationTickTraceData info)
    {
        var listeners = AllocationTick;
        listeners?.Invoke(this, new AllocationTickArgs(
            info.TimeStamp,
            info.ProcessID,
            info.AllocationAmount,
            info.AllocationAmount64,
            info.AllocationKind == GCAllocationKind.Large,
            info.TypeName,
            info.HeapIndex,
            info.Address
        ));
    }
}