using System.Text;
using System.Threading;
using Microsoft.Diagnostics.Tracing.Session;

namespace PoeShared.GCLog;

public sealed class EtwGcLog : GcLogBase
{
    // TODO: don't forget to update the header if you are adding more columns 
    private const string Header =
        "StartRelativeMSec,Number,Generation,Type,Reason,IsCompacting,SuspensionDurationInMilliSeconds,PauseDurationInMilliSeconds,FinalPauseDurationInMilliSeconds,Gen0Size,Gen1Size,Gen2Size,LOHSize,ObjGen0Before,ObjGen1Before,ObjGen2Before,ObjLOHBefore,ObjGen0After,ObjGen1After,ObjGen2After,ObjLOHAfter";

    private readonly StringBuilder line = new(2048);

    private TraceEventSession userSession;

    public EtwGcLog(FileInfo outputFile, int pid) : base(outputFile)
    {
        Pid = pid;
        Log = GetType().PrepareLogger().WithSuffix($"PID: {pid}");
        Log.Info("Initializing Gc log tracking");
    }

    public int Pid { get; }

    private IFluentLog Log { get; }

    protected override void OnStart()
    {
        Log.Debug("Starting Etw collection");

        // add a header to the .csv file
        WriteLine(Header);
        var reportThread = new Thread(RunReporting)
        {
            IsBackground = true,
            Name = "ClrEvents"
        };

        Log.Debug("Starting Clr report thread");
        reportThread.Start();
    }

    protected override void OnStop()
    {
        // when the session is disposed, the call to ProcessEvents() returns
        userSession.Dispose();
    }

    private void RunReporting()
    {
        try
        {
            Log.Debug("Clr report thread has started");
            var sessionName = $"GcLogEtwSession_PID{Pid}_{Guid.NewGuid()}";
            Log.Debug($"Starting {sessionName}...\r\n");
            userSession = new TraceEventSession(sessionName);

            Log.Info($"Initializing Clr events manager for {sessionName}");

            // only want to receive GC event
            var manager = new ClrEventsManager(userSession, Pid, ClrEventFilter.Gc);
            manager.GarbageCollection += OnGarbageCollection;

            // this is a blocking call until the session is disposed
            manager.ProcessEvents();
            Log.Info("End of CLR event processing");
        }
        catch (Exception ex)
        {
            Log.Error("Suppressing unhandled exception in Clr report thread", ex);
        }
        finally
        {
            Log.Debug("Clr report thread has completed");
        }
    }

    private void OnGarbageCollection(object sender, GarbageCollectionArgs e)
    {
        line.Clear();
        line.Append($"{e.StartRelativeMSec.ToString(CultureInfo.InvariantCulture)},");
        line.Append($"{e.Number.ToString()},");
        line.Append($"{e.Generation.ToString()},");
        line.Append($"{e.Type},");
        line.Append($"{e.Reason},");
        line.Append($"{e.IsCompacting.ToString()},");
        line.Append($"{e.SuspensionDuration.ToString(CultureInfo.InvariantCulture)},");
        line.Append($"{e.PauseDuration.ToString(CultureInfo.InvariantCulture)},");
        line.Append($"{e.BgcFinalPauseDuration.ToString(CultureInfo.InvariantCulture)},");
        line.Append($"{e.Gen0Size.ToString()},");
        line.Append($"{e.Gen1Size.ToString()},");
        line.Append($"{e.Gen2Size.ToString()},");
        line.Append($"{e.LohSize.ToString()},");
        line.Append($"{e.ObjSizeBefore[0].ToString()},");
        line.Append($"{e.ObjSizeBefore[1].ToString()},");
        line.Append($"{e.ObjSizeBefore[2].ToString()},");
        line.Append($"{e.ObjSizeBefore[3].ToString()},");
        line.Append($"{e.ObjSizeAfter[0].ToString()},");
        line.Append($"{e.ObjSizeAfter[1].ToString()},");
        line.Append($"{e.ObjSizeAfter[2].ToString()},");
        line.Append($"{e.ObjSizeAfter[3].ToString()}");

        WriteLine(line.ToString());
    }
}